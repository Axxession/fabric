using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class CredentialPACSAssignmentService(AccessControlDbContext db, CredentialManagementDbContext credentialDb, TimeProvider timeProvider)
{
    public async Task<Result<CredentialTypeTarget, AccessControlErrors>> CreateCredentialTypeTargetAsync(
        Guid credentialTypeId,
        Guid accessControlSystemId,
        Guid? providerCredentialTypeId,
        ProvisioningTiming provisioningTiming,
        CancellationToken cancellationToken = default)
    {
        bool systemExists = await db.AccessControlSystems.AnyAsync(item => item.Id == accessControlSystemId, cancellationToken);
        if (!systemExists)
            return Result.Failure<CredentialTypeTarget, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        bool exists = await db.CredentialTypeTargets.AnyAsync(item => item.CredentialTypeId == credentialTypeId && item.AccessControlSystemId == accessControlSystemId, cancellationToken);
        if (exists)
            return Result.Failure<CredentialTypeTarget, AccessControlErrors>(AccessControlErrors.CredentialTypeTargetAlreadyExists);

        CredentialTypeTarget target = CredentialTypeTarget.Create(credentialTypeId, accessControlSystemId, providerCredentialTypeId, provisioningTiming, timeProvider.GetUtcNow());
        db.CredentialTypeTargets.Add(target);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialTypeTarget, AccessControlErrors>(target);
    }

    public async Task<Result<CredentialTypeTarget, AccessControlErrors>> UpdateCredentialTypeTargetAsync(
        Guid targetId,
        Guid? providerCredentialTypeId,
        ProvisioningTiming provisioningTiming,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        CredentialTypeTarget? target = await db.CredentialTypeTargets.SingleOrDefaultAsync(item => item.Id == targetId, cancellationToken);
        if (target is null)
            return Result.Failure<CredentialTypeTarget, AccessControlErrors>(AccessControlErrors.CredentialTypeTargetNotFound);

        target.Update(providerCredentialTypeId, provisioningTiming, isEnabled, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialTypeTarget, AccessControlErrors>(target);
    }

    public async Task CreateAssignmentsForCredentialAsync(
        Guid credentialId,
        Guid credentialTypeId,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        CredentialTypeTarget[] targets = await db.CredentialTypeTargets
            .Where(item => item.CredentialTypeId == credentialTypeId && item.IsEnabled)
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

    public async Task<IReadOnlyList<Guid>> GetExpiredProvisionedAssignmentIdsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        (Guid Id, Guid CredentialId)[] provisionedAssignments = await db.CredentialPACSAssignments
            .Where(item => item.Status == CredentialPACSAssignmentStatus.Provisioned)
            .Select(item => new ValueTuple<Guid, Guid>(item.Id, item.CredentialId))
            .ToArrayAsync(cancellationToken);

        if (provisionedAssignments.Length == 0)
            return [];

        Guid[] expiredCredentialIds = await credentialDb.Credentials
            .Where(item => provisionedAssignments.Select(assignment => assignment.CredentialId).Contains(item.Id))
            .Where(item => item.ValidUntil.HasValue && item.ValidUntil.Value <= now)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);

        if (expiredCredentialIds.Length == 0)
            return [];

        HashSet<Guid> expiredCredentialIdSet = [.. expiredCredentialIds];

        return provisionedAssignments
            .Where(item => expiredCredentialIdSet.Contains(item.CredentialId))
            .Select(item => item.Id)
            .ToArray();
    }
}
