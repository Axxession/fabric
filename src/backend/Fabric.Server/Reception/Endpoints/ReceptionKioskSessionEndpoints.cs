using System.Security.Claims;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Contracts;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Endpoints;

public static class ReceptionKioskSessionEndpoints
{
    public static IEndpointRouteBuilder MapReceptionKioskSessionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder sessions = app.MapGroup("/api/reception/kiosk/sessions")
            .RequireAuthorization(ReceptionKioskAuthenticationDefaults.Policy);

        sessions.MapPost("", StartReceptionKioskSession)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sessions.MapGet("/current", GetCurrentReceptionKioskSession)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        sessions.MapPost("/current/next", AdvanceReceptionKioskSession)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sessions.MapPost("/current/stop", StopReceptionKioskSession)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        sessions.MapPost("/current/face-picture/store", StoreReceptionKioskFacePicture)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sessions.MapPost("/current/identity-document/store", StoreReceptionKioskIdentityDocument)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sessions.MapGet("/current/compliance", GetCurrentReceptionKioskCompliance)
            .Produces<ReceptionKioskComplianceResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        sessions.MapPost("/current/compliance/requirements/{requirementDefinitionId:guid}/launch", LaunchReceptionKioskComplianceCourse)
            .Produces<ReceptionKioskComplianceCourseLaunchResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        sessions.MapPost("/current/compliance/non-compliant", MarkCurrentReceptionKioskNonCompliant)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        sessions.MapPost("/current/finalize", FinalizeReceptionKioskSession)
            .Produces<ReceptionKioskSessionResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> StartReceptionKioskSession(
        [FromBody] StartReceptionKioskSessionRequest request,
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.StartAsync(kiosk, request.Code, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> GetCurrentReceptionKioskSession(
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.GetCurrentAsync(kiosk, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> AdvanceReceptionKioskSession(
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.AdvanceAsync(kiosk, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> StopReceptionKioskSession(
        [FromBody] StopReceptionKioskSessionRequest request,
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.StopAsync(kiosk, request.Reason, request.Message, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> StoreReceptionKioskFacePicture(
        [FromBody] StoreReceptionKioskSessionCaptureRequest request,
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.StoreFacePictureAsync(kiosk, request.Content, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> StoreReceptionKioskIdentityDocument(
        [FromBody] StoreReceptionKioskSessionCaptureRequest request,
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.StoreIdentityDocumentAsync(kiosk, request.Content, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> GetCurrentReceptionKioskCompliance(
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskComplianceResponse, ReceptionKioskSessionErrors> result = await service.GetComplianceAsync(kiosk, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> LaunchReceptionKioskComplianceCourse(
        Guid requirementDefinitionId,
        [FromBody] ReceptionKioskComplianceCourseLaunchRequest request,
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskComplianceCourseLaunchResponse, ReceptionKioskSessionErrors> result = await service.LaunchComplianceCourseAsync(kiosk, requirementDefinitionId, request.LanguageId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> MarkCurrentReceptionKioskNonCompliant(
        [FromBody] MarkReceptionKioskSessionNonCompliantRequest request,
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.MarkNonCompliantAsync(kiosk, request.Message, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> FinalizeReceptionKioskSession(
        HttpContext httpContext,
        ReceptionDbContext db,
        ReceptionKioskSessionService service,
        CancellationToken cancellationToken = default)
    {
        ReceptionKiosk? kiosk = await GetAuthenticatedKioskAsync(httpContext.User, db, cancellationToken);
        if (kiosk is null)
            return Results.NotFound();

        Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> result = await service.FinalizeAsync(kiosk, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<ReceptionKiosk?> GetAuthenticatedKioskAsync(ClaimsPrincipal principal, ReceptionDbContext db, CancellationToken cancellationToken) =>
        await db.ReceptionKiosks.SingleOrDefaultAsync(kiosk => kiosk.Id == Guid.Parse(principal.FindFirstValue(ReceptionKioskAuthenticationDefaults.KioskIdClaim)!), cancellationToken);

    private static (int statusCode, ProblemDetails? problemDetails) MapError(ReceptionKioskSessionErrors error) => error switch
    {
        ReceptionKioskSessionErrors.SessionNotFound => Problem(StatusCodes.Status404NotFound, "Reception kiosk session not found."),
        ReceptionKioskSessionErrors.ArrivalNotFound => Problem(StatusCodes.Status404NotFound, "Arrival not found."),
        ReceptionKioskSessionErrors.ArrivalAssignedToDifferentLocation => Problem(StatusCodes.Status409Conflict, "This kiosk cannot serve your location."),
        ReceptionKioskSessionErrors.ArrivalOutsideKioskOnboardingWindow => Problem(StatusCodes.Status409Conflict, "Arrival is outside kiosk onboarding window."),
        ReceptionKioskSessionErrors.SubjectAlreadyHasOnboardedArrival => Problem(StatusCodes.Status409Conflict, "Subject already has another onboarded arrival."),
        ReceptionKioskSessionErrors.InvalidArrivalStatus => Problem(StatusCodes.Status409Conflict, "Arrival status does not allow this operation."),
        ReceptionKioskSessionErrors.FacePictureMissing => Problem(StatusCodes.Status409Conflict, "Face picture is required before continuing."),
        ReceptionKioskSessionErrors.IdentityDocumentMissing => Problem(StatusCodes.Status409Conflict, "Identity document picture is required before continuing."),
        ReceptionKioskSessionErrors.ComplianceNotSatisfied => Problem(StatusCodes.Status409Conflict, "Compliance is not satisfied."),
        ReceptionKioskSessionErrors.FinalizationNotReady => Problem(StatusCodes.Status409Conflict, "The onboarding session is not ready to finalize."),
        ReceptionKioskSessionErrors.InvalidCurrentStep => Problem(StatusCodes.Status409Conflict, "This action is not allowed for the current step."),
        ReceptionKioskSessionErrors.SessionNotActive => Problem(StatusCodes.Status409Conflict, "Reception kiosk session is not active."),
        ReceptionKioskSessionErrors.CourseNotAvailable => Problem(StatusCodes.Status409Conflict, "No course is available for this requirement."),
        ReceptionKioskSessionErrors.LanguageNotAvailable => Problem(StatusCodes.Status409Conflict, "The selected language is not available for this course."),
        ReceptionKioskSessionErrors.MissingIdentity => Problem(StatusCodes.Status409Conflict, "No linked identity is available for this arrival."),
        ReceptionKioskSessionErrors.StorageItemMissing => Problem(StatusCodes.Status409Conflict, "Stored session data is missing."),
        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected reception kiosk session error."),
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
