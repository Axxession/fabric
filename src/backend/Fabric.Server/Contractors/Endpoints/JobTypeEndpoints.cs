using Fabric.Server.Contractors.Application;
using Fabric.Server.Contractors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors.Endpoints;

public static class JobTypeEndpoints
{
    public static IEndpointRouteBuilder MapJobTypeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder jobTypes = app.MapGroup("/api/contractors/job-types");

        jobTypes.MapGet("", ListJobTypes)
            .WithSummary("List contractor job types")
            .Produces<Page<JobTypeResponse>>();
        jobTypes.MapGet("/{id:guid}", GetJobType)
            .WithSummary("Get contractor job type")
            .Produces<JobTypeResponse>()
            .Produces(StatusCodes.Status404NotFound);
        jobTypes.MapPost("", CreateJobType)
            .WithSummary("Create contractor job type")
            .Produces<JobTypeResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        jobTypes.MapPut("/{id:guid}", UpdateJobType)
            .WithSummary("Update contractor job type")
            .Produces<JobTypeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        jobTypes.MapPost("/{id:guid}/activate", ActivateJobType)
            .WithSummary("Activate contractor job type")
            .Produces<JobTypeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        jobTypes.MapPost("/{id:guid}/deactivate", DeactivateJobType)
            .WithSummary("Deactivate contractor job type")
            .Produces<JobTypeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListJobTypes(
        [AsParameters] ListJobTypesRequest request,
        [FromQuery] Guid[]? ids,
        ContractorsDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<JobType> query = db.JobTypes.AsNoTracking();

        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        if (request.IsActive.HasValue)
            query = query.Where(item => item.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, filter)
                || EF.Functions.ILike(item.Name, filter)
                || item.Description != null && EF.Functions.ILike(item.Description, filter));
        }

        IPaged<JobType> result = await query.OrderBy(item => item.Name).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetJobType(Guid id, ContractorsDbContext db, CancellationToken cancellationToken = default)
    {
        JobType? jobType = await db.JobTypes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return jobType is null ? Results.NotFound() : Results.Ok(jobType.ToResponse());
    }

    private static async Task<IResult> CreateJobType([FromBody] CreateJobTypeRequest request, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<JobType, JobTypeErrors> result = await service.CreateJobTypeAsync(request, cancellationToken);
        return result.Match<IResult>(
            jobType => Results.Created($"/api/contractors/job-types/{jobType.Id}", jobType.ToResponse()),
            _ => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpdateJobType(Guid id, [FromBody] UpdateJobTypeRequest request, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<JobType, JobTypeErrors> result = await service.UpdateJobTypeAsync(id, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ActivateJobType(Guid id, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<JobType, JobTypeErrors> result = await service.SetJobTypeActiveAsync(id, true, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> DeactivateJobType(Guid id, ContractorsService service, CancellationToken cancellationToken = default)
    {
        Result<JobType, JobTypeErrors> result = await service.SetJobTypeActiveAsync(id, false, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(JobTypeErrors error) =>
        error switch
        {
            JobTypeErrors.JobTypeNotFound => Problem(StatusCodes.Status404NotFound, "Job type not found."),
            JobTypeErrors.JobTypeCodeAlreadyExists => Problem(StatusCodes.Status409Conflict, "Job type code already exists."),
            JobTypeErrors.JobTypeAlreadyActive => Problem(StatusCodes.Status409Conflict, "Job type is already active."),
            JobTypeErrors.JobTypeAlreadyInactive => Problem(StatusCodes.Status409Conflict, "Job type is already inactive."),
            JobTypeErrors.CodeRequired => Problem(StatusCodes.Status400BadRequest, "Job type code is required."),
            JobTypeErrors.NameRequired => Problem(StatusCodes.Status400BadRequest, "Job type name is required."),
            _ => Problem(StatusCodes.Status400BadRequest, "Job type request is invalid."),
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
