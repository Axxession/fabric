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

public static class ContractorJobAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapContractorJobAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder assignments = app.MapGroup("/api/contractors/jobs/{contractorJobId:guid}/assignments")
            .RequireAuthorization(FabricRoleDefaults.ContractorEnrollmentOrPlanningPolicy);

        assignments.MapGet("", ListContractorJobAssignments)
            .WithSummary("List contractor job assignments")
            .Produces<Page<ContractorJobAssignmentResponse>>();
        assignments.MapGet("/{assignmentId:guid}", GetContractorJobAssignment)
            .WithSummary("Get contractor job assignment")
            .Produces<ContractorJobAssignmentResponse>()
            .Produces(StatusCodes.Status404NotFound);
        assignments.MapPost("", CreateContractorJobAssignment)
            .WithSummary("Create contractor job assignment")
            .Produces<ContractorJobAssignmentResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        assignments.MapPut("/{assignmentId:guid}", UpdateContractorJobAssignment)
            .WithSummary("Update contractor job assignment")
            .Produces<ContractorJobAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        assignments.MapPost("/{assignmentId:guid}/activate", ActivateContractorJobAssignment)
            .WithSummary("Activate contractor job assignment")
            .Produces<ContractorJobAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        assignments.MapPost("/{assignmentId:guid}/complete", CompleteContractorJobAssignment)
            .WithSummary("Complete contractor job assignment")
            .Produces<ContractorJobAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        assignments.MapPost("/{assignmentId:guid}/cancel", CancelContractorJobAssignment)
            .WithSummary("Cancel contractor job assignment")
            .Produces<ContractorJobAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListContractorJobAssignments(
        Guid contractorJobId,
        [AsParameters] ListContractorJobAssignmentsRequest request,
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
        bool jobExists = await ContractorAuthorization.OwnsJobAsync(db, contractorJobId, currentIdentityId, cancellationToken);
        if (!jobExists)
            return Results.NotFound();

        IQueryable<ContractorJobAssignment> query = db.ContractorJobAssignments.AsNoTracking().Where(item => item.ContractorJobId == contractorJobId);

        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        if (request.ContractorId.HasValue)
            query = query.Where(item => item.ContractorId == request.ContractorId.Value);

        if (request.Status is { Length: > 0 })
            query = query.Where(item => request.Status.Contains(item.Status));

        if (request.AssignedAfter.HasValue)
            query = query.Where(item => item.AssignedFrom >= request.AssignedAfter.Value);

        if (request.AssignedBefore.HasValue)
            query = query.Where(item => item.AssignedUntil <= request.AssignedBefore.Value);

        IPaged<ContractorJobAssignment> result = await query.OrderBy(item => item.AssignedFrom).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetContractorJobAssignment(Guid contractorJobId, Guid assignmentId, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        bool jobExists = await ContractorAuthorization.OwnsJobAsync(db, contractorJobId, currentIdentityId, cancellationToken);
        if (!jobExists)
            return Results.NotFound();

        ContractorJobAssignment? assignment = await db.ContractorJobAssignments.AsNoTracking().SingleOrDefaultAsync(
            item => item.ContractorJobId == contractorJobId && item.Id == assignmentId,
            cancellationToken);
        return assignment is null ? Results.NotFound() : Results.Ok(assignment.ToResponse());
    }

    private static async Task<IResult> CreateContractorJobAssignment(Guid contractorJobId, [FromBody] CreateContractorJobAssignmentRequest request, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, contractorJobId, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJobAssignment, ContractorJobErrors> result = await service.CreateAssignmentAsync(contractorJobId, request, cancellationToken);
        return result.Match<IResult>(
            assignment => Results.Created($"/api/contractors/jobs/{contractorJobId}/assignments/{assignment.Id}", assignment.ToResponse()),
            _ => result.Map(item => item.ToResponse()).AsResponse(ContractorJobEndpoints.MapError));
    }

    private static async Task<IResult> UpdateContractorJobAssignment(Guid contractorJobId, Guid assignmentId, [FromBody] UpdateContractorJobAssignmentRequest request, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, contractorJobId, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJobAssignment, ContractorJobErrors> result = await service.UpdateAssignmentAsync(contractorJobId, assignmentId, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(ContractorJobEndpoints.MapError);
    }

    private static async Task<IResult> ActivateContractorJobAssignment(Guid contractorJobId, Guid assignmentId, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, contractorJobId, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJobAssignment, ContractorJobErrors> result = await service.ActivateAssignmentAsync(contractorJobId, assignmentId, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(ContractorJobEndpoints.MapError);
    }

    private static async Task<IResult> CompleteContractorJobAssignment(Guid contractorJobId, Guid assignmentId, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, contractorJobId, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJobAssignment, ContractorJobErrors> result = await service.CompleteAssignmentAsync(contractorJobId, assignmentId, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(ContractorJobEndpoints.MapError);
    }

    private static async Task<IResult> CancelContractorJobAssignment(Guid contractorJobId, Guid assignmentId, ContractorsService service, ContractorsDbContext db, CurrentActorService currentActorService, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        Result<Guid, IResult> actorIdentity = await ContractorAuthorization.GetCurrentIdentityIdAsync(httpContext, currentActorService, cancellationToken);
        if (actorIdentity.IsFailure(out IResult errorResult))
            return errorResult;

        actorIdentity.IsSuccess(out Guid currentIdentityId);
        if (!await ContractorAuthorization.OwnsJobAsync(db, contractorJobId, currentIdentityId, cancellationToken))
            return Results.NotFound();

        Result<ContractorJobAssignment, ContractorJobErrors> result = await service.CancelAssignmentAsync(contractorJobId, assignmentId, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(ContractorJobEndpoints.MapError);
    }
}
