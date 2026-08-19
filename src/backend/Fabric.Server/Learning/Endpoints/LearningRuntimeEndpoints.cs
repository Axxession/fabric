using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Learning.Endpoints;

public static class LearningRuntimeEndpoints
{
    public static IEndpointRouteBuilder MapLearningRuntimeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder runtime = app.MapGroup("/api/learning/runtime");

        runtime.MapPost("/sessions", StartLaunchSession)
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole })
            .Produces<StartLaunchSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        runtime.MapGet("/sessions/{token}", GetLaunchSessionBootstrap)
            .AllowAnonymous()
            .Produces<LaunchSessionBootstrapResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status410Gone);

        runtime.MapGet("/progress", LoadProgress)
            .AllowAnonymous()
            .Produces<ScormProgressResponse>()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status410Gone);

        runtime.MapPost("/progress", RecordProgress)
            .AllowAnonymous()
            .Produces<ScormProgressResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status410Gone);

        runtime.MapGet("/content/{token}/{**path}", GetContent)
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status410Gone);

        return app;
    }

    private static async Task<IResult> StartLaunchSession([FromBody] StartLaunchSessionRequest request, LearningRuntimeService service, CancellationToken cancellationToken = default)
    {
        Result<LaunchSession, EnrollmentErrors> result = await service.CreateLaunchSessionAsync(request.EnrollmentId, request.LanguageId, request.ScoId, cancellationToken);
        return result.Map(session => new StartLaunchSessionResponse(session.Token)).AsResponse(MapError);
    }

    private static async Task<IResult> GetLaunchSessionBootstrap(string token, HttpContext httpContext, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        LaunchSession? session = await db.LaunchSessions.AsNoTracking().SingleOrDefaultAsync(item => item.Token == token, cancellationToken);
        if (session is null)
            return MapFailure(EnrollmentErrors.LaunchSessionTokenInvalid);
        if (session.IsExpired(TimeProvider.System.GetUtcNow()))
            return MapFailure(EnrollmentErrors.LaunchSessionExpired);

        CourseVersion? version = await db.CourseVersions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == session.CourseVersionId, cancellationToken);
        if (version is null)
            return MapFailure(EnrollmentErrors.CourseVersionNotFound);

        CourseSco[] scos = await db.CourseScos.AsNoTracking()
            .Where(item => item.CourseVersionId == version.Id)
            .OrderBy(item => item.ManifestOrder)
            .ToArrayAsync(cancellationToken);

        CourseSco? activeSco = session.ScoId.HasValue ? scos.SingleOrDefault(item => item.Id == session.ScoId.Value) : scos.FirstOrDefault();
        if (activeSco is null)
            return MapFailure(EnrollmentErrors.CourseVersionNotFound);

        string contentBaseUrl = $"/api/learning/runtime/content/{Uri.EscapeDataString(token)}";
        LaunchSessionBootstrapResponse response = new(
            session.EnrollmentId,
            session.CourseId,
            session.CourseVersionId,
            version.CourseLanguageId,
            version.ScormVersion,
            session.AttemptId,
            activeSco.Id,
            contentBaseUrl,
            activeSco.LaunchUrl,
            session.ExpiresAt,
            scos.Select(item => item.ToResponse()).ToArray());

        return Results.Ok(response);
    }

    private static async Task<IResult> LoadProgress([FromQuery] string token, [FromQuery] Guid? scoId, LearningRuntimeService service, CancellationToken cancellationToken = default)
    {
        Result<ScormProgress?, EnrollmentErrors> result = await service.LoadProgressAsync(token, scoId, cancellationToken);
        if (result.IsFailure(out EnrollmentErrors error))
            return MapFailure(error);

        result.IsSuccess(out ScormProgress? progress);
        return progress is null ? Results.NoContent() : Results.Ok(progress.ToResponse());
    }

    private static async Task<IResult> RecordProgress([FromBody] RecordScormProgressRequest request, LearningRuntimeService service, CancellationToken cancellationToken = default)
    {
        Result<ScormProgress, EnrollmentErrors> result = await service.RecordProgressAsync(request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> GetContent(string token, string path, LearningDbContext db, ILearningPackageStorage packageStorage, CancellationToken cancellationToken = default)
    {
        LaunchSession? session = await db.LaunchSessions.AsNoTracking().SingleOrDefaultAsync(item => item.Token == token, cancellationToken);
        if (session is null)
            return MapFailure(EnrollmentErrors.LaunchSessionTokenInvalid);
        if (session.IsExpired(TimeProvider.System.GetUtcNow()))
            return MapFailure(EnrollmentErrors.LaunchSessionExpired);

        CourseVersion? version = await db.CourseVersions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == session.CourseVersionId, cancellationToken);
        if (version is null)
            return MapFailure(EnrollmentErrors.CourseVersionNotFound);

        Stream? stream;
        try
        {
            stream = await packageStorage.OpenReadAsync(version.StoragePath, path, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Results.NotFound();
        }

        if (stream is null)
            return Results.NotFound();

        return Results.File(stream, ResolveContentType(path));
    }

    private static string ResolveContentType(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream"
        };
    }

    private static IResult MapFailure(EnrollmentErrors error)
    {
        (int statusCode, ProblemDetails? problemDetails) = MapError(error);
        return problemDetails is null ? Results.StatusCode(statusCode) : Results.Problem(problemDetails.Detail, statusCode: statusCode);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(EnrollmentErrors error) => error switch
    {
        EnrollmentErrors.EnrollmentNotFound => Problem(StatusCodes.Status404NotFound, "Enrollment not found."),
        EnrollmentErrors.EnrollmentNotActive => Problem(StatusCodes.Status409Conflict, "Enrollment is not active."),
        EnrollmentErrors.CourseVersionNotFound => Problem(StatusCodes.Status404NotFound, "Course version not found for selected language."),
        EnrollmentErrors.LaunchSessionTokenInvalid => Problem(StatusCodes.Status404NotFound, "Launch session token is invalid."),
        EnrollmentErrors.LaunchSessionExpired => Problem(StatusCodes.Status410Gone, "Launch session has expired."),
        _ => Problem(StatusCodes.Status400BadRequest, "Learning runtime request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
