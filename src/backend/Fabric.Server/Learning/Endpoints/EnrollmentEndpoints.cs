using Fabric.Server.Actors.Application;
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

public static class EnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder enrollments = app.MapGroup("/api/learning/enrollments")
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole });

        enrollments.MapGet("", ListEnrollments).Produces<Page<EnrollmentResponse>>();
        enrollments.MapGet("/{id:guid}", GetEnrollment).Produces<EnrollmentResponse>().Produces(StatusCodes.Status404NotFound);
        enrollments.MapGet("/{id:guid}/attempts", ListAttempts).Produces<Page<AttemptResponse>>().Produces(StatusCodes.Status404NotFound);
        enrollments.MapPost("", CreateEnrollment).Produces<EnrollmentResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        enrollments.MapPost("/upsert", UpsertEnrollment).Produces<EnrollmentResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        enrollments.MapPost("/{id:guid}/cancel", CancelEnrollment).Produces<EnrollmentResponse>().Produces<ProblemDetails>(StatusCodes.Status403Forbidden).Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListEnrollments([AsParameters] ListEnrollmentsRequest request, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<Enrollment> query = db.Enrollments.AsNoTracking();
        if (request.CourseId.HasValue)
            query = query.Where(item => item.CourseId == request.CourseId.Value);
        if (request.IdentityId.HasValue)
            query = query.Where(item => item.IdentityId == request.IdentityId.Value);
        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<Enrollment> page = await query.OrderByDescending(item => item.AssignedAt).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetEnrollment(Guid id, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        Enrollment? enrollment = await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return enrollment is null ? Results.NotFound() : Results.Ok(enrollment.ToResponse());
    }

    private static async Task<IResult> ListAttempts(Guid id, [AsParameters] BaseListRequest request, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Enrollments.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
            return Results.NotFound();

        IQueryable<Attempt> query = db.Attempts.AsNoTracking().Where(item => item.EnrollmentId == id);
        IPaged<Attempt> page = await query.OrderByDescending(item => item.StartedAt).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateEnrollment([FromBody] CreateEnrollmentRequest request, EnrollmentService service, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentityResult = await LearningAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentityResult.IsFailure(out IResult actorFailure))
            return actorFailure;

        actorIdentityResult.IsSuccess(out Guid assignedByIdentityId);
        Result<Enrollment, EnrollmentErrors> result = await service.CreateEnrollmentAsync(request, assignedByIdentityId, cancellationToken);
        return result.Match<IResult>(enrollment => Results.Created($"/api/learning/enrollments/{enrollment.Id}", enrollment.ToResponse()), _ => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpsertEnrollment([FromBody] CreateEnrollmentRequest request, EnrollmentService service, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentityResult = await LearningAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentityResult.IsFailure(out IResult actorFailure))
            return actorFailure;

        actorIdentityResult.IsSuccess(out Guid assignedByIdentityId);
        Result<Enrollment, EnrollmentErrors> result = await service.UpsertEnrollmentAsync(request, assignedByIdentityId, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> CancelEnrollment(Guid id, [FromBody] CancelEnrollmentRequest request, EnrollmentService service, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentityResult = await LearningAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentityResult.IsFailure(out IResult actorFailure))
            return actorFailure;

        actorIdentityResult.IsSuccess(out Guid identityId);
        Result<Enrollment, EnrollmentErrors> result = await service.CancelEnrollmentAsync(id, identityId, request.Reason, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(EnrollmentErrors error) => error switch
    {
        EnrollmentErrors.EnrollmentNotFound => Problem(StatusCodes.Status404NotFound, "Enrollment not found."),
        EnrollmentErrors.CourseNotFound => Problem(StatusCodes.Status404NotFound, "Course not found."),
        EnrollmentErrors.IdentityNotFound => Problem(StatusCodes.Status404NotFound, "Identity not found."),
        EnrollmentErrors.ActiveEnrollmentAlreadyExists => Problem(StatusCodes.Status409Conflict, "An active enrollment already exists for this learner and course."),
        EnrollmentErrors.EnrollmentAlreadyCompleted => Problem(StatusCodes.Status409Conflict, "Enrollment is already completed."),
        EnrollmentErrors.EnrollmentAlreadyCancelled => Problem(StatusCodes.Status409Conflict, "Enrollment is already cancelled."),
        EnrollmentErrors.EnrollmentNotActive => Problem(StatusCodes.Status409Conflict, "Enrollment is not active."),
        _ => Problem(StatusCodes.Status400BadRequest, "Enrollment request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
