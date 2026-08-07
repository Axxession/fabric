using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class PACSProvisioningReconciliationService(
    AccessControlDbContext db,
    ITenantContext tenantContext,
    PACSProvisioningReconciliationTrigger trigger,
    PACSSubjectService subjectService,
    PACSSubjectConformityAuditService conformityAuditService,
    UnipassPACSProvisioner unipassProvisioner,
    TimeProvider timeProvider)
{
    public async Task EnqueueAsync(Guid identityId, Guid accessControlSystemId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        PACSProvisioningReconciliation? existing = await db.PACSProvisioningReconciliations
            .SingleOrDefaultAsync(item => item.IdentityId == identityId && item.AccessControlSystemId == accessControlSystemId, cancellationToken);

        if (existing is null)
        {
            db.PACSProvisioningReconciliations.Add(PACSProvisioningReconciliation.Create(identityId, accessControlSystemId, now, now));
        }
        else
        {
            existing.RescheduleNow(now);
        }

        await db.SaveChangesAsync(cancellationToken);
        await trigger.EnqueueAsync(new PACSProvisioningReconciliationWorkItem(tenantContext.TenantId, identityId, accessControlSystemId), cancellationToken);
    }

    public async Task<IReadOnlyList<PACSProvisioningReconciliationWorkItem>> GetDueReconciliationWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.PACSProvisioningReconciliations
            .IgnoreQueryFilters()
            .Where(item => item.ScheduledFor <= now)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => new PACSProvisioningReconciliationWorkItem(
                EF.Property<string>(item, TenantDbContext.TenantIdPropertyName),
                item.IdentityId,
                item.AccessControlSystemId))
            .ToListAsync(cancellationToken);
    }

    public async Task ReconcileAsync(Guid identityId, Guid accessControlSystemId, CancellationToken cancellationToken = default)
    {
        PACSProvisioningReconciliation? reconciliation = await db.PACSProvisioningReconciliations
            .SingleOrDefaultAsync(item => item.IdentityId == identityId && item.AccessControlSystemId == accessControlSystemId, cancellationToken);
        if (reconciliation is null)
            return;

        try
        {
            AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == accessControlSystemId, cancellationToken);
            if (system is null)
                throw new InvalidOperationException("Access control system not found.");

            PACSAssignment[] assignments = await db.PACSAssignments
                .Where(item => item.IdentityId == identityId)
                .Where(item => item.AccessControlSystemId == accessControlSystemId)
                .Where(item => item.Status != PACSAssignmentStatus.Revoked)
                .OrderBy(item => item.ValidFrom)
                .ToArrayAsync(cancellationToken);

            PACSProvisioning[] existingProvisionings = await db.PACSProvisionings
                .Where(item => item.IdentityId == identityId)
                .Where(item => item.AccessControlSystemId == accessControlSystemId)
                .ToArrayAsync(cancellationToken);

            List<DesiredProvisioning> desiredProvisionings = await BuildDesiredProvisioningsAsync(assignments, cancellationToken);

            foreach (DesiredProvisioning desired in desiredProvisionings)
            {
                PACSProvisioning? match = existingProvisionings.SingleOrDefault(item => item.Matches(
                    desired.AccessLevelTargetId,
                    identityId,
                    desired.DurationKind,
                    desired.ValidFrom,
                    desired.ValidUntil,
                    desired.ProvisioningTiming));

                if (match is null)
                {
                    DateTimeOffset scheduledFor = ProvisioningScheduling.GetScheduledFor(
                        desired.ProvisioningTiming,
                        desired.ValidFrom,
                        timeProvider.GetUtcNow());

                    match = PACSProvisioning.Create(
                        desired.AccessLevelTargetId,
                        accessControlSystemId,
                        identityId,
                        desired.DurationKind,
                        desired.ValidFrom,
                        desired.ValidUntil,
                        desired.ProvisioningTiming,
                        scheduledFor);

                    db.PACSProvisionings.Add(match);
                    existingProvisionings = [.. existingProvisionings, match];
                }
                else if (match.Status == PACSProvisioningStatus.PendingRevocation)
                {
                    match.RestoreProvisioned();
                }

                await ReplaceLinksAsync(match.Id, desired.SourceAssignmentIds, cancellationToken);
            }

            foreach (PACSProvisioning existing in existingProvisionings)
            {
                bool stillDesired = desiredProvisionings.Any(desired => existing.Matches(
                    desired.AccessLevelTargetId,
                    identityId,
                    desired.DurationKind,
                    desired.ValidFrom,
                    desired.ValidUntil,
                    desired.ProvisioningTiming));

                if (stillDesired)
                    continue;

                _ = await db.PACSProvisioningSourceAssignments
                    .Where(item => item.PACSProvisioningId == existing.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await RevokeProvisioningAsync(existing, system, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);

            PACSProvisioning[] currentProvisionings = await db.PACSProvisionings
                .Where(item => item.IdentityId == identityId)
                .Where(item => item.AccessControlSystemId == accessControlSystemId)
                .ToArrayAsync(cancellationToken);

            PACSProvisioningSourceAssignment[] currentLinks = await db.PACSProvisioningSourceAssignments
                .Where(item => assignments.Select(x => x.Id).Contains(item.PACSAssignmentId))
                .ToArrayAsync(cancellationToken);

            UpdateAssignmentStatuses(assignments, currentProvisionings, currentLinks, timeProvider.GetUtcNow());

            db.PACSProvisioningReconciliations.Remove(reconciliation);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            reconciliation.MarkFailed(ex.Message, GetRetryAt(reconciliation.AttemptCount + 1, now), now);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Guid>> GetDueProvisioningIdsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<Guid> candidateIds = await db.PACSProvisionings
            .AsNoTracking()
            .Where(item => item.Status == PACSProvisioningStatus.Pending || item.Status == PACSProvisioningStatus.PendingRevocation)
            .Where(item => item.ScheduledFor <= now)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);

        List<Guid> dueIds = [];
        foreach (Guid provisioningId in candidateIds)
        {
            PACSProvisioning? provisioning = await db.PACSProvisionings.AsNoTracking().SingleOrDefaultAsync(item => item.Id == provisioningId, cancellationToken);
            if (provisioning is null)
                continue;

            if (provisioning.Status == PACSProvisioningStatus.PendingRevocation)
            {
                dueIds.Add(provisioningId);
                continue;
            }

            (PACSSubjectProvisioningBlockStatus status, _) = await subjectService.GetProvisioningBlockAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
            if (status == PACSSubjectProvisioningBlockStatus.ProvisioningAllowed)
                dueIds.Add(provisioningId);
        }

        return dueIds;
    }

    public async Task<IReadOnlyList<Guid>> GetExpiredProvisioningIdsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.PACSProvisionings
            .AsNoTracking()
            .Where(item => item.Status == PACSProvisioningStatus.Provisioned)
            .Where(item => item.ValidUntil.HasValue && item.ValidUntil.Value <= now)
            .OrderBy(item => item.ValidUntil)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task ApplyProvisioningAsync(Guid provisioningId, CancellationToken cancellationToken = default)
    {
        PACSProvisioning? provisioning = await db.PACSProvisionings.SingleOrDefaultAsync(item => item.Id == provisioningId, cancellationToken);
        if (provisioning is null)
            return;

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == provisioning.AccessControlSystemId, cancellationToken);
        if (system is null)
            return;

        if (provisioning.Status == PACSProvisioningStatus.PendingRevocation)
        {
            await RevokeProvisioningAsync(provisioning, system, cancellationToken);
            await EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
            await conformityAuditService.EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
            return;
        }

        switch (system.ProviderKind)
        {
            case AccessControlProviderKind.Unipass:
                await unipassProvisioner.ProvisionAsync(provisioning, cancellationToken);
                break;
            default:
                provisioning.MarkAttemptFailed("System provider not supported.", GetRetryAt(1, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                break;
        }

        await EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
        await conformityAuditService.EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
    }

    public async Task RevokeExpiredProvisioningAsync(Guid provisioningId, CancellationToken cancellationToken = default)
    {
        PACSProvisioning? provisioning = await db.PACSProvisionings.SingleOrDefaultAsync(item => item.Id == provisioningId, cancellationToken);
        if (provisioning is null)
            return;

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == provisioning.AccessControlSystemId, cancellationToken);
        if (system is null)
            return;

        provisioning.MarkPendingRevocation(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        await RevokeProvisioningAsync(provisioning, system, cancellationToken);
        await EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
        await conformityAuditService.EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
    }

    private async Task<List<DesiredProvisioning>> BuildDesiredProvisioningsAsync(PACSAssignment[] assignments, CancellationToken cancellationToken)
    {
        Guid[] targetIds = assignments.Select(item => item.AccessLevelTargetId).Distinct().ToArray();
        Dictionary<Guid, AccessLevelTarget> targets = await db.AccessLevelTargets
            .Where(item => targetIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        List<DesiredProvisioning> desired = [];
        foreach (IGrouping<Guid, PACSAssignment> group in assignments.GroupBy(item => item.AccessLevelTargetId))
        {
            AccessLevelTarget target = targets[group.Key];
            PACSAssignment[] targetAssignments = group.OrderBy(item => item.ValidFrom).ToArray();

            if (targetAssignments.Any(item => item.DurationKind == PACSAssignmentDurationKind.Permanent))
            {
                desired.Add(new DesiredProvisioning(
                    target.Id,
                    PACSAssignmentDurationKind.Permanent,
                    targetAssignments.Min(item => item.ValidFrom),
                    null,
                    target.ProvisioningTiming,
                    [.. targetAssignments.Select(item => item.Id)]));
                continue;
            }

            if (targetAssignments.Length == 0)
                continue;

            DateTimeOffset currentFrom = targetAssignments[0].ValidFrom;
            DateTimeOffset currentUntil = targetAssignments[0].ValidUntil!.Value;
            List<Guid> currentIds = [targetAssignments[0].Id];

            foreach (PACSAssignment assignment in targetAssignments.Skip(1))
            {
                DateTimeOffset nextFrom = assignment.ValidFrom;
                DateTimeOffset nextUntil = assignment.ValidUntil!.Value;

                if (nextFrom <= currentUntil)
                {
                    if (nextUntil > currentUntil)
                        currentUntil = nextUntil;

                    currentIds.Add(assignment.Id);
                    continue;
                }

                desired.Add(new DesiredProvisioning(group.Key, PACSAssignmentDurationKind.Temporary, currentFrom, currentUntil, target.ProvisioningTiming, [.. currentIds]));
                currentFrom = nextFrom;
                currentUntil = nextUntil;
                currentIds = [assignment.Id];
            }

            desired.Add(new DesiredProvisioning(group.Key, PACSAssignmentDurationKind.Temporary, currentFrom, currentUntil, target.ProvisioningTiming, [.. currentIds]));
        }

        return desired;
    }

    private async Task ReplaceLinksAsync(Guid provisioningId, Guid[] sourceAssignmentIds, CancellationToken cancellationToken)
    {
        _ = await db.PACSProvisioningSourceAssignments
            .Where(item => item.PACSProvisioningId == provisioningId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (Guid assignmentId in sourceAssignmentIds)
            db.PACSProvisioningSourceAssignments.Add(PACSProvisioningSourceAssignment.Create(provisioningId, assignmentId));
    }

    private async Task RevokeProvisioningAsync(PACSProvisioning provisioning, AccessControlSystem system, CancellationToken cancellationToken)
    {
        if (provisioning.Status != PACSProvisioningStatus.PendingRevocation)
            provisioning.MarkPendingRevocation(timeProvider.GetUtcNow());

        switch (system.ProviderKind)
        {
            case AccessControlProviderKind.Unipass:
                await unipassProvisioner.RevokeAsync(provisioning, cancellationToken);
                break;
            default:
                provisioning.MarkAttemptFailed("System provider not supported.", GetRetryAt(1, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                break;
        }
    }

    private static void UpdateAssignmentStatuses(PACSAssignment[] assignments, PACSProvisioning[] provisionings, PACSProvisioningSourceAssignment[] links, DateTimeOffset now)
    {
        Dictionary<Guid, PACSProvisioningStatus[]> statusesByAssignment = links
            .GroupBy(link => link.PACSAssignmentId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => provisionings.Single(item => item.Id == link.PACSProvisioningId).Status).ToArray());

        foreach (PACSAssignment assignment in assignments)
        {
            if (assignment.Status == PACSAssignmentStatus.Revoked)
                continue;

            if (!statusesByAssignment.TryGetValue(assignment.Id, out PACSProvisioningStatus[]? statuses) || statuses.Length == 0)
            {
                assignment.MarkPending();
                continue;
            }

            if (statuses.Any(status => status == PACSProvisioningStatus.Pending))
            {
                assignment.MarkPending();
                continue;
            }

            assignment.MarkContributingProvisioned();
        }
    }

    private sealed record DesiredProvisioning(
        Guid AccessLevelTargetId,
        PACSAssignmentDurationKind DurationKind,
        DateTimeOffset ValidFrom,
        DateTimeOffset? ValidUntil,
        ProvisioningTiming ProvisioningTiming,
        Guid[] SourceAssignmentIds);

    private static DateTimeOffset GetRetryAt(int attemptCount, DateTimeOffset now)
    {
        TimeSpan delay = attemptCount switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(15)
        };

        return now.Add(delay);
    }
}
