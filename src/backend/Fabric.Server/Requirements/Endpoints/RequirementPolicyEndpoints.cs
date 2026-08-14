using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Core;
using Fabric.Server.Requirements.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Requirements.Endpoints;

public static class RequirementPolicyEndpoints
{
    public static IEndpointRouteBuilder MapRequirementPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder policies = app.MapGroup("/api/requirements/policies");
        policies.MapPost("/zone", CreateZoneRequirementPolicy).Produces<ZoneRequirementPolicyResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapPost("/contractor-job", CreateContractorJobRequirementPolicy).Produces<ContractorJobRequirementPolicyResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        policies.MapPost("/zone-access", CreateEnforcementZoneAccessPolicy).Produces<EnforcementZoneAccessPolicyResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        return app;
    }

    private static async Task<IResult> CreateZoneRequirementPolicy([FromBody] CreateZoneRequirementPolicyRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<ZoneRequirementPolicy, RequirementsEvaluationErrors> result = await service.CreateZoneRequirementPolicyAsync(request, cancellationToken);
        return result.Match<IResult>(policy => Results.Created($"/api/requirements/policies/zone/{policy.Id}", policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> CreateContractorJobRequirementPolicy([FromBody] CreateContractorJobRequirementPolicyRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<ContractorJobRequirementPolicy, RequirementsEvaluationErrors> result = await service.CreateContractorJobRequirementPolicyAsync(request, cancellationToken);
        return result.Match<IResult>(policy => Results.Created($"/api/requirements/policies/contractor-job/{policy.Id}", policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> CreateEnforcementZoneAccessPolicy([FromBody] CreateEnforcementZoneAccessPolicyRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<EnforcementZoneAccessPolicy, RequirementsEvaluationErrors> result = await service.CreateEnforcementZoneAccessPolicyAsync(request, cancellationToken);
        return result.Match<IResult>(policy => Results.Created($"/api/requirements/policies/zone-access/{policy.Id}", policy.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(RequirementsEvaluationErrors error) => error switch
    {
        RequirementsEvaluationErrors.EnforcementZoneNotFound => Problem(StatusCodes.Status404NotFound, "Enforcement zone not found."),
        RequirementsEvaluationErrors.RequirementDefinitionNotFound => Problem(StatusCodes.Status404NotFound, "Requirement definition not found."),
        RequirementsEvaluationErrors.JobTypeNotFound => Problem(StatusCodes.Status404NotFound, "Job type not found."),
        RequirementsEvaluationErrors.AccessItemNotFound => Problem(StatusCodes.Status404NotFound, "Access item not found."),
        _ => Problem(StatusCodes.Status400BadRequest, "Policy request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
