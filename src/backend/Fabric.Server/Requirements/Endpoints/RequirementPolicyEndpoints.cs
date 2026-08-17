using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Core;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Endpoints;

public static class RequirementPolicyEndpoints
{
    public static IEndpointRouteBuilder MapRequirementPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder policies = app.MapGroup("/api/requirements/policies");
        policies.MapGet("/location/{locationId:guid}", ListLocationRequirementPolicies).Produces<LocationAttachedRequirementResponse[]>();
        policies.MapPost("/location", CreateLocationRequirementPolicy).Produces<LocationRequirementPolicyResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapDelete("/location/{policyId:guid}", DeleteLocationRequirementPolicy).Produces<LocationRequirementPolicyResponse>().Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapPost("/location-job", CreateLocationJobRequirementPolicy).Produces<LocationJobRequirementPolicyResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> ListLocationRequirementPolicies(Guid locationId, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        LocationRequirementPolicy[] policies = await db.LocationRequirementPolicies.AsNoTracking()
            .Where(item => item.LocationId == locationId)
            .OrderBy(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken);
        Guid[] definitionIds = policies.Select(item => item.RequirementDefinitionId).Distinct().ToArray();
        Dictionary<Guid, RequirementDefinition> definitionsById = await db.RequirementDefinitions.AsNoTracking()
            .Where(item => definitionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return Results.Ok(policies
            .Where(item => definitionsById.ContainsKey(item.RequirementDefinitionId))
            .Select(item => item.ToAttachedRequirementResponse(definitionsById[item.RequirementDefinitionId]))
            .ToArray());
    }

    private static async Task<IResult> CreateLocationRequirementPolicy([FromBody] CreateLocationRequirementPolicyRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<LocationRequirementPolicy, RequirementsEvaluationErrors> result = await service.CreateLocationRequirementPolicyAsync(request, cancellationToken);
        return result.Match<IResult>(policy => Results.Created($"/api/requirements/policies/location/{policy.Id}", policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> DeleteLocationRequirementPolicy(Guid policyId, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<LocationRequirementPolicy, RequirementPolicyErrors> result = await service.DeleteLocationRequirementPolicyAsync(policyId, cancellationToken);
        return result.Match<IResult>(policy => Results.Ok(policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapPolicyError));
    }

    private static async Task<IResult> CreateLocationJobRequirementPolicy([FromBody] CreateLocationJobRequirementPolicyRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<LocationJobRequirementPolicy, RequirementsEvaluationErrors> result = await service.CreateLocationJobRequirementPolicyAsync(request, cancellationToken);
        return result.Match<IResult>(policy => Results.Created($"/api/requirements/policies/location-job/{policy.Id}", policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(RequirementsEvaluationErrors error) => error switch
    {
        RequirementsEvaluationErrors.LocationNotFound => Problem(StatusCodes.Status404NotFound, "Location not found."),
        RequirementsEvaluationErrors.RequirementDefinitionNotFound => Problem(StatusCodes.Status404NotFound, "Requirement definition not found."),
        RequirementsEvaluationErrors.JobTypeNotFound => Problem(StatusCodes.Status404NotFound, "Job type not found."),
        _ => Problem(StatusCodes.Status400BadRequest, "Policy request is invalid.")
    };

    private static (int statusCode, ProblemDetails? problemDetails) MapPolicyError(RequirementPolicyErrors error) => error switch
    {
        RequirementPolicyErrors.LocationRequirementPolicyNotFound => Problem(StatusCodes.Status404NotFound, "Location requirement policy not found."),
        _ => Problem(StatusCodes.Status400BadRequest, "Policy request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
