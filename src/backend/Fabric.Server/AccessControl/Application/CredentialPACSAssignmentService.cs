using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class CredentialPACSAssignmentService(
    AccessControlDbContext db,
    CredentialManagementDbContext credentialDb,
    AccessControlLocationResolver resolver,
    TimeProvider timeProvider)
{
    public async Task<Result<UnipassCredentialTypeTarget, AccessControlErrors>> CreateUnipassCredentialTypeTargetAsync(
        Guid credentialTypeId,
        Guid accessControlSystemId,
        ProvisioningTiming provisioningTiming,
        CancellationToken cancellationToken = default)
    {
        bool systemExists = await db.AccessControlSystems.AnyAsync(item => item.Id == accessControlSystemId, cancellationToken);
        if (!systemExists)
            return Result.Failure<UnipassCredentialTypeTarget, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        AccessControlProviderKind? providerKind = await db.AccessControlSystems
            .Where(item => item.Id == accessControlSystemId)
            .Select(item => (AccessControlProviderKind?)item.ProviderKind)
            .SingleOrDefaultAsync(cancellationToken);

        if (providerKind != AccessControlProviderKind.Unipass)
            return Result.Failure<UnipassCredentialTypeTarget, AccessControlErrors>(AccessControlErrors.SystemProviderNotSupported);

        bool exists = await db.CredentialTypeTargets.AnyAsync(item => item.CredentialTypeId == credentialTypeId && item.AccessControlSystemId == accessControlSystemId, cancellationToken);
        if (exists)
            return Result.Failure<UnipassCredentialTypeTarget, AccessControlErrors>(AccessControlErrors.CredentialTypeTargetAlreadyExists);

        UnipassCredentialTypeTarget target = UnipassCredentialTypeTarget.Create(credentialTypeId, accessControlSystemId, provisioningTiming, timeProvider.GetUtcNow());
        db.CredentialTypeTargets.Add(target);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<UnipassCredentialTypeTarget, AccessControlErrors>(target);
    }

    public async Task<Result<UnipassCredentialTypeTarget, AccessControlErrors>> UpdateUnipassCredentialTypeTargetAsync(
        Guid targetId,
        ProvisioningTiming provisioningTiming,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        UnipassCredentialTypeTarget? target = await db.CredentialTypeTargets
            .OfType<UnipassCredentialTypeTarget>()
            .SingleOrDefaultAsync(item => item.Id == targetId, cancellationToken);
        if (target is null)
            return Result.Failure<UnipassCredentialTypeTarget, AccessControlErrors>(AccessControlErrors.CredentialTypeTargetNotFound);

        target.Update(provisioningTiming, isEnabled, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<UnipassCredentialTypeTarget, AccessControlErrors>(target);
    }

    public async Task CreateAssignmentsForCredentialAsync(
        Guid credentialId,
        Guid credentialTypeId,
        Guid[] locationIds,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        CancellationToken cancellationToken = default)
    {
        if (locationIds.Length == 0)
            return;

        DateTimeOffset now = timeProvider.GetUtcNow();
        IReadOnlyList<ResolvedAccessControlSystem> resolvedSystems = await ResolveDistinctSystemsAsync(locationIds, cancellationToken);
        if (resolvedSystems.Count == 0)
            return;

        Guid[] resolvedSystemIds = resolvedSystems.Select(item => item.AccessControlSystemId).Distinct().ToArray();
        CredentialTypeTarget[] targets = await db.CredentialTypeTargets
            .Where(item => item.CredentialTypeId == credentialTypeId && item.IsEnabled)
            .Where(item => resolvedSystemIds.Contains(item.AccessControlSystemId))
            .ToArrayAsync(cancellationToken);

        foreach (CredentialTypeTarget target in targets)
        {
            bool exists = await db.CredentialPACSAssignments.AnyAsync(item => item.CredentialId == credentialId && item.CredentialTypeTargetId == target.Id, cancellationToken);
            if (exists)
                continue;

            DateTimeOffset scheduledFor = ProvisioningScheduling.GetScheduledFor(target.ProvisioningTiming, validFrom, now);
            db.CredentialPACSAssignments.Add(CredentialPACSAssignment.Create(credentialId, target.Id, target.AccessControlSystemId, scheduledFor, now));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ResolvedAccessControlSystem>> ResolveDistinctSystemsAsync(Guid[] locationIds, CancellationToken cancellationToken)
    {
        List<ResolvedAccessControlSystem> resolved = [];
        HashSet<Guid> seenSystems = [];

        foreach (Guid locationId in locationIds.Distinct())
        {
            Result<ResolvedAccessControlSystem, AccessControlErrors> resolvedResult = await resolver.ResolveSystemForLocationAsync(locationId, cancellationToken);
            if (resolvedResult.IsFailure(out _))
                continue;

            resolvedResult.IsSuccess(out ResolvedAccessControlSystem system);
            if (seenSystems.Add(system.AccessControlSystemId))
                resolved.Add(system);
        }

        return resolved;
    }

    public async Task<IReadOnlyList<Guid>> GetProvisionedAssignmentIdsNeedingRevocationAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        (Guid Id, Guid CredentialId)[] provisionedAssignments = await db.CredentialPACSAssignments
            .Where(item => item.Status == CredentialPACSAssignmentStatus.Provisioned)
            .Select(item => new ValueTuple<Guid, Guid>(item.Id, item.CredentialId))
            .ToArrayAsync(cancellationToken);

        if (provisionedAssignments.Length == 0)
            return [];

        Guid[] revokedOrExpiredCredentialIds = await credentialDb.Credentials
            .Where(item => provisionedAssignments.Select(assignment => assignment.CredentialId).Contains(item.Id))
            .Where(item => item.Status == Fabric.Server.CredentialManagement.Domain.CredentialStatus.Revoked || (item.ValidUntil.HasValue && item.ValidUntil.Value <= now))
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);

        if (revokedOrExpiredCredentialIds.Length == 0)
            return [];

        HashSet<Guid> revokedOrExpiredCredentialIdSet = [.. revokedOrExpiredCredentialIds];

        return provisionedAssignments
            .Where(item => revokedOrExpiredCredentialIdSet.Contains(item.CredentialId))
            .Select(item => item.Id)
            .ToArray();
    }
}
