using Fabric.Server.Core;
using Fabric.Server.Reception.Contracts;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Endpoints;

public static class AccessRuleAssignmentEndpoints
{
    public static IEndpointRouteBuilder MapReceptionAccessRuleAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder assignments = app.MapGroup("/api/reception/access-rule-assignments");

        assignments.MapGet("", ListAccessRuleAssignments)
            .WithDescription("List reception access rule assignments")
            .WithSummary("List access rule assignments")
            .Produces<Page<AccessRuleAssignmentResponse>>();
        assignments.MapGet("/{id:guid}", GetAccessRuleAssignment)
            .WithDescription("Retrieve a reception access rule assignment by id")
            .WithSummary("Retrieve access rule assignment")
            .Produces<AccessRuleAssignmentResponse>()
            .Produces(StatusCodes.Status404NotFound);
        assignments.MapPost("", CreateAccessRuleAssignment)
            .WithDescription("Create a reception access rule assignment")
            .WithSummary("Create access rule assignment")
            .Produces<AccessRuleAssignmentResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        assignments.MapPut("/{id:guid}", UpdateAccessRuleAssignment)
            .WithDescription("Update a reception access rule assignment")
            .WithSummary("Update access rule assignment")
            .Produces<AccessRuleAssignmentResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        assignments.MapDelete("/{id:guid}", DeleteAccessRuleAssignment)
            .WithDescription("Delete a reception access rule assignment")
            .WithSummary("Delete access rule assignment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAccessRuleAssignments(
        [AsParameters] BaseListRequest request,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        IPaged<ReceptionAccessRuleAssignment> result = await db.AccessRuleAssignments
            .AsNoTracking()
            .OrderBy(assignment => assignment.Trigger)
            .ThenBy(assignment => assignment.PackageId)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(result.Map(assignment => assignment.ToResponse()));
    }

    private static async Task<IResult> GetAccessRuleAssignment(
        Guid id,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        ReceptionAccessRuleAssignment? assignment = await db.AccessRuleAssignments
            .AsNoTracking()
            .SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

        return assignment is null ? Results.NotFound() : Results.Ok(assignment.ToResponse());
    }

    private static async Task<IResult> CreateAccessRuleAssignment(
        [FromBody] CreateAccessRuleAssignmentRequest request,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        Result<ReceptionAccessRuleAssignment, ReceptionErrors> result = ReceptionAccessRuleAssignment.Create(
            request.PackageId,
            request.GracePeriodMinutes,
            request.Trigger);

        if (result.IsFailure(out ReceptionErrors error))
            return Result.Failure(error).AsResponse(MapError);

        result.IsSuccess(out ReceptionAccessRuleAssignment assignment);
        db.AccessRuleAssignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/reception/access-rule-assignments/{assignment.Id}", assignment.ToResponse());
    }

    private static async Task<IResult> UpdateAccessRuleAssignment(
        Guid id,
        [FromBody] UpdateAccessRuleAssignmentRequest request,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        ReceptionAccessRuleAssignment? assignment = await db.AccessRuleAssignments
            .SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

        if (assignment is null)
            return Results.NotFound();

        Result<ReceptionErrors> result = assignment.Update(
            request.PackageId,
            request.GracePeriodMinutes,
            request.Trigger);

        if (result.IsFailure(out ReceptionErrors error))
            return Result.Failure(error).AsResponse(MapError);

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(assignment.ToResponse());
    }

    private static async Task<IResult> DeleteAccessRuleAssignment(
        Guid id,
        ReceptionDbContext db,
        CancellationToken cancellationToken = default)
    {
        ReceptionAccessRuleAssignment? assignment = await db.AccessRuleAssignments
            .SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

        if (assignment is null)
            return Results.NotFound();

        db.AccessRuleAssignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static (int statusCode, ProblemDetails problemDetails) MapError(ReceptionErrors error) =>
        error switch
        {
            ReceptionErrors.GracePeriodMustNotBeNegative => Problem(StatusCodes.Status400BadRequest, "Grace period must not be negative."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected reception error.")
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
