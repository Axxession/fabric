using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class PackageRequestService(
    AccessCatalogDbContext db,
    AccessGrantService accessGrantService,
    EmployeesDbContext employeesDb,
    IdentitiesDbContext identitiesDb,
    LocationsDbContext locationsDb,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan ApprovalWindow = TimeSpan.FromDays(7);

    public async Task<Result<PackageRequest, AccessCatalogErrors>> CreateAsync(
        Guid packageId,
        Guid requesterIdentityId,
        Guid beneficiaryIdentityId,
        Guid[] locationIds,
        string requestReason,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        CancellationToken cancellationToken = default)
    {
        if (!await identitiesDb.Identities.AnyAsync(item => item.Id == requesterIdentityId, cancellationToken))
            return Result.Failure<PackageRequest, AccessCatalogErrors>(AccessCatalogErrors.IdentityNotFound);

        Result<ValidatedPackageRequestInputs, AccessCatalogErrors> input = await ValidateRequirementInputsAsync(
            packageId,
            beneficiaryIdentityId,
            locationIds,
            cancellationToken);
        if (input.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<PackageRequest, AccessCatalogErrors>(error);

        input.IsSuccess(out ValidatedPackageRequestInputs value);

        DateTimeOffset now = timeProvider.GetUtcNow();
        Result<PackageRequest, AccessCatalogErrors> create = PackageRequest.Create(
            packageId,
            requesterIdentityId,
            beneficiaryIdentityId,
            requestReason,
            durationKind,
            validFrom,
            validUntil,
            now,
            now.Add(ApprovalWindow));
        if (create.IsFailure(out error))
            return Result.Failure<PackageRequest, AccessCatalogErrors>(error);

        create.IsSuccess(out PackageRequest request);
        db.PackageRequests.Add(request);

        foreach (Guid locationId in value.RequestedLocationIds)
            db.PackageRequestLocations.Add(PackageRequestLocation.Create(request.Id, locationId));

        List<ApprovalFlow> flows = CreateFlows(request, value.AccessItemIds, value.NormalizedSiteIds, now);
        db.ApprovalFlows.AddRange(flows);

        List<PackageRequestScope> scopes = CreateScopes(request.Id, value.AccessItemIds, value.LocationLookups, flows);
        db.PackageRequestScopes.AddRange(scopes);

        List<ApprovalRequirement> requirements = await BuildRequirementsAsync(flows, request.BeneficiaryIdentityId, cancellationToken);
        db.ApprovalRequirements.AddRange(requirements);

        MarkSystemApprovedFlows(flows, requirements, now);

        Result<AccessCatalogErrors> grantResult = await CreateGrantsForApprovedFlowsAsync(request, flows, scopes, cancellationToken);
        if (grantResult.IsFailure(out error))
            return Result.Failure<PackageRequest, AccessCatalogErrors>(error);

        PackageRequestStatusCalculator.ApplySummary(request, flows, now);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<PackageRequest, AccessCatalogErrors>(request);
    }

    public async Task<Result<IReadOnlyList<ApprovalRequirement>, AccessCatalogErrors>> PreviewRequirementsAsync(
        Guid packageId,
        Guid beneficiaryIdentityId,
        Guid[] locationIds,
        CancellationToken cancellationToken = default)
    {
        Result<ValidatedPackageRequestInputs, AccessCatalogErrors> input = await ValidateRequirementInputsAsync(
            packageId,
            beneficiaryIdentityId,
            locationIds,
            cancellationToken);
        if (input.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<IReadOnlyList<ApprovalRequirement>, AccessCatalogErrors>(error);

        input.IsSuccess(out ValidatedPackageRequestInputs value);

        DateTimeOffset now = timeProvider.GetUtcNow();
        PackageRequest previewRequest = PackageRequest.Create(
            packageId,
            Guid.Empty,
            beneficiaryIdentityId,
            "preview",
            AccessDurationKind.Permanent,
            now,
            null,
            now,
            now.Add(ApprovalWindow)).Match(
                item => item,
                _ => throw new InvalidOperationException("Preview request should be valid."));

        List<ApprovalFlow> flows = CreateFlows(previewRequest, value.AccessItemIds, value.NormalizedSiteIds, now);
        List<ApprovalRequirement> requirements = await BuildRequirementsAsync(flows, beneficiaryIdentityId, cancellationToken);

        return Result.Success<IReadOnlyList<ApprovalRequirement>, AccessCatalogErrors>(
            requirements.Where(item => item.Status == ApprovalStatus.Pending).ToArray());
    }

    public async Task<IReadOnlyList<Guid>> GetExpirableRequestIdsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.PackageRequests
            .AsNoTracking()
            .Where(item => item.Status == PackageRequestStatus.InProgress)
            .Where(item => item.ExpiresAt <= now)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExpireAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        PackageRequest? request = await db.PackageRequests
            .SingleOrDefaultAsync(item => item.Id == requestId && item.Status == PackageRequestStatus.InProgress && item.ExpiresAt <= now, cancellationToken);

        if (request is null)
            return false;

        List<ApprovalFlow> flows = await db.ApprovalFlows
            .Where(item => item.RequestId == request.Id)
            .ToListAsync(cancellationToken);

        foreach (ApprovalFlow flow in flows.Where(item => item.Status == ApprovalFlowStatus.InProgress))
            flow.MarkExpired(now);

        PackageRequestStatusCalculator.ApplySummary(request, flows, now);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<List<ApprovalRequirement>> BuildRequirementsAsync(
        IReadOnlyList<ApprovalFlow> flows,
        Guid beneficiaryIdentityId,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<ApprovalRequirement> requirements = [];

        Guid[] accessItemIds = flows.Select(item => item.AccessItemId).Distinct().ToArray();
        Dictionary<Guid, ApprovalDefinition> definitions = await db.ApprovalDefinitions
            .Where(item => accessItemIds.Contains(item.AccessItemId))
            .ToDictionaryAsync(item => item.AccessItemId, cancellationToken);

        Employee? beneficiary = await employeesDb.Employees.SingleOrDefaultAsync(item => item.IdentityId == beneficiaryIdentityId, cancellationToken);

        foreach (ApprovalFlow flow in flows)
        {
            if (!definitions.TryGetValue(flow.AccessItemId, out ApprovalDefinition? definition))
                continue;

            if (definition.DestinationApprovalGroupId.HasValue)
            {
                bool hasApprover = await HasDestinationApproverAsync(definition.DestinationApprovalGroupId.Value, flow.SiteId, cancellationToken);
                requirements.Add(hasApprover
                    ? ApprovalRequirement.CreateDestination(flow.Id, flow.RequestId, flow.AccessItemId, flow.SiteId, definition.DestinationApprovalGroupId.Value, ApprovalDecisionRole.FacilityManager, now)
                    : ApprovalRequirement.CreateSystemApproved(flow.Id, flow.RequestId, flow.AccessItemId, flow.SiteId, ApprovalRequirementType.Destination, ApprovalDecisionRole.FacilityManager, definition.DestinationApprovalGroupId.Value, "No approver configured for request site.", now));
            }

            if (definition.OrganizationalApprovalMode == OrganizationalApprovalMode.ManagerChain)
            {
                for (int level = 1; level <= definition.OrganizationalApprovalLevels; level++)
                {
                    Guid? approverIdentityId = await ResolveManagerIdentityIdAsync(beneficiary, level, cancellationToken);
                    ApprovalDecisionRole role = ToManagerRole(level);

                    requirements.Add(approverIdentityId.HasValue
                        ? ApprovalRequirement.CreateOrganizational(flow.Id, flow.RequestId, flow.AccessItemId, flow.SiteId, approverIdentityId.Value, role, now)
                        : ApprovalRequirement.CreateSystemApproved(flow.Id, flow.RequestId, flow.AccessItemId, flow.SiteId, ApprovalRequirementType.Organizational, role, null, $"No manager configured for organizational approval level L+{level}.", now));
                }
            }
        }

        return requirements;
    }

    private static List<ApprovalFlow> CreateFlows(PackageRequest request, Guid[] accessItemIds, Guid[] normalizedSiteIds, DateTimeOffset now)
    {
        List<ApprovalFlow> flows = [];

        foreach (Guid siteId in normalizedSiteIds)
        {
            foreach (Guid accessItemId in accessItemIds)
                flows.Add(ApprovalFlow.Create(request.Id, request.PackageId, accessItemId, siteId, now));
        }

        return flows;
    }

    private static List<PackageRequestScope> CreateScopes(
        Guid requestId,
        Guid[] accessItemIds,
        IReadOnlyDictionary<Guid, LocationLookup> locationLookups,
        IReadOnlyList<ApprovalFlow> flows)
    {
        Dictionary<(Guid AccessItemId, Guid SiteId), ApprovalFlow> flowsByKey = flows.ToDictionary(item => (item.AccessItemId, item.SiteId));
        List<PackageRequestScope> scopes = [];

        foreach ((Guid requestedLocationId, LocationLookup lookup) in locationLookups)
        {
            foreach (Guid accessItemId in accessItemIds)
            {
                ApprovalFlow flow = flowsByKey[(accessItemId, lookup.SiteId)];
                scopes.Add(PackageRequestScope.Create(requestId, flow.Id, requestedLocationId));
            }
        }

        return scopes;
    }

    private static void MarkSystemApprovedFlows(
        IReadOnlyList<ApprovalFlow> flows,
        IReadOnlyList<ApprovalRequirement> requirements,
        DateTimeOffset now)
    {
        ILookup<Guid, ApprovalRequirement> requirementsByFlow = requirements.ToLookup(item => item.ApprovalFlowId);

        foreach (ApprovalFlow flow in flows)
        {
            if (requirementsByFlow[flow.Id].All(item => item.Status == ApprovalStatus.SystemApproved))
                flow.MarkSystemApproved(now);
        }
    }

    private async Task<Result<AccessCatalogErrors>> CreateGrantsForApprovedFlowsAsync(
        PackageRequest request,
        IReadOnlyList<ApprovalFlow> flows,
        IReadOnlyList<PackageRequestScope> scopes,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, ApprovalFlow> approvedFlows = flows
            .Where(item => item.Status is ApprovalFlowStatus.Approved or ApprovalFlowStatus.SystemApproved)
            .ToDictionary(item => item.Id);

        foreach (PackageRequestScope scope in scopes)
        {
            if (!approvedFlows.TryGetValue(scope.ApprovalFlowId, out ApprovalFlow? flow))
                continue;

            bool grantExists = await db.AccessGrants.AnyAsync(item => item.RequestScopeId == scope.Id, cancellationToken);
            if (grantExists)
                continue;

            Result<AccessGrant, AccessCatalogErrors> grantResult = await accessGrantService.CreateForRequestScopeAsync(
                request.PackageId,
                flow.AccessItemId,
                request.BeneficiaryIdentityId,
                scope.RequestedLocationId,
                request.Id,
                flow.Id,
                scope.Id,
                request.DurationKind,
                request.ValidFrom,
                request.ValidUntil,
                request.RequestReason,
                cancellationToken);

            if (grantResult.IsFailure(out AccessCatalogErrors error))
                return Result.Failure(error);
        }

        return Result.Success<AccessCatalogErrors>();
    }

    private async Task<Result<ValidatedPackageRequestInputs, AccessCatalogErrors>> ValidateRequirementInputsAsync(
        Guid packageId,
        Guid beneficiaryIdentityId,
        Guid[] locationIds,
        CancellationToken cancellationToken)
    {
        if (locationIds.Length == 0)
            return Result.Failure<ValidatedPackageRequestInputs, AccessCatalogErrors>(AccessCatalogErrors.LocationRequired);

        Package? package = await db.Packages.SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);
        if (package is null)
            return Result.Failure<ValidatedPackageRequestInputs, AccessCatalogErrors>(AccessCatalogErrors.PackageNotFound);

        if (package.Status != PackageStatus.Active)
            return Result.Failure<ValidatedPackageRequestInputs, AccessCatalogErrors>(AccessCatalogErrors.PackageInactive);

        if (!await identitiesDb.Identities.AnyAsync(item => item.Id == beneficiaryIdentityId, cancellationToken))
            return Result.Failure<ValidatedPackageRequestInputs, AccessCatalogErrors>(AccessCatalogErrors.IdentityNotFound);

        Guid[] requestedLocationIds = locationIds.Distinct().ToArray();
        LocationLookup[] lookups = await locationsDb.LocationLookups
            .Where(item => requestedLocationIds.Contains(item.Id))
            .ToArrayAsync(cancellationToken);
        if (lookups.Length != requestedLocationIds.Length)
            return Result.Failure<ValidatedPackageRequestInputs, AccessCatalogErrors>(AccessCatalogErrors.LocationRequired);

        Guid[] accessItemIds = await db.PackageAccessItems
            .Where(item => item.PackageId == packageId)
            .Select(item => item.AccessItemId)
            .ToArrayAsync(cancellationToken);

        if (accessItemIds.Length == 0)
            return Result.Failure<ValidatedPackageRequestInputs, AccessCatalogErrors>(AccessCatalogErrors.PackageMustContainAccessItems);

        Dictionary<Guid, LocationLookup> lookupsById = lookups.ToDictionary(item => item.Id);
        Guid[] normalizedSiteIds = lookups.Select(item => item.SiteId).Distinct().ToArray();

        return Result.Success<ValidatedPackageRequestInputs, AccessCatalogErrors>(
            new ValidatedPackageRequestInputs(accessItemIds, requestedLocationIds, lookupsById, normalizedSiteIds));
    }

    private async Task<bool> HasDestinationApproverAsync(Guid approvalGroupId, Guid siteId, CancellationToken cancellationToken)
    {
        return await db.ApprovalGroupMembers
            .AnyAsync(item => item.ApprovalGroupId == approvalGroupId && item.ResponsibleLocationId == siteId, cancellationToken);
    }

    private async Task<Guid?> ResolveManagerIdentityIdAsync(Employee? employee, int level, CancellationToken cancellationToken)
    {
        if (employee is null)
            return null;

        Guid? managerEmployeeId = employee.ManagerEmployeeId;
        for (int currentLevel = 1; currentLevel <= level; currentLevel++)
        {
            if (!managerEmployeeId.HasValue)
                return null;

            Employee? manager = await employeesDb.Employees.SingleOrDefaultAsync(item => item.Id == managerEmployeeId.Value, cancellationToken);
            if (manager is null)
                return null;

            if (currentLevel == level)
                return manager.IdentityId;

            managerEmployeeId = manager.ManagerEmployeeId;
        }

        return null;
    }

    private static ApprovalDecisionRole ToManagerRole(int level) => level switch
    {
        1 => ApprovalDecisionRole.L1,
        2 => ApprovalDecisionRole.L2,
        _ => ApprovalDecisionRole.L3
    };

    private sealed record ValidatedPackageRequestInputs(
        Guid[] AccessItemIds,
        Guid[] RequestedLocationIds,
        Dictionary<Guid, LocationLookup> LocationLookups,
        Guid[] NormalizedSiteIds);
}
