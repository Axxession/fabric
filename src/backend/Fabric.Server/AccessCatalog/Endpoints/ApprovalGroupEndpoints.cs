using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class ApprovalGroupEndpoints
{
    public static IEndpointRouteBuilder MapApprovalGroupEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder groups = app.MapGroup("/api/access-catalog/approval-groups");

        groups.MapGet("", ListApprovalGroups).Produces<Page<ApprovalGroupResponse>>();
        groups.MapPost("", CreateApprovalGroup).Produces<ApprovalGroupResponse>(StatusCodes.Status201Created);
        groups.MapGet("/{approvalGroupId:guid}", GetApprovalGroup).Produces<ApprovalGroupResponse>().Produces(StatusCodes.Status404NotFound);
        groups.MapPut("/{approvalGroupId:guid}", UpdateApprovalGroup).Produces<ApprovalGroupResponse>().Produces(StatusCodes.Status404NotFound);
        groups.MapGet("/{approvalGroupId:guid}/members", ListApprovalGroupMembers).Produces<Page<ApprovalGroupMemberResponse>>();
        groups.MapPost("/{approvalGroupId:guid}/members", AddApprovalGroupMember).Produces<ApprovalGroupMemberResponse>(StatusCodes.Status201Created);
        groups.MapDelete("/{approvalGroupId:guid}/members/{memberId:guid}", DeleteApprovalGroupMember).Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> ListApprovalGroups([AsParameters] ListApprovalGroupsRequest request, [FromQuery] Guid[]? ids, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<ApprovalGroup> query = db.ApprovalGroups.AsNoTracking();
        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));
        if (!string.IsNullOrWhiteSpace(request.Name))
            query = query.Where(item => item.Name.ToLower().Contains(request.Name.ToLower()));

        IPaged<ApprovalGroup> result = await query.OrderBy(item => item.Name).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> CreateApprovalGroup([FromBody] CreateApprovalGroupRequest request, ApprovalConfigurationService service, CancellationToken cancellationToken = default)
    {
        Result<ApprovalGroup, AccessCatalogErrors> result = await service.CreateApprovalGroupAsync(request.Name, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-catalog/approval-groups/{item.Id}", item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> GetApprovalGroup(Guid approvalGroupId, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        ApprovalGroup? group = await db.ApprovalGroups.AsNoTracking().SingleOrDefaultAsync(item => item.Id == approvalGroupId, cancellationToken);
        return group is null ? Results.NotFound() : Results.Ok(group.ToResponse());
    }

    private static async Task<IResult> UpdateApprovalGroup(Guid approvalGroupId, [FromBody] UpdateApprovalGroupRequest request, ApprovalConfigurationService service, CancellationToken cancellationToken = default)
    {
        Result<ApprovalGroup, AccessCatalogErrors> result = await service.UpdateApprovalGroupAsync(approvalGroupId, request.Name, request.Status, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListApprovalGroupMembers(Guid approvalGroupId, [AsParameters] BaseListRequest request, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IPaged<ApprovalGroupMember> result = await db.ApprovalGroupMembers.AsNoTracking()
            .Where(item => item.ApprovalGroupId == approvalGroupId)
            .OrderBy(item => item.IdentityId)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> AddApprovalGroupMember(Guid approvalGroupId, [FromBody] CreateApprovalGroupMemberRequest request, ApprovalConfigurationService service, CancellationToken cancellationToken = default)
    {
        Result<ApprovalGroupMember, AccessCatalogErrors> result = await service.AddApprovalGroupMemberAsync(approvalGroupId, request.IdentityId, request.ResponsibleLocationId, cancellationToken);
        return result.Match<IResult>(item => Results.Created($"/api/access-catalog/approval-groups/{approvalGroupId}/members/{item.Id}", item.ToResponse()), error => MapError(error).ToResult());
    }

    private static async Task<IResult> DeleteApprovalGroupMember(Guid approvalGroupId, Guid memberId, ApprovalConfigurationService service, CancellationToken cancellationToken = default)
    {
        Result<AccessCatalogErrors> result = await service.RemoveApprovalGroupMemberAsync(approvalGroupId, memberId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) => error switch
    {
        AccessCatalogErrors.ApprovalGroupNotFound => Problem(StatusCodes.Status404NotFound, "Approval group not found."),
        AccessCatalogErrors.ApprovalGroupNameAlreadyExists => Problem(StatusCodes.Status409Conflict, "Approval group name already exists."),
        AccessCatalogErrors.ApprovalGroupMemberNotFound => Problem(StatusCodes.Status404NotFound, "Approval group member not found."),
        AccessCatalogErrors.ApprovalGroupMemberAlreadyExists => Problem(StatusCodes.Status409Conflict, "Approval group member already exists."),
        AccessCatalogErrors.ApprovalGroupMemberLocationMustBeSite => Problem(StatusCodes.Status400BadRequest, "Approval group members must be assigned to a site."),
        AccessCatalogErrors.IdentityNotFound => Problem(StatusCodes.Status404NotFound, "Identity not found."),
        AccessCatalogErrors.LocationRequired => Problem(StatusCodes.Status400BadRequest, "A valid location is required."),
        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) => (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) => error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
