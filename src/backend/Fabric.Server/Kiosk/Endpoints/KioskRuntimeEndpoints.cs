using System.Security.Claims;
using Fabric.Server.Automation.Kiosk;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Kiosk.Application;
using Fabric.Server.Kiosk.Contracts;
using Fabric.Server.Kiosk.Domain;
using Fabric.Server.Kiosk.Persistence;
using Fabric.Server.Sagas.Kiosk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Kiosk.Endpoints;

public static class KioskRuntimeEndpoints
{
    private static readonly TimeSpan SessionStartupTimeout = TimeSpan.FromSeconds(30);

    public static IEndpointRouteBuilder MapKioskRuntimeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder kiosk = app.MapGroup("/api/kiosk").RequireAuthorization(KioskAuthenticationDefaults.Policy);

        kiosk.MapGet("/config", GetKioskConfig).Produces<KioskConfigResponse>().Produces(StatusCodes.Status404NotFound);
        kiosk.MapPost("/heartbeat", PostKioskHeartbeat).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);
        kiosk.MapPost("/language", ChangeKioskLanguage).Produces<KioskSessionResponse>().Produces(StatusCodes.Status404NotFound);
        kiosk.MapPost("/sessions", StartKioskSession).Produces<KioskSessionResponse>().Produces(StatusCodes.Status409Conflict);
        kiosk.MapGet("/runtime-state", GetRuntimeState).Produces<KioskInstructionResponse>().Produces(StatusCodes.Status404NotFound);
        kiosk.MapPost("/sessions/current/instructions/{instructionId}/response", SubmitInstructionResponse).Produces<KioskSessionResponse>().Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        kiosk.MapPost("/sessions/current/cancel", CancelCurrentSession).Produces<KioskSessionResponse>().Produces(StatusCodes.Status404NotFound);
        kiosk.MapGet("/assets/{assetName}", GetKioskAsset).Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetKioskConfig([FromQuery] string? languageCode, HttpContext context, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        Guid kioskId = context.User.GetKioskId();
        Domain.Kiosk? kiosk = await db.Kiosks.AsNoTracking().SingleOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        KioskProfile? profile = await db.Profiles.AsNoTracking().SingleOrDefaultAsync(profile => profile.Id == kiosk.ProfileId, cancellationToken);
        if (profile is null)
            return Results.NotFound();

        KioskProfileLanguage[] languages = await db.Languages.AsNoTracking().Where(language => language.ProfileId == profile.Id).OrderBy(language => language.SortOrder).ToArrayAsync(cancellationToken);
        KioskWelcomeSettings? welcome = await db.WelcomeSettings.AsNoTracking().SingleOrDefaultAsync(settings => settings.ProfileId == profile.Id, cancellationToken);
        Dictionary<string, string> theme = await db.ThemeTokens.AsNoTracking().Where(token => token.ProfileId == profile.Id).ToDictionaryAsync(token => token.Key, token => token.Value, cancellationToken);
        ResolvedKioskWelcomeResponse? resolvedWelcome = welcome is null ? null : await ResolveWelcomeAsync(welcome, profile, languageCode, db, cancellationToken);

        return Results.Ok(new KioskConfigResponse(kiosk.ToResponse(), profile.ToResponse(), languages.Select(language => language.ToResponse()).ToArray(), welcome?.ToResponse(), resolvedWelcome, theme));
    }

    private static async Task<ResolvedKioskWelcomeResponse> ResolveWelcomeAsync(KioskWelcomeSettings welcome, KioskProfile profile, string? languageCode, KioskDbContext db, CancellationToken cancellationToken)
    {
        string resolvedLanguage = string.IsNullOrWhiteSpace(languageCode) ? profile.DefaultLanguageCode : languageCode;
        KioskTranslation[] translationRows = await db.Translations
            .AsNoTracking()
            .Where(translation => translation.ProfileId == profile.Id && (translation.LanguageCode == resolvedLanguage || translation.LanguageCode == profile.DefaultLanguageCode))
            .OrderBy(translation => translation.LanguageCode == resolvedLanguage ? 0 : 1)
            .ToArrayAsync(cancellationToken);
        Dictionary<string, string> translations = [];
        foreach (KioskTranslation translation in translationRows)
            translations.TryAdd(translation.Key, translation.Value);

        string? backgroundUrl = string.IsNullOrWhiteSpace(welcome.BackgroundAssetName) ? null : $"/api/kiosk/assets/{Uri.EscapeDataString(welcome.BackgroundAssetName)}?languageCode={Uri.EscapeDataString(resolvedLanguage)}";
        string? logoUrl = string.IsNullOrWhiteSpace(welcome.LogoAssetName) ? null : $"/api/kiosk/assets/{Uri.EscapeDataString(welcome.LogoAssetName)}?languageCode={Uri.EscapeDataString(resolvedLanguage)}";

        return new ResolvedKioskWelcomeResponse(
            ResolveText(welcome.TitleKey, translations),
            welcome.SubtitleKey is null ? null : ResolveText(welcome.SubtitleKey, translations),
            ResolveText(welcome.StartButtonKey, translations),
            backgroundUrl,
            logoUrl);
    }

    private static string ResolveText(string key, IReadOnlyDictionary<string, string> translations) => translations.TryGetValue(key, out string? value) ? value : key;

    private static async Task<IResult> PostKioskHeartbeat([FromBody] KioskHeartbeatRequest request, HttpContext context, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        Domain.Kiosk? kiosk = await GetAuthenticatedKioskAsync(context, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        kiosk.MarkSeen(request.ReportedAt);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> StartKioskSession([FromBody] StartKioskSessionRequest request, HttpContext context, KioskDbContext db, [FromServices] KioskWorkflowStarter workflowStarter, [FromServices] KioskSagaService kioskSagaService, TimeProvider timeProvider, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken = default)
    {
        ILogger logger = loggerFactory.CreateLogger("Fabric.Server.Kiosk.StartKioskSession");
        Domain.Kiosk? kiosk = await GetAuthenticatedKioskAsync(context, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        if (kiosk.Mode != KioskMode.Active)
            return Results.Problem("Kiosk is not active.", statusCode: StatusCodes.Status409Conflict);

        if (string.IsNullOrWhiteSpace(kiosk.WorkflowDefinitionId))
            return Results.Problem("Kiosk has no assigned workflow.", statusCode: StatusCodes.Status409Conflict);

        DateTimeOffset now = timeProvider.GetUtcNow();
        KioskRuntimeLog.SessionStartRequested(logger, kiosk.Id, request.LanguageCode);
        KioskSession? existing = await db.Sessions
            .Where(session => session.KioskId == kiosk.Id && (session.Status == KioskSessionStatus.Starting || session.Status == KioskSessionStatus.Running))
            .OrderByDescending(session => session.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == KioskSessionStatus.Running && !string.IsNullOrWhiteSpace(existing.WorkflowInstanceId))
            {
                KioskRuntimeLog.ReusingRunningSession(logger, kiosk.Id, existing.Id, existing.WorkflowInstanceId);
                return Results.Ok(existing.ToResponse());
            }

            if (existing.Status == KioskSessionStatus.Starting && existing.StartedAt >= now.Subtract(SessionStartupTimeout))
            {
                KioskRuntimeLog.StartingSessionStillPending(logger, kiosk.Id, existing.Id, existing.StartedAt);
                return Results.Problem("Kiosk session is still starting.", statusCode: StatusCodes.Status409Conflict);
            }

            KioskRuntimeLog.RemovingStaleSession(logger, kiosk.Id, existing.Id, existing.Status, existing.WorkflowInstanceId);
            db.Sessions.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
        }

        KioskProfile profile = await db.Profiles.AsNoTracking().SingleAsync(profile => profile.Id == kiosk.ProfileId, cancellationToken);
        string languageCode = string.IsNullOrWhiteSpace(request.LanguageCode) ? profile.DefaultLanguageCode : request.LanguageCode;
        KioskSession session = KioskSession.Start(kiosk.Id, languageCode, now);
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        KioskRuntimeLog.SessionCreated(logger, kiosk.Id, session.Id, session.Status, languageCode);

        try
        {
            await workflowStarter.StartSessionWorkflowAsync(kiosk, session, cancellationToken);
            if (string.IsNullOrWhiteSpace(session.WorkflowInstanceId))
                throw new InvalidOperationException("Workflow instance was not assigned to kiosk session.");

            await kioskSagaService.StartAsync(session.Id, session.WorkflowInstanceId, cancellationToken);
            KioskRuntimeLog.SessionReady(logger, kiosk.Id, session.Id, session.WorkflowInstanceId);
        }
        catch (Exception exception)
        {
            KioskRuntimeLog.SessionStartFailed(logger, exception, kiosk.Id, session.Id);
            await kioskSagaService.CleanupAsync(session.Id, cancellationToken);
            db.Sessions.Remove(session);
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }

        return Results.Ok(session.ToResponse());
    }

    private static async Task<IResult> ChangeKioskLanguage([FromBody] ChangeKioskLanguageRequest request, HttpContext context, KioskDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        KioskSession? session = await GetActiveSessionAsync(context.User.GetKioskId(), db, cancellationToken);
        if (session is null)
            return Results.NotFound();

        session.ChangeLanguage(request.LanguageCode, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(session.ToResponse());
    }

    private static async Task<IResult> GetRuntimeState([FromQuery] int? sinceVersion, HttpContext context, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        Guid kioskId = context.User.GetKioskId();
        KioskSession? session = await GetLatestSessionAsync(kioskId, db, cancellationToken);
        if (session is null)
            return Results.NotFound();

        if (sinceVersion.HasValue && sinceVersion.Value >= session.CurrentInstructionVersion && (session.Status == KioskSessionStatus.Starting || session.Status == KioskSessionStatus.Running))
        {
            using var delay = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, delay.Token);
            while (!linked.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), linked.Token).ContinueWith(_ => { }, CancellationToken.None);
                await db.Entry(session).ReloadAsync(cancellationToken);
                if (session.CurrentInstructionVersion > sinceVersion.Value || (session.Status != KioskSessionStatus.Starting && session.Status != KioskSessionStatus.Running))
                    break;
            }
        }

        return Results.Ok(new KioskInstructionResponse(session.Id, session.Status, session.CurrentInstructionVersion, session.CurrentInstructionId, session.CurrentInstructionJson, session.TerminalTitle, session.TerminalMessage));
    }

    private static async Task<IResult> SubmitInstructionResponse(string instructionId, [FromBody] SubmitKioskInstructionResponseRequest request, HttpContext context, KioskDbContext db, KioskInstructionService instructionService, CancellationToken cancellationToken = default)
    {
        KioskSession? session = await GetActiveSessionAsync(context.User.GetKioskId(), db, cancellationToken);
        if (session is null)
            return Results.NotFound();

        try
        {
            await instructionService.SubmitInstructionAsync(session.Id, instructionId, request.Values, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status409Conflict);
        }

        await db.Entry(session).ReloadAsync(cancellationToken);
        return Results.Ok(session.ToResponse());
    }

    private static async Task<IResult> CancelCurrentSession([FromBody] CancelCurrentKioskSessionRequest? request, HttpContext context, KioskSessionCancellationService cancellationService, CancellationToken cancellationToken = default)
    {
        Guid kioskId = context.User.GetKioskId();
        KioskSessionCancellationSource source = request?.Source ?? KioskSessionCancellationSource.UserHome;
        KioskSession? session = await cancellationService.CancelActiveSessionAsync(kioskId, source, cancellationToken);
        if (session is null)
            return Results.NotFound();
        return Results.Ok(session.ToResponse());
    }

    private static async Task<IResult> GetKioskAsset(string assetName, [FromQuery] string? languageCode, HttpContext context, KioskDbContext db, IKioskAssetStorage storage, CancellationToken cancellationToken = default)
    {
        Domain.Kiosk? kiosk = await db.Kiosks.AsNoTracking().SingleOrDefaultAsync(kiosk => kiosk.Id == context.User.GetKioskId(), cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        KioskProfile profile = await db.Profiles.AsNoTracking().SingleAsync(profile => profile.Id == kiosk.ProfileId, cancellationToken);
        string resolvedLanguage = string.IsNullOrWhiteSpace(languageCode) ? profile.DefaultLanguageCode : languageCode;
        KioskAsset? asset = await db.Assets.AsNoTracking()
            .Where(asset => asset.ProfileId == profile.Id && asset.Name == assetName && (asset.LanguageCode == resolvedLanguage || asset.LanguageCode == profile.DefaultLanguageCode || asset.LanguageCode == null))
            .OrderByDescending(asset => asset.LanguageCode == resolvedLanguage)
            .ThenByDescending(asset => asset.LanguageCode == profile.DefaultLanguageCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (asset is null)
            return Results.NotFound();

        Stream? stream = await storage.OpenReadAsync(asset.RelativePath, cancellationToken);
        return stream is null ? Results.NotFound() : Results.File(stream, asset.ContentType, asset.FileName);
    }

    private static async Task<Domain.Kiosk?> GetAuthenticatedKioskAsync(HttpContext context, KioskDbContext db, CancellationToken cancellationToken) =>
        await db.Kiosks.SingleOrDefaultAsync(kiosk => kiosk.Id == context.User.GetKioskId(), cancellationToken);

    private static async Task<KioskSession?> GetActiveSessionAsync(Guid kioskId, KioskDbContext db, CancellationToken cancellationToken) =>
        await db.Sessions.Where(session => session.KioskId == kioskId && (session.Status == KioskSessionStatus.Starting || session.Status == KioskSessionStatus.Running)).OrderByDescending(session => session.StartedAt).FirstOrDefaultAsync(cancellationToken);

    private static async Task<KioskSession?> GetLatestSessionAsync(Guid kioskId, KioskDbContext db, CancellationToken cancellationToken) =>
        await db.Sessions
            .Where(session => session.KioskId == kioskId)
            .OrderByDescending(session => session.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private static Guid GetKioskId(this ClaimsPrincipal principal) => Guid.Parse(principal.FindFirstValue(KioskAuthenticationDefaults.KioskIdClaim)!);
}

internal static partial class KioskRuntimeLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Kiosk {KioskId} requested session start with language {LanguageCode}")]
    public static partial void SessionStartRequested(this ILogger logger, Guid kioskId, string? languageCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Reusing running session {SessionId} for kiosk {KioskId} with workflow instance {WorkflowInstanceId}")]
    public static partial void ReusingRunningSession(this ILogger logger, Guid kioskId, Guid sessionId, string workflowInstanceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Kiosk {KioskId} still has starting session {SessionId} created at {StartedAt}")]
    public static partial void StartingSessionStillPending(this ILogger logger, Guid kioskId, Guid sessionId, DateTimeOffset startedAt);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Removing stale kiosk session {SessionId} for kiosk {KioskId} with status {Status} and workflow instance {WorkflowInstanceId}")]
    public static partial void RemovingStaleSession(this ILogger logger, Guid kioskId, Guid sessionId, KioskSessionStatus status, string? workflowInstanceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created kiosk session {SessionId} for kiosk {KioskId} in status {Status} with language {LanguageCode}")]
    public static partial void SessionCreated(this ILogger logger, Guid kioskId, Guid sessionId, KioskSessionStatus status, string languageCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Kiosk session {SessionId} for kiosk {KioskId} is ready with workflow instance {WorkflowInstanceId}")]
    public static partial void SessionReady(this ILogger logger, Guid kioskId, Guid sessionId, string workflowInstanceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start session {SessionId} for kiosk {KioskId}")]
    public static partial void SessionStartFailed(this ILogger logger, Exception exception, Guid kioskId, Guid sessionId);
}
