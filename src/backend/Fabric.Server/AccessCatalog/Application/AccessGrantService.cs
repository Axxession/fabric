using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class AccessGrantService(
    AccessCatalogDbContext db,
    LocationsDbContext locationsDb,
    AccessGrantProvisioningSagaService sagaService)
{
    public async Task<Result<AccessGrant, AccessCatalogErrors>> CreateAsync(
        Guid packageId,
        Guid identityId,
        Guid[] locationIds,
        AssignmentChannel assignmentChannel,
        AssignmentSourceKind sourceKind,
        Guid sourceId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        string reasonText,
        CancellationToken cancellationToken = default)
    {
        if (locationIds.Length == 0)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.LocationRequired);

        Package? package = await db.Packages.SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);
        if (package is null)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.PackageNotFound);

        Guid[] accessItemIds = await db.PackageAccessItems
            .Where(item => item.PackageId == packageId)
            .Select(item => item.AccessItemId)
            .ToArrayAsync(cancellationToken);

        if (accessItemIds.Length == 0)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.PackageMustContainAccessItems);

        int locationCount = await locationsDb.LocationLookups.CountAsync(item => locationIds.Contains(item.Id), cancellationToken);
        if (locationCount != locationIds.Distinct().Count())
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.LocationRequired);

        return await CreateInternalAsync(
            packageId,
            null,
            identityId,
            locationIds,
            assignmentChannel,
            sourceKind,
            sourceId,
            null,
            null,
            durationKind,
            validFrom,
            validUntil,
            reasonText,
            cancellationToken);
    }

    public async Task<Result<AccessGrant, AccessCatalogErrors>> CreateForRequestScopeAsync(
        Guid packageId,
        Guid accessItemId,
        Guid identityId,
        Guid locationId,
        Guid requestId,
        Guid approvalFlowId,
        Guid requestScopeId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        string reasonText,
        CancellationToken cancellationToken = default)
    {
        return await CreateInternalAsync(
            packageId,
            accessItemId,
            identityId,
            [locationId],
            AssignmentChannel.CatalogRequest,
            AssignmentSourceKind.CatalogRequest,
            requestId,
            approvalFlowId,
            requestScopeId,
            durationKind,
            validFrom,
            validUntil,
            reasonText,
            cancellationToken);
    }

    public async Task<Result<AccessGrant, AccessCatalogErrors>> RevokeAsync(
        Guid accessGrantId,
        AccessGrantRevokeCause revokeCause,
        string? revokedBy,
        CancellationToken cancellationToken = default)
    {
        AccessGrant? grant = await db.AccessGrants.SingleOrDefaultAsync(item => item.Id == accessGrantId, cancellationToken);
        if (grant is null)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.AccessGrantNotFound);

        Result<AccessCatalogErrors> revoke = grant.Revoke(revokedBy, revokeCause);
        if (revoke.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await sagaService.EnqueueAccessGrantRevokedAsync(grant.Id, cancellationToken);
        return Result.Success<AccessGrant, AccessCatalogErrors>(grant);
    }

    private async Task<Result<AccessGrant, AccessCatalogErrors>> CreateInternalAsync(
        Guid packageId,
        Guid? accessItemId,
        Guid identityId,
        Guid[] locationIds,
        AssignmentChannel assignmentChannel,
        AssignmentSourceKind sourceKind,
        Guid sourceId,
        Guid? approvalFlowId,
        Guid? requestScopeId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        string reasonText,
        CancellationToken cancellationToken)
    {
        Result<AccessGrant, AccessCatalogErrors> create = AccessGrant.Create(
            packageId,
            identityId,
            assignmentChannel,
            sourceKind,
            sourceId,
            accessItemId,
            approvalFlowId,
            requestScopeId,
            durationKind,
            validFrom,
            validUntil,
            reasonText);

        if (create.IsFailure(out AccessCatalogErrors error))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(error);

        create.IsSuccess(out AccessGrant grant);

        db.AccessGrants.Add(grant);
        foreach (Guid locationId in locationIds.Distinct())
            db.AccessGrantLocations.Add(AccessGrantLocation.Create(grant.Id, locationId));

        await db.SaveChangesAsync(cancellationToken);
        await sagaService.EnqueueAccessGrantCreatedAsync(grant.Id, cancellationToken);
        return Result.Success<AccessGrant, AccessCatalogErrors>(grant);
    }
}
