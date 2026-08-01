using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class ApprovalDefinitionEndpoints
{
    public static IEndpointRouteBuilder MapApprovalDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder definitions = app.MapGroup("/api/access-catalog/approval-definitions");

        definitions.MapGet("", ListApprovalDefinitions).Produces<Page<ApprovalDefinitionResponse>>();
        definitions.MapPost("", CreateApprovalDefinition).Produces<ApprovalDefinitionResponse>(StatusCodes.Status201Created);
        definitions.MapGet("/{approvalDefinitionId:guid}", GetApprovalDefinition).Produces<ApprovalDefinitionResponse>().Produces(StatusCodes.Status404NotFound);
        definitions.MapPut("/{approvalDefinitionId:guid}", UpdateApprovalDefinition).Produces<ApprovalDefinitionResponse>().Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListApprovalDefinitions([AsParameters] BaseListRequest request, [FromQuery] Guid[]? ids, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<ApprovalDefinition> query = db.ApprovalDefinitions.AsNoTracking();
        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        IPaged<ApprovalDefinition> result = await query.OrderBy(item => item.AccessItemId).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateApprovalDefinition([FromBody] CreateApprovalDefinitionRequest request, ApprovalConfigurationService service, CancellationToken cancellationToken = default)
    {
        Result<ApprovalDefinition, AccessCatalogErrors> result = await service.CreateApprovalDefinitionAsync(request.AccessItemId, request.DestinationApprovalGroupId, request.OrganizationalApprovalMode, request.OrganizationalApprovalLevels, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-catalog/approval-definitions/{item.Id}", item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> GetApprovalDefinition(Guid approvalDefinitionId, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        ApprovalDefinition? definition = await db.ApprovalDefinitions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == approvalDefinitionId, cancellationToken);
        return definition is null ? Results.NotFound() : Results.Ok(definition.ToResponse());
    }

    private static async Task<IResult> UpdateApprovalDefinition(Guid approvalDefinitionId, [FromBody] UpdateApprovalDefinitionRequest request, ApprovalConfigurationService service, CancellationToken cancellationToken = default)
    {
        Result<ApprovalDefinition, AccessCatalogErrors> result = await service.UpdateApprovalDefinitionAsync(approvalDefinitionId, request.DestinationApprovalGroupId, request.OrganizationalApprovalMode, request.OrganizationalApprovalLevels, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) => error switch
    {
        AccessCatalogErrors.ApprovalDefinitionNotFound => Problem(StatusCodes.Status404NotFound, "Approval definition not found."),
        AccessCatalogErrors.ApprovalDefinitionAlreadyExists => Problem(StatusCodes.Status409Conflict, "Approval definition already exists for that access item."),
        AccessCatalogErrors.ApprovalGroupNotFound => Problem(StatusCodes.Status404NotFound, "Approval group not found."),
        AccessCatalogErrors.AccessItemNotFound => Problem(StatusCodes.Status404NotFound, "Access item not found."),
        AccessCatalogErrors.InvalidOrganizationalApprovalLevels => Problem(StatusCodes.Status400BadRequest, "Organizational approval levels must be greater than zero when manager-chain mode is used."),
        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) => (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) => error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
