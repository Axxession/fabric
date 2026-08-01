using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Storage;
using Fabric.Server.Kiosk.Application;
using Fabric.Server.Kiosk.Contracts;
using Fabric.Server.Kiosk.Domain;
using Fabric.Server.Kiosk.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Kiosk.Endpoints;

public static class KioskProfileEndpoints
{
    public static IEndpointRouteBuilder MapKioskProfileEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder profiles = app.MapGroup("/api/kiosk-profiles");

        profiles.MapGet("", ListKioskProfiles).Produces<Page<KioskProfileResponse>>();
        profiles.MapPost("", CreateKioskProfile).Produces<KioskProfileResponse>(StatusCodes.Status201Created);
        profiles.MapGet("/{id:guid}", GetKioskProfile).Produces<KioskProfileResponse>().Produces(StatusCodes.Status404NotFound);
        profiles.MapPut("/{id:guid}", UpdateKioskProfile).Produces<KioskProfileResponse>().Produces(StatusCodes.Status404NotFound);
        profiles.MapDelete("/{id:guid}", DeleteKioskProfile).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound).Produces(StatusCodes.Status409Conflict);
        profiles.MapGet("/{id:guid}/languages", ListKioskProfileLanguages).Produces<KioskProfileLanguageResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapPut("/{id:guid}/languages", UpsertKioskProfileLanguages).Produces<KioskProfileLanguageResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapGet("/{id:guid}/translations", ListKioskTranslations).Produces<KioskTranslationResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapPut("/{id:guid}/translations", UpsertKioskTranslations).Produces<KioskTranslationResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapGet("/{id:guid}/theme", ListKioskTheme).Produces<KioskThemeTokenResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapPut("/{id:guid}/theme", UpsertKioskTheme).Produces<KioskThemeTokenResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapGet("/{id:guid}/welcome", GetKioskWelcomeSettings).Produces<KioskWelcomeSettingsResponse>().Produces(StatusCodes.Status404NotFound);
        profiles.MapPut("/{id:guid}/welcome", UpsertKioskWelcomeSettings).Produces<KioskWelcomeSettingsResponse>().Produces(StatusCodes.Status404NotFound);
        profiles.MapGet("/{id:guid}/hardware-bindings", ListKioskHardwareBindings).Produces<KioskHardwareBindingResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapPut("/{id:guid}/hardware-bindings", UpsertKioskHardwareBindings).Produces<KioskHardwareBindingResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapGet("/{id:guid}/assets", ListKioskAssets).Produces<KioskAssetResponse[]>().Produces(StatusCodes.Status404NotFound);
        profiles.MapPost("/{id:guid}/assets", CreateKioskAsset).DisableAntiforgery().Produces<KioskAssetResponse>(StatusCodes.Status201Created).Produces(StatusCodes.Status404NotFound);
        profiles.MapGet("/{id:guid}/assets/{assetId:guid}/content", GetKioskAssetContent).Produces(StatusCodes.Status404NotFound);
        profiles.MapDelete("/{id:guid}/assets/{assetId:guid}", DeleteKioskAsset).Produces(StatusCodes.Status204NoContent).Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListKioskProfiles([AsParameters] BaseListRequest request, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<KioskProfile> result = await db.Profiles.AsNoTracking().OrderBy(profile => profile.Name).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(profile => profile.ToResponse()));
    }

    private static async Task<IResult> CreateKioskProfile([FromBody] CreateKioskProfileRequest request, KioskDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        KioskProfile profile = KioskProfile.Create(request.Name, request.DefaultLanguageCode, timeProvider.GetUtcNow());
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/kiosk-profiles/{profile.Id}", profile.ToResponse());
    }

    private static async Task<IResult> GetKioskProfile(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        KioskProfile? profile = await db.Profiles.AsNoTracking().SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);
        return profile is null ? Results.NotFound() : Results.Ok(profile.ToResponse());
    }

    private static async Task<IResult> UpdateKioskProfile(Guid id, [FromBody] UpdateKioskProfileRequest request, KioskDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        KioskProfile? profile = await db.Profiles.SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);
        if (profile is null)
            return Results.NotFound();

        profile.Update(request.Name, request.DefaultLanguageCode, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(profile.ToResponse());
    }

    private static async Task<IResult> DeleteKioskProfile(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        KioskProfile? profile = await db.Profiles.SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);
        if (profile is null)
            return Results.NotFound();

        bool used = await db.Kiosks.AnyAsync(kiosk => kiosk.ProfileId == id, cancellationToken);
        if (used)
            return Results.Problem("Kiosk profile is used by one or more kiosks.", statusCode: StatusCodes.Status409Conflict);

        db.Profiles.Remove(profile);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> UpsertKioskProfileLanguages(Guid id, [FromBody] UpsertKioskProfileLanguagesRequest request, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        await db.Languages.Where(language => language.ProfileId == id).ExecuteDeleteAsync(cancellationToken);
        KioskProfileLanguage[] languages = request.Languages.Select(language => KioskProfileLanguage.Create(id, language.LanguageCode, language.DisplayName, language.IsDefault, language.SortOrder)).ToArray();
        db.Languages.AddRange(languages);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(languages.Select(language => language.ToResponse()).ToArray());
    }

    private static async Task<IResult> ListKioskProfileLanguages(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        KioskProfileLanguage[] languages = await db.Languages.AsNoTracking().Where(language => language.ProfileId == id).OrderBy(language => language.SortOrder).ToArrayAsync(cancellationToken);
        return Results.Ok(languages.Select(language => language.ToResponse()).ToArray());
    }

    private static async Task<IResult> UpsertKioskTranslations(Guid id, [FromBody] UpsertKioskTranslationsRequest request, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        await db.Translations.Where(translation => translation.ProfileId == id).ExecuteDeleteAsync(cancellationToken);
        KioskTranslation[] translations = request.Translations.Select(translation => KioskTranslation.Create(id, translation.LanguageCode, translation.Key, translation.Value)).ToArray();
        db.Translations.AddRange(translations);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(translations.Select(translation => translation.ToResponse()).ToArray());
    }

    private static async Task<IResult> ListKioskTranslations(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        KioskTranslation[] translations = await db.Translations.AsNoTracking().Where(translation => translation.ProfileId == id).OrderBy(translation => translation.LanguageCode).ThenBy(translation => translation.Key).ToArrayAsync(cancellationToken);
        return Results.Ok(translations.Select(translation => translation.ToResponse()).ToArray());
    }

    private static async Task<IResult> UpsertKioskTheme(Guid id, [FromBody] UpsertKioskThemeRequest request, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        await db.ThemeTokens.Where(token => token.ProfileId == id).ExecuteDeleteAsync(cancellationToken);
        KioskThemeToken[] tokens = request.Tokens.Select(token => KioskThemeToken.Create(id, token.Key, token.Value)).ToArray();
        db.ThemeTokens.AddRange(tokens);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(tokens.Select(token => token.ToResponse()).ToArray());
    }

    private static async Task<IResult> ListKioskTheme(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        KioskThemeToken[] tokens = await db.ThemeTokens.AsNoTracking().Where(token => token.ProfileId == id).OrderBy(token => token.Key).ToArrayAsync(cancellationToken);
        return Results.Ok(tokens.Select(token => token.ToResponse()).ToArray());
    }

    private static async Task<IResult> UpsertKioskWelcomeSettings(Guid id, [FromBody] UpsertKioskWelcomeSettingsRequest request, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        await db.WelcomeSettings.Where(settings => settings.ProfileId == id).ExecuteDeleteAsync(cancellationToken);
        KioskWelcomeSettings settings = KioskWelcomeSettings.Create(id, request.TitleKey, request.SubtitleKey, request.StartButtonKey, request.BackgroundAssetName, request.LogoAssetName);
        db.WelcomeSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(settings.ToResponse());
    }

    private static async Task<IResult> GetKioskWelcomeSettings(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        KioskWelcomeSettings? settings = await db.WelcomeSettings.AsNoTracking().SingleOrDefaultAsync(settings => settings.ProfileId == id, cancellationToken);
        return settings is null ? Results.NotFound() : Results.Ok(settings.ToResponse());
    }

    private static async Task<IResult> UpsertKioskHardwareBindings(Guid id, [FromBody] UpsertKioskHardwareBindingsRequest request, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        await db.HardwareBindings.Where(binding => binding.ProfileId == id).ExecuteDeleteAsync(cancellationToken);
        KioskHardwareBinding[] bindings = request.Bindings.Select(binding => KioskHardwareBinding.Create(id, binding.BindingKey, binding.DisplayName, binding.RequiredCapability, binding.Required, binding.CleanupOnSessionEnd, binding.SortOrder)).ToArray();
        db.HardwareBindings.AddRange(bindings);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(bindings.Select(binding => binding.ToResponse()).ToArray());
    }

    private static async Task<IResult> ListKioskHardwareBindings(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        KioskHardwareBinding[] bindings = await db.HardwareBindings.AsNoTracking().Where(binding => binding.ProfileId == id).OrderBy(binding => binding.SortOrder).ToArrayAsync(cancellationToken);
        return Results.Ok(bindings.Select(binding => binding.ToResponse()).ToArray());
    }

    private static async Task<IResult> ListKioskAssets(Guid id, KioskDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        KioskAsset[] assets = await db.Assets.AsNoTracking().Where(asset => asset.ProfileId == id).OrderBy(asset => asset.Name).ToArrayAsync(cancellationToken);
        return Results.Ok(assets.Select(asset => asset.ToResponse()).ToArray());
    }

    private static async Task<IResult> CreateKioskAsset(Guid id, [FromForm] string name, [FromForm] string? languageCode, [FromForm] KioskAssetKind kind, [FromForm] StoredFileVisibility visibility, [FromForm] string? altTextKey, [FromForm] IFormFile file, HttpContext context, KioskDbContext db, IKioskAssetStorage storage, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        if (!await db.Profiles.AnyAsync(profile => profile.Id == id, cancellationToken))
            return Results.NotFound();

        Guid assetId = Guid.NewGuid();
        StorageClaimSnapshot uploadedBy = StorageClaimSnapshot.FromPrincipal(context.User);
        string relativePath = await storage.SaveAsync(id, assetId, file.FileName, file.ContentType, file.OpenReadStream(), cancellationToken);
        KioskAsset asset = KioskAsset.Create(assetId, id, name, languageCode, kind, file.FileName, file.ContentType, file.Length, relativePath, visibility, uploadedBy.Oid, uploadedBy.Email, uploadedBy.DisplayName, altTextKey, timeProvider.GetUtcNow());
        db.Assets.Add(asset);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/kiosk-profiles/{id}/assets/{asset.Id}", asset.ToResponse());
    }

    private static async Task<IResult> GetKioskAssetContent(Guid id, Guid assetId, KioskDbContext db, IKioskAssetStorage storage, CancellationToken cancellationToken = default)
    {
        KioskAsset? asset = await db.Assets.AsNoTracking().SingleOrDefaultAsync(asset => asset.ProfileId == id && asset.Id == assetId, cancellationToken);
        if (asset is null)
            return Results.NotFound();

        Stream? stream = await storage.OpenReadAsync(asset.RelativePath, cancellationToken);
        return stream is null ? Results.NotFound() : Results.File(stream, asset.ContentType, asset.FileName);
    }

    private static async Task<IResult> DeleteKioskAsset(Guid id, Guid assetId, KioskDbContext db, IKioskAssetStorage storage, CancellationToken cancellationToken = default)
    {
        KioskAsset? asset = await db.Assets.SingleOrDefaultAsync(asset => asset.ProfileId == id && asset.Id == assetId, cancellationToken);
        if (asset is null)
            return Results.NotFound();

        db.Assets.Remove(asset);
        await db.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(asset.RelativePath, cancellationToken);
        return Results.NoContent();
    }
}
