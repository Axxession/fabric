using Fabric.Server.AccessCatalog.Application;
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

public static class PackageRequestEndpoints
{
    public static IEndpointRouteBuilder MapPackageRequestEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder requests = app.MapGroup("/api/access-catalog/package-requests");

        requests.MapGet("", ListPackageRequests).Produces<Page<PackageRequestResponse>>();
        requests.MapPost("/approval-preview", PreviewPackageRequestApprovals).Produces<PackageRequestPreviewResponse>();
        requests.MapPost("", CreatePackageRequest).Produces<PackageRequestResponse>(StatusCodes.Status201Created);
        requests.MapGet("/{requestId:guid}", GetPackageRequest).Produces<PackageRequestResponse>().Produces(StatusCodes.Status404NotFound);
        requests.MapGet("/{requestId:guid}/details", GetPackageRequestDetails).Produces<PackageRequestDetailResponse>().Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListPackageRequests([AsParameters] ListPackageRequestsRequest request, [FromQuery] Guid[]? ids, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<PackageRequest> query = db.PackageRequests.AsNoTracking();
        if (ids is { Length: > 0 })
            query = query.Where(item => ids.Contains(item.Id));
        if (request.RequesterIdentityId.HasValue)
            query = query.Where(item => item.RequesterIdentityId == request.RequesterIdentityId.Value);
        if (request.BeneficiaryIdentityId.HasValue)
            query = query.Where(item => item.BeneficiaryIdentityId == request.BeneficiaryIdentityId.Value);
        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);

        IPaged<PackageRequest> result = await query.OrderByDescending(item => item.CreatedAt).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        PackageRequest[] items = result.Items.ToArray();
        Dictionary<Guid, Guid[]> locations = await LoadLocationsAsync(db, items.Select(item => item.Id).ToArray(), cancellationToken);
        return Results.Ok(result.Map(item => item.ToResponse(locations.GetValueOrDefault(item.Id, []))));
    }

    private static async Task<IResult> CreatePackageRequest([FromBody] CreatePackageRequestRequest request, PackageRequestService service, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        Result<PackageRequest, AccessCatalogErrors> result = await service.CreateAsync(request.PackageId, request.RequesterIdentityId, request.BeneficiaryIdentityId, request.LocationIds, request.RequestReason, request.DurationKind, request.ValidFrom, request.ValidUntil, cancellationToken);
        return await result.Match<Task<IResult>>(
            async item =>
            {
                Guid[] locationIds = await db.PackageRequestLocations.AsNoTracking().Where(link => link.RequestId == item.Id).Select(link => link.LocationId).ToArrayAsync(cancellationToken);
                return Results.Created($"/api/access-catalog/package-requests/{item.Id}", item.ToResponse(locationIds));
            },
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<IResult> PreviewPackageRequestApprovals(
        [FromBody] PreviewPackageRequestApprovalsRequest request,
        PackageRequestService service,
        AccessCatalogDbContext db,
        AccessControlDbContext accessControlDb,
        IdentitiesDbContext identitiesDb,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        Result<PackageRequestPreviewModel, AccessCatalogErrors> result = await service.PreviewAsync(
            request.PackageId,
            request.BeneficiaryIdentityId,
            request.LocationIds,
            request.DurationKind,
            request.ValidFrom,
            request.ValidUntil,
            cancellationToken);

        return await result.Match<Task<IResult>>(
            async preview => Results.Ok(await BuildPreviewResponseAsync(request.PackageId, preview, db, accessControlDb, identitiesDb, locationService, cancellationToken)),
            error => Task.FromResult(MapError(error).ToResult()));
    }

    private static async Task<IResult> GetPackageRequest(Guid requestId, AccessCatalogDbContext db, CancellationToken cancellationToken = default)
    {
        PackageRequest? request = await db.PackageRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (request is null)
            return Results.NotFound();

        Guid[] locationIds = await db.PackageRequestLocations.AsNoTracking().Where(link => link.RequestId == requestId).Select(link => link.LocationId).ToArrayAsync(cancellationToken);
        return Results.Ok(request.ToResponse(locationIds));
    }

    private static async Task<IResult> GetPackageRequestDetails(
        Guid requestId,
        AccessCatalogDbContext db,
        AccessControlDbContext accessControlDb,
        IdentitiesDbContext identitiesDb,
        LocationService locationService,
        CancellationToken cancellationToken = default)
    {
        PackageRequest? request = await db.PackageRequests.AsNoTracking().SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
        if (request is null)
            return Results.NotFound();

        Package? package = await db.Packages.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.PackageId, cancellationToken);
        if (package is null)
            return Results.NotFound();

        Guid[] requestedLocationIds = await db.PackageRequestLocations.AsNoTracking()
            .Where(link => link.RequestId == requestId)
            .Select(link => link.LocationId)
            .ToArrayAsync(cancellationToken);

        ApprovalFlow[] flows = await db.ApprovalFlows.AsNoTracking()
            .Where(item => item.RequestId == requestId)
            .OrderBy(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken);

        Guid[] flowIds = flows.Select(item => item.Id).ToArray();
        PackageRequestScope[] scopes = flowIds.Length == 0
            ? []
            : await db.PackageRequestScopes.AsNoTracking()
                .Where(item => flowIds.Contains(item.ApprovalFlowId))
                .ToArrayAsync(cancellationToken);

        ApprovalRequirement[] requirements = flowIds.Length == 0
            ? []
            : await db.ApprovalRequirements.AsNoTracking()
                .Where(item => flowIds.Contains(item.ApprovalFlowId))
                .OrderBy(item => item.CreatedAt)
                .ToArrayAsync(cancellationToken);
        Guid[] requirementIds = requirements.Select(item => item.Id).ToArray();
        ApprovalDecision[] decisions = requirementIds.Length == 0
            ? []
            : await db.ApprovalDecisions.AsNoTracking()
                .Where(item => requirementIds.Contains(item.ApprovalRequirementId))
                .OrderBy(item => item.DecidedAt)
                .ToArrayAsync(cancellationToken);

        AccessGrant[] grants = await db.AccessGrants.AsNoTracking()
            .Where(item => item.SourceKind == AssignmentSourceKind.CatalogRequest && item.SourceId == requestId)
            .OrderBy(item => item.ValidFrom)
            .ToArrayAsync(cancellationToken);

        Guid[] accessItemIds = flows.Select(item => item.AccessItemId)
            .Concat(grants.Where(item => item.AccessItemId.HasValue).Select(item => item.AccessItemId!.Value))
            .Distinct()
            .ToArray();
        Guid[] approvalGroupIds = requirements.Where(item => item.ApprovalGroupId.HasValue).Select(item => item.ApprovalGroupId!.Value).Distinct().ToArray();
        Guid[] approverIds = requirements.Where(item => item.RequiredApproverIdentityId.HasValue).Select(item => item.RequiredApproverIdentityId!.Value)
            .Concat(decisions.Select(item => item.ApproverIdentityId))
            .Distinct()
            .ToArray();

        Dictionary<Guid, AccessItemPreview> accessItemsById = await accessControlDb.AccessItems.AsNoTracking()
            .Where(item => accessItemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => new AccessItemPreview(item.Id, item.Name, item.Description), cancellationToken);
        Dictionary<Guid, string> approvalGroupNames = approvalGroupIds.Length == 0
            ? []
            : await db.ApprovalGroups.AsNoTracking()
                .Where(item => approvalGroupIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        Dictionary<Guid, string> approverDisplayNames = approverIds.Length == 0
            ? []
            : await identitiesDb.Identities.AsNoTracking()
                .Where(item => approverIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);

        Guid[] allLocationIds = requestedLocationIds
            .Concat(scopes.Select(item => item.RequestedLocationId))
            .Concat(flows.Select(item => item.SiteId))
            .Concat(grants.Select(item => item.LocationId))
            .Distinct()
            .ToArray();

        Dictionary<Guid, PackageRequestDetailLocationResponse> locationsById = await BuildLocationResponsesAsync(allLocationIds, locationService, cancellationToken);

        ILookup<Guid, PackageRequestScope> scopesByFlow = scopes.ToLookup(item => item.ApprovalFlowId);
        ILookup<Guid, ApprovalRequirement> requirementsByFlow = requirements.ToLookup(item => item.ApprovalFlowId);
        ILookup<Guid, ApprovalDecision> decisionsByRequirement = decisions.ToLookup(item => item.ApprovalRequirementId);
        ILookup<Guid, AccessGrant> grantsByFlow = grants.Where(item => item.ApprovalFlowId.HasValue).ToLookup(item => item.ApprovalFlowId!.Value);

        PackageRequestDetailFlowResponse[] flowResponses = flows
            .Select(flow =>
            {
                AccessItemPreview? accessItem = accessItemsById.GetValueOrDefault(flow.AccessItemId);
                PackageRequestDetailLocationResponse[] flowLocations = scopesByFlow[flow.Id]
                    .Select(scope => locationsById.GetValueOrDefault(scope.RequestedLocationId))
                    .Where(item => item is not null)
                    .Cast<PackageRequestDetailLocationResponse>()
                    .OrderBy(item => item.Label)
                    .ToArray();

                PackageRequestDetailRequirementResponse[] flowRequirements = requirementsByFlow[flow.Id]
                    .Select(item => new PackageRequestDetailRequirementResponse(
                        item.Id,
                        item.Type,
                        item.Role,
                        item.ApprovalGroupId,
                        item.ApprovalGroupId.HasValue ? approvalGroupNames.GetValueOrDefault(item.ApprovalGroupId.Value) : null,
                        item.RequiredApproverIdentityId,
                        item.RequiredApproverIdentityId.HasValue ? approverDisplayNames.GetValueOrDefault(item.RequiredApproverIdentityId.Value) : null,
                        item.Status,
                        item.SystemApprovalReason,
                        item.CreatedAt,
                        item.CompletedAt,
                        decisionsByRequirement[item.Id]
                            .Select(decision => new PackageRequestDetailDecisionResponse(
                                decision.Id,
                                approverDisplayNames.GetValueOrDefault(decision.ApproverIdentityId, decision.ApproverIdentityId.ToString()),
                                decision.Role,
                                decision.DecisionKind,
                                decision.Note,
                                decision.DecidedAt))
                            .ToArray()))
                    .ToArray();

                PackageRequestDetailGrantResponse[] flowGrants = grantsByFlow[flow.Id]
                    .Select(grant => ToGrantDetailResponse(grant, locationsById, accessItemsById))
                    .OrderBy(item => item.LocationLabel)
                    .ToArray();

                PackageRequestDetailLocationResponse site = locationsById[flow.SiteId];

                return new PackageRequestDetailFlowResponse(
                    flow.Id,
                    flow.AccessItemId,
                    accessItem?.Name ?? flow.AccessItemId.ToString(),
                    accessItem?.Description,
                    flow.SiteId,
                    site.SiteName,
                    flow.Status,
                    flow.CreatedAt,
                    flow.CompletedAt,
                    flowLocations,
                    flowRequirements,
                    flowGrants);
            })
            .OrderBy(item => item.AccessItemName)
            .ThenBy(item => item.SiteName)
            .ToArray();

        PackageRequestDetailGrantResponse[] grantResponses = grants
            .Select(grant => ToGrantDetailResponse(grant, locationsById, accessItemsById))
            .OrderBy(item => item.AccessItemName)
            .ThenBy(item => item.LocationLabel)
            .ToArray();

        PackageRequestDetailLocationResponse[] requestLocations = requestedLocationIds
            .Select(locationId => locationsById.GetValueOrDefault(locationId))
            .Where(item => item is not null)
            .Cast<PackageRequestDetailLocationResponse>()
            .OrderBy(item => item.Label)
            .ToArray();

        return Results.Ok(new PackageRequestDetailResponse(
            request.ToResponse(requestedLocationIds),
            package.ToResponse(),
            requestLocations,
            flowResponses,
            grantResponses));
    }

    private static async Task<Dictionary<Guid, Guid[]>> LoadLocationsAsync(AccessCatalogDbContext db, Guid[] requestIds, CancellationToken cancellationToken)
    {
        return await db.PackageRequestLocations.AsNoTracking()
            .Where(item => requestIds.Contains(item.RequestId))
            .GroupBy(item => item.RequestId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.LocationId).ToArray(), cancellationToken);
    }

    private static async Task<Dictionary<Guid, PackageRequestDetailLocationResponse>> BuildLocationResponsesAsync(
        Guid[] locationIds,
        LocationService locationService,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, PackageRequestDetailLocationResponse> result = [];

        foreach (Guid locationId in locationIds)
        {
            LocationResponse? location = (await locationService.GetLocationById(locationId, cancellationToken))?.ToResponse();
            if (location is null)
                continue;

            result[locationId] = new PackageRequestDetailLocationResponse(location.Id, FormatLocationLabel(location), location.Site.Id, location.Site.Name);
        }

        return result;
    }

    private static PackageRequestDetailGrantResponse ToGrantDetailResponse(
        AccessGrant grant,
        IReadOnlyDictionary<Guid, PackageRequestDetailLocationResponse> locationsById,
        IReadOnlyDictionary<Guid, AccessItemPreview> accessItemsById)
    {
        Guid accessItemId = grant.AccessItemId ?? Guid.Empty;
        AccessItemPreview? accessItem = grant.AccessItemId.HasValue ? accessItemsById.GetValueOrDefault(grant.AccessItemId.Value) : null;
        PackageRequestDetailLocationResponse location = locationsById[grant.LocationId];

        return new PackageRequestDetailGrantResponse(
            grant.Id,
            accessItemId,
            accessItem?.Name ?? accessItemId.ToString(),
            grant.LocationId,
            location.Label,
            grant.Status,
            grant.ApprovalStatus,
            grant.ComplianceStatus,
            grant.CompliantUntil,
            grant.ValidFrom,
            grant.ValidUntil);
    }

    private static string FormatLocationLabel(LocationResponse location) =>
        string.Join(" / ", new[] { location.Site.Name, location.Building?.Name, location.Room?.Name }.Where(item => !string.IsNullOrWhiteSpace(item)));

    private static async Task<PackageRequestPreviewResponse> BuildPreviewResponseAsync(
        Guid packageId,
        PackageRequestPreviewModel preview,
        AccessCatalogDbContext db,
        AccessControlDbContext accessControlDb,
        IdentitiesDbContext identitiesDb,
        LocationService locationService,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ApprovalRequirement> requirements = preview.ApprovalRequirements;
        Guid[] accessItemIds = await db.PackageAccessItems.AsNoTracking()
            .Where(item => item.PackageId == packageId)
            .Select(item => item.AccessItemId)
            .ToArrayAsync(cancellationToken);

        AccessItemPreview[] accessItems = await accessControlDb.AccessItems.AsNoTracking()
            .Where(item => accessItemIds.Contains(item.Id))
            .Select(item => new AccessItemPreview(item.Id, item.Name, item.Description))
            .ToArrayAsync(cancellationToken);

        Guid[] approvalGroupIds = requirements
            .Where(item => item.ApprovalGroupId.HasValue)
            .Select(item => item.ApprovalGroupId!.Value)
            .Distinct()
            .ToArray();

        Guid[] approverIdentityIds = requirements
            .Where(item => item.RequiredApproverIdentityId.HasValue)
            .Select(item => item.RequiredApproverIdentityId!.Value)
            .Distinct()
            .ToArray();

        Dictionary<Guid, ApprovalRequirementPreviewApprovalGroupResponse> approvalGroups = await db.ApprovalGroups.AsNoTracking()
            .Where(item => approvalGroupIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => new ApprovalRequirementPreviewApprovalGroupResponse(item.Id, item.Name),
                cancellationToken);

        Dictionary<Guid, ApprovalRequirementPreviewApproverIdentityResponse> approverIdentities = await identitiesDb.Identities.AsNoTracking()
            .Where(item => approverIdentityIds.Contains(item.Id))
            .ToDictionaryAsync(
                item => item.Id,
                item => new ApprovalRequirementPreviewApproverIdentityResponse(item.Id, item.DisplayName, item.Email),
                cancellationToken);

        Dictionary<Guid, AccessItemPreview> accessItemsById = accessItems.ToDictionary(item => item.Id);
        ILookup<Guid, ApprovalRequirement> requirementsByAccessItemId = requirements.ToLookup(item => item.AccessItemId);

        ApprovalRequirementsPreviewAccessItemResponse[] approvals = accessItemIds
            .Select(accessItemId =>
            {
                AccessItemPreview? accessItem = accessItemsById.GetValueOrDefault(accessItemId);
                ApprovalRequirementPreviewResponse[] itemRequirements = requirementsByAccessItemId[accessItemId]
                    .Select(item => new ApprovalRequirementPreviewResponse(
                        item.LocationId,
                        item.Type,
                        item.Role,
                        item.ApprovalGroupId.HasValue ? approvalGroups.GetValueOrDefault(item.ApprovalGroupId.Value) : null,
                        item.RequiredApproverIdentityId.HasValue ? approverIdentities.GetValueOrDefault(item.RequiredApproverIdentityId.Value) : null))
                    .ToArray();

                return new ApprovalRequirementsPreviewAccessItemResponse(
                    accessItemId,
                    accessItem?.Name ?? string.Empty,
                    accessItem?.Description,
                    itemRequirements);
            })
            .OrderBy(item => item.Name)
            .ToArray();

        Dictionary<Guid, string> locationLabels = [];
        foreach (CompliancePreviewModel item in preview.Compliance)
        {
            LocationResponse? location = (await locationService.GetLocationById(item.LocationId, cancellationToken))?.ToResponse();
            locationLabels[item.LocationId] = location is null ? item.LocationId.ToString() : FormatLocationLabel(location);
        }

        CompliancePreviewLocationResponse[] compliance = preview.Compliance
            .Select(item => new CompliancePreviewLocationResponse(
                item.LocationId,
                locationLabels.GetValueOrDefault(item.LocationId, item.LocationId.ToString()),
                item.Status,
                item.CompliantUntil,
                item.Requirements.Select(requirement => new ComplianceRequirementPreviewResponse(
                    requirement.RequirementDefinitionId,
                    requirement.Code,
                    requirement.Name,
                    requirement.IsBlocking,
                    requirement.Status,
                    requirement.Reason,
                    requirement.ValidUntil)).ToArray()))
            .OrderBy(item => item.LocationLabel)
            .ToArray();

        return new PackageRequestPreviewResponse(approvals, compliance);
    }

    private sealed record AccessItemPreview(Guid Id, string Name, string? Description);

    private static (int statusCode, ProblemDetails? problemDetails) MapError(AccessCatalogErrors error) => error switch
    {
        AccessCatalogErrors.PackageNotFound => Problem(StatusCodes.Status404NotFound, "Package not found."),
        AccessCatalogErrors.PackageInactive => Problem(StatusCodes.Status409Conflict, "Package is inactive."),
        AccessCatalogErrors.IdentityNotFound => Problem(StatusCodes.Status404NotFound, "Identity not found."),
        AccessCatalogErrors.PackageMustContainAccessItems => Problem(StatusCodes.Status409Conflict, "Package must contain at least one access item."),
        AccessCatalogErrors.LocationRequired => Problem(StatusCodes.Status400BadRequest, "At least one valid location is required."),
        AccessCatalogErrors.ReasonRequired => Problem(StatusCodes.Status400BadRequest, "Request reason is required."),
        AccessCatalogErrors.InvalidValidityRange => Problem(StatusCodes.Status400BadRequest, "Invalid approval window."),
        _ => Problem(StatusCodes.Status500InternalServerError, "Unexpected access catalog error.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) => (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
    private static IResult ToResult(this (int statusCode, ProblemDetails? problemDetails) error) => error.problemDetails is null ? Results.StatusCode(error.statusCode) : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
