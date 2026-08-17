using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.AspNetCore.Authorization;
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
        policies.MapGet("/location-job", ListLocationJobRequirementPolicies)
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole })
            .Produces<LocationJobAttachedRequirementResponse[]>();
        policies.MapPost("/location-job", CreateLocationJobRequirementPolicy)
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole })
            .Produces<LocationJobRequirementPolicyResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapPut("/location-job/{policyId:guid}", UpdateLocationJobRequirementPolicy)
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole })
            .Produces<LocationJobRequirementPolicyResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapPost("/location-job/{policyId:guid}/enable", EnableLocationJobRequirementPolicy)
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole })
            .Produces<LocationJobRequirementPolicyResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapPost("/location-job/{policyId:guid}/disable", DisableLocationJobRequirementPolicy)
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole })
            .Produces<LocationJobRequirementPolicyResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapDelete("/location-job/{policyId:guid}", DeleteLocationJobRequirementPolicy)
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole })
            .Produces<LocationJobRequirementPolicyResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
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

    private static async Task<IResult> ListLocationJobRequirementPolicies(
        [AsParameters] ListLocationJobRequirementPoliciesRequest request,
        RequirementsDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<LocationJobRequirementPolicy> query = db.LocationJobRequirementPolicies.AsNoTracking();

        if (request.LocationId.HasValue)
            query = query.Where(item => item.LocationId == request.LocationId.Value);

        if (request.JobTypeId.HasValue)
            query = query.Where(item => item.JobTypeId == request.JobTypeId.Value);

        if (request.RequirementDefinitionId.HasValue)
            query = query.Where(item => item.RequirementDefinitionId == request.RequirementDefinitionId.Value);

        if (request.IsEnabled.HasValue)
            query = query.Where(item => item.IsEnabled == request.IsEnabled.Value);

        LocationJobRequirementPolicy[] policies = await query
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
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

    private static async Task<IResult> UpdateLocationJobRequirementPolicy(Guid policyId, [FromBody] UpdateLocationJobRequirementPolicyRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<LocationJobRequirementPolicy, RequirementPolicyErrors> result = await service.UpdateLocationJobRequirementPolicyAsync(policyId, request, cancellationToken);
        return result.Match<IResult>(policy => Results.Ok(policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapPolicyError));
    }

    private static async Task<IResult> EnableLocationJobRequirementPolicy(Guid policyId, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<LocationJobRequirementPolicy, RequirementPolicyErrors> result = await service.SetLocationJobRequirementPolicyEnabledAsync(policyId, true, cancellationToken);
        return result.Match<IResult>(policy => Results.Ok(policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapPolicyError));
    }

    private static async Task<IResult> DisableLocationJobRequirementPolicy(Guid policyId, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<LocationJobRequirementPolicy, RequirementPolicyErrors> result = await service.SetLocationJobRequirementPolicyEnabledAsync(policyId, false, cancellationToken);
        return result.Match<IResult>(policy => Results.Ok(policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapPolicyError));
    }

    private static async Task<IResult> DeleteLocationJobRequirementPolicy(Guid policyId, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<LocationJobRequirementPolicy, RequirementPolicyErrors> result = await service.DeleteLocationJobRequirementPolicyAsync(policyId, cancellationToken);
        return result.Match<IResult>(policy => Results.Ok(policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapPolicyError));
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
        RequirementPolicyErrors.LocationJobRequirementPolicyNotFound => Problem(StatusCodes.Status404NotFound, "Location job requirement policy not found."),
        RequirementPolicyErrors.PolicyAlreadyEnabled => Problem(StatusCodes.Status409Conflict, "Policy is already enabled."),
        RequirementPolicyErrors.PolicyAlreadyDisabled => Problem(StatusCodes.Status409Conflict, "Policy is already disabled."),
        _ => Problem(StatusCodes.Status400BadRequest, "Policy request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
