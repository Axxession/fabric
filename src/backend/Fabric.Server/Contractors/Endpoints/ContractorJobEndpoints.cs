using Fabric.Server.Contractors.Application;
using Fabric.Server.Actors.Application;
using Fabric.Server.Contractors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors.Endpoints;

public static class ContractorJobEndpoints
{
    public static IEndpointRouteBuilder MapContractorJobEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder jobs = app.MapGroup("/api/contractors/jobs")
            .RequireAuthorization(FabricRoleDefaults.ContractorPlanningPolicy);

        jobs.MapGet("", ListContractorJobs)
            .WithSummary("List contractor jobs")
            .Produces<Page<ContractorJobResponse>>();
        jobs.MapGet("/{id:guid}", GetContractorJob)
            .WithSummary("Get contractor job")
            .Produces<ContractorJobResponse>()
            .Produces(StatusCodes.Status404NotFound);
        jobs.MapPost("", CreateContractorJob)
            .WithSummary("Create contractor job")
            .Produces<ContractorJobResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        jobs.MapPut("/{id:guid}", UpdateContractorJob)
            .WithSummary("Update contractor job")
            .Produces<ContractorJobResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        jobs.MapPost("/{id:guid}/activate", ActivateContractorJob)
            .WithSummary("Activate contractor job")
            .Produces<ContractorJobResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        jobs.MapPost("/{id:guid}/complete", CompleteContractorJob)
            .WithSummary("Complete contractor job")
            .Produces<ContractorJobResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        jobs.MapPost("/{id:guid}/cancel", CancelContractorJob)
            .WithSummary("Cancel contractor job")
            .Produces<ContractorJobResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListContractorJobs(
        [AsParameters] ListContractorJobsRequest request,
        [FromQuery] Guid[]? ids,
        ContractorsDbContext db,
        CurrentActorService currentActorService,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        IQueryable<ContractorJob> query = db.ContractorJobs.AsNoTracking().Include(item => item.Assignments);
        query = query.Where(item => item.CreatedByIdentityId == currentIdentityId);

        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        if (request.CompanyId.HasValue)
            query = query.Where(item => item.CompanyId == request.CompanyId.Value);

        if (request.JobTypeId.HasValue)
            query = query.Where(item => item.JobTypeId == request.JobTypeId.Value);

        if (request.LocationId.HasValue)
            query = query.Where(item => item.LocationId == request.LocationId.Value);

        if (request.Status is { Length: > 0 })
            query = query.Where(item => request.Status.Contains(item.Status));

        if (request.PlannedStartAfter.HasValue)
            query = query.Where(item => item.PlannedStart >= request.PlannedStartAfter.Value);

        if (request.PlannedEndBefore.HasValue)
            query = query.Where(item => item.PlannedEnd <= request.PlannedEndBefore.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Name, filter)
                || item.Description != null && EF.Functions.ILike(item.Description, filter));
        }

        IPaged<ContractorJob> result = await query.OrderByDescending(item => item.PlannedStart).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetContractorJob(Guid id, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        ContractorJob? job = await db.ContractorJobs.AsNoTracking().Include(item => item.Assignments).SingleOrDefaultAsync(item => item.Id == id && item.CreatedByIdentityId == currentIdentityId, cancellationToken);
        return job is null ? Results.NotFound() : Results.Ok(job.ToResponse());
    }

    private static async Task<IResult> CreateContractorJob([FromBody] CreateContractorJobRequest request, ContractorsService service, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        Result<ContractorJob, ContractorJobErrors> result = await service.CreateContractorJobAsync(request, currentIdentityId, cancellationToken);
        return result.Match<IResult>(
            job => Results.Created($"/api/contractors/jobs/{job.Id}", job.ToResponse()),
            _ => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpdateContractorJob(Guid id, [FromBody] UpdateContractorJobRequest request, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, id, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJob, ContractorJobErrors> result = await service.UpdateContractorJobAsync(id, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ActivateContractorJob(Guid id, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, id, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJob, ContractorJobErrors> result = await service.ActivateContractorJobAsync(id, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> CompleteContractorJob(Guid id, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, id, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJob, ContractorJobErrors> result = await service.CompleteContractorJobAsync(id, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> CancelContractorJob(Guid id, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, id, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJob, ContractorJobErrors> result = await service.CancelContractorJobAsync(id, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    internal static (int statusCode, ProblemDetails? problemDetails) MapError(ContractorJobErrors error) =>
        error switch
        {
            ContractorJobErrors.ContractorJobNotFound => Problem(StatusCodes.Status404NotFound, "Contractor job not found."),
            ContractorJobErrors.AssignmentNotFound => Problem(StatusCodes.Status404NotFound, "Contractor job assignment not found."),
            ContractorJobErrors.CompanyNotFound => Problem(StatusCodes.Status404NotFound, "Company not found."),
            ContractorJobErrors.JobTypeNotFound => Problem(StatusCodes.Status404NotFound, "Job type not found."),
            ContractorJobErrors.LocationNotFound => Problem(StatusCodes.Status404NotFound, "Location not found."),
            ContractorJobErrors.ContractorNotFound => Problem(StatusCodes.Status404NotFound, "Contractor not found."),
            ContractorJobErrors.ContractorCompanyMismatch => Problem(StatusCodes.Status409Conflict, "Contractor belongs to a different company than the job."),
            ContractorJobErrors.ContractorJobAlreadyActive => Problem(StatusCodes.Status409Conflict, "Contractor job is already active."),
            ContractorJobErrors.ContractorJobCompleted => Problem(StatusCodes.Status409Conflict, "Contractor job is completed."),
            ContractorJobErrors.ContractorJobCancelled => Problem(StatusCodes.Status409Conflict, "Contractor job is cancelled."),
            ContractorJobErrors.AssignmentAlreadyActive => Problem(StatusCodes.Status409Conflict, "Contractor job assignment is already active."),
            ContractorJobErrors.AssignmentCompleted => Problem(StatusCodes.Status409Conflict, "Contractor job assignment is completed."),
            ContractorJobErrors.AssignmentCancelled => Problem(StatusCodes.Status409Conflict, "Contractor job assignment is cancelled."),
            ContractorJobErrors.NameRequired => Problem(StatusCodes.Status400BadRequest, "Contractor job name is required."),
            ContractorJobErrors.PlannedEndMustBeAfterStart => Problem(StatusCodes.Status400BadRequest, "Planned end must be after planned start."),
            ContractorJobErrors.AssignmentUntilMustBeAfterFrom => Problem(StatusCodes.Status400BadRequest, "Assigned until must be after assigned from."),
            ContractorJobErrors.AssignmentEndsAfterJobEnds => Problem(StatusCodes.Status400BadRequest, "Assigned until must not be after the job planned end."),
            _ => Problem(StatusCodes.Status400BadRequest, "Contractor job request is invalid."),
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
