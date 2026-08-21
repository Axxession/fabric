using Fabric.Server.Core;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Endpoints;

public static class RequirementDefinitionEndpoints
{
    public static IEndpointRouteBuilder MapRequirementDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder definitions = app.MapGroup("/api/requirements/definitions");

        definitions.MapGet("", ListRequirementDefinitions).Produces<Page<RequirementDefinitionResponse>>();
        definitions.MapGet("/{id:guid}", GetRequirementDefinition).Produces<RequirementDefinitionResponse>().Produces(StatusCodes.Status404NotFound);
        definitions.MapPost("", CreateRequirementDefinition).Produces<RequirementDefinitionResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        definitions.MapPut("/{id:guid}", UpdateRequirementDefinition).Produces<RequirementDefinitionResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        definitions.MapDelete("/{id:guid}", DeleteRequirementDefinition).Produces<RequirementDefinitionResponse>().Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        return app;
    }

    private static async Task<IResult> ListRequirementDefinitions([AsParameters] ListRequirementsRequest request, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<RequirementDefinition> query = db.RequirementDefinitions.AsNoTracking();
        if (request.Ids is { Length: > 0 })
            query = query.Where(item => request.Ids.Contains(item.Id));

        if (request.LocationId.HasValue)
        {
            Guid[] definitionIds = await db.LocationRequirementPolicies.AsNoTracking()
                .Where(item => item.LocationId == request.LocationId.Value)
                .Select(item => item.RequirementDefinitionId)
                .Distinct()
                .ToArrayAsync(cancellationToken);
            query = query.Where(item => definitionIds.Contains(item.Id));
        }
        if (request.IsActive.HasValue)
            query = query.Where(item => item.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item => EF.Functions.ILike(item.Code, filter) || EF.Functions.ILike(item.Name, filter));
        }

        IPaged<RequirementDefinition> page = await query.OrderBy(item => item.Name).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetRequirementDefinition(Guid id, RequirementsDbContext db, CancellationToken cancellationToken = default)
    {
        RequirementDefinition? definition = await db.RequirementDefinitions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return definition is null ? Results.NotFound() : Results.Ok(definition.ToResponse());
    }

    private static async Task<IResult> CreateRequirementDefinition([FromBody] CreateRequirementDefinitionRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<RequirementDefinition, RequirementDefinitionErrors> result = await service.CreateRequirementDefinitionAsync(request, cancellationToken);
        return result.Match<IResult>(definition => Results.Created($"/api/requirements/definitions/{definition.Id}", definition.ToResponse()), error => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpdateRequirementDefinition(Guid id, [FromBody] UpdateRequirementDefinitionRequest request, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<RequirementDefinition, RequirementDefinitionErrors> result = await service.UpdateRequirementDefinitionAsync(id, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> DeleteRequirementDefinition(Guid id, RequirementsService service, CancellationToken cancellationToken = default)
    {
        Result<RequirementDefinition, RequirementDefinitionErrors> result = await service.DeleteRequirementDefinitionAsync(id, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(RequirementDefinitionErrors error) => error switch
    {
        RequirementDefinitionErrors.RequirementDefinitionNotFound => Problem(StatusCodes.Status404NotFound, "Requirement definition not found."),
        RequirementDefinitionErrors.RequirementDefinitionInUse => Problem(StatusCodes.Status409Conflict, "Requirement definition is in use and cannot be deleted."),
        RequirementDefinitionErrors.CodeRequired => Problem(StatusCodes.Status400BadRequest, "Requirement definition code is required."),
        RequirementDefinitionErrors.NameRequired => Problem(StatusCodes.Status400BadRequest, "Requirement definition name is required."),
        RequirementDefinitionErrors.AllowedEvidenceKindsRequired => Problem(StatusCodes.Status400BadRequest, "At least one allowed evidence kind is required."),
        _ => Problem(StatusCodes.Status400BadRequest, "Requirement definition request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
