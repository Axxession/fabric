using Fabric.Server.AccessCatalog.Contracts;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Application;
using Fabric.Server.Locations.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Endpoints;

public static class ApprovalInboxEndpoints
{
    public static IEndpointRouteBuilder MapApprovalInboxEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder inbox = app.MapGroup("/api/access-catalog/approval-inbox");

        inbox.MapGet("", ListApprovalInbox).Produces<Page<ApprovalInboxItemResponse>>();

        return app;
    }

    private static async Task<IResult> ListApprovalInbox(
        [AsParameters] ListApprovalInboxRequest request,
        [FromQuery] Guid approverIdentityId,
        [FromQuery] Guid[]? ids,
        AccessCatalogDbContext db,
        AccessControlDbContext accessControlDb,
        IdentitiesDbContext identitiesDb,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ApprovalRequirement> query = db.ApprovalRequirements.AsNoTracking()
            .Where(item => item.Status == ApprovalStatus.Pending)
            .Where(item => item.RequiredApproverIdentityId == approverIdentityId
                || (item.ApprovalGroupId.HasValue && db.ApprovalGroupMembers.Any(member => member.IdentityId == approverIdentityId && member.ApprovalGroupId == item.ApprovalGroupId.Value && member.ResponsibleLocationId == item.LocationId)));

        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));

        Page<ApprovalRequirement> page = await query.OrderBy(item => item.CreatedAt).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        ApprovalRequirement[] requirements = page.Items.ToArray();

        Guid[] requestIds = requirements.Select(item => item.RequestId).Distinct().ToArray();
        Guid[] flowIds = requirements.Select(item => item.ApprovalFlowId).Distinct().ToArray();
        Guid[] approvalGroupIds = requirements.Where(item => item.ApprovalGroupId.HasValue).Select(item => item.ApprovalGroupId!.Value).Distinct().ToArray();

        Dictionary<Guid, PackageRequest> requestsById = await db.PackageRequests.AsNoTracking()
            .Where(item => requestIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        Dictionary<Guid, ApprovalFlow> flowsById = await db.ApprovalFlows.AsNoTracking()
            .Where(item => flowIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        Dictionary<Guid, string> approvalGroupNames = approvalGroupIds.Length == 0
            ? []
            : await db.ApprovalGroups.AsNoTracking()
                .Where(item => approvalGroupIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

        Guid[] packageIds = requestsById.Values.Select(item => item.PackageId).Distinct().ToArray();
        Dictionary<Guid, Package> packagesById = await db.Packages.AsNoTracking()
            .Where(item => packageIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        Guid[] accessItemIds = flowsById.Values.Select(item => item.AccessItemId).Distinct().ToArray();
        Dictionary<Guid, AccessItemPreview> accessItemsById = await accessControlDb.AccessItems.AsNoTracking()
            .Where(item => accessItemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => new AccessItemPreview(item.Id, item.Name), cancellationToken);

        Guid[] identityIds = requestsById.Values.Select(item => item.RequesterIdentityId)
            .Concat(requestsById.Values.Select(item => item.BeneficiaryIdentityId))
            .Distinct()
            .ToArray();
        Dictionary<Guid, string> identityNames = await identitiesDb.Identities.AsNoTracking()
            .Where(item => identityIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);

        PackageRequestScope[] scopes = flowIds.Length == 0
            ? []
            : await db.PackageRequestScopes.AsNoTracking()
                .Where(item => flowIds.Contains(item.ApprovalFlowId))
                .ToArrayAsync(cancellationToken);
        ILookup<Guid, PackageRequestScope> scopesByFlowId = scopes.ToLookup(item => item.ApprovalFlowId);

        Guid[] locationIds = flowsById.Values.Select(item => item.SiteId)
            .Concat(scopes.Select(item => item.RequestedLocationId))
            .Distinct()
            .ToArray();
        Dictionary<Guid, string> locationLabels = await BuildLocationLabelsAsync(locationIds, locationService, cancellationToken);

        Page<ApprovalInboxItemResponse> response = ((IPaged<ApprovalRequirement>)page).Map(requirement =>
        {
            PackageRequest packageRequest = requestsById[requirement.RequestId];
            ApprovalFlow flow = flowsById[requirement.ApprovalFlowId];
            Package package = packagesById[packageRequest.PackageId];
            AccessItemPreview accessItem = accessItemsById[flow.AccessItemId];

            return new ApprovalInboxItemResponse(
                requirement.Id,
                flow.Id,
                packageRequest.Id,
                package.Id,
                package.Name,
                packageRequest.BeneficiaryIdentityId,
                identityNames.GetValueOrDefault(packageRequest.BeneficiaryIdentityId, packageRequest.BeneficiaryIdentityId.ToString()),
                packageRequest.RequesterIdentityId,
                identityNames.GetValueOrDefault(packageRequest.RequesterIdentityId, packageRequest.RequesterIdentityId.ToString()),
                flow.AccessItemId,
                accessItem.Name,
                flow.SiteId,
                locationLabels.GetValueOrDefault(flow.SiteId, flow.SiteId.ToString()),
                scopesByFlowId[flow.Id].Select(item => locationLabels.GetValueOrDefault(item.RequestedLocationId, item.RequestedLocationId.ToString())).OrderBy(item => item).ToArray(),
                requirement.Type,
                requirement.Role,
                requirement.ApprovalGroupId.HasValue ? approvalGroupNames.GetValueOrDefault(requirement.ApprovalGroupId.Value) : null,
                requirement.CreatedAt,
                packageRequest.ExpiresAt,
                requirement.Status);
        });

        return Results.Ok(response);
    }

    private static async Task<Dictionary<Guid, string>> BuildLocationLabelsAsync(Guid[] locationIds, LocationService locationService, CancellationToken cancellationToken)
    {
        Dictionary<Guid, string> result = [];

        foreach (Guid locationId in locationIds)
        {
            LocationResponse? location = (await locationService.GetLocationById(locationId, cancellationToken))?.ToResponse();
            if (location is null)
                continue;

            result[locationId] = string.Join(" / ", new[] { location.Site.Name, location.Building?.Name, location.Room?.Name }.Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        return result;
    }

    private sealed record AccessItemPreview(Guid Id, string Name);
}
