using AccessControl.Unipass.ChangeSets;
using AccessControl.Unipass.Contracts;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class UnipassPACSProvisioner(
    AccessControlDbContext db,
    PACSSubjectService subjectService,
    PACSSubjectConformityAuditService conformityAuditService,
    UnipassApiFactory apiFactory,
    TimeProvider timeProvider)
{
    public async Task ProvisionAsync(PACSProvisioning provisioning, CancellationToken cancellationToken = default)
    {
        if (provisioning.Status != PACSProvisioningStatus.Pending)
            return;

        (PACSSubjectProvisioningBlockStatus blockStatus, string? blockReason) = await subjectService.GetProvisioningBlockAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
        if (blockStatus != PACSSubjectProvisioningBlockStatus.ProvisioningAllowed)
            return;

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == provisioning.AccessControlSystemId, cancellationToken);
        UnipassAccessLevelTarget? target = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .SingleOrDefaultAsync(item => item.Id == provisioning.AccessLevelTargetId, cancellationToken);

        if (system is null || target is null || system.UnipassConfig is null)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            provisioning.MarkAttemptFailed("System or target not found.", now.AddMinutes(5), now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        Result<PACSSubject, AccessControlErrors> subjectResult = await subjectService.GetOrCreateAsync(provisioning.IdentityId, system, cancellationToken);
        if (subjectResult.IsFailure(out AccessControlErrors error))
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            provisioning.MarkAttemptFailed(error.ToString(), now.AddMinutes(5), now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        subjectResult.IsSuccess(out PACSSubject subject);

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);

        try
        {
            int personId = int.Parse(subject.NativeSubjectId);
            await api.ApplyChangeSet(PersonChangeSet.Update(personId).EnableSite(target.SiteId), cancellationToken);

            AssignedAccessRuleChangeSet changeSet = AssignedAccessRuleChangeSet.Assign(personId, target.SiteId, target.AccessRuleId);
            if (provisioning.DurationKind == PACSAssignmentDurationKind.Temporary)
            {
                changeSet.StartTime(provisioning.ValidFrom);
                if (provisioning.ValidUntil.HasValue)
                    changeSet.EndTime(provisioning.ValidUntil.Value);
            }

            var response = await api.ApplyChangeSet(changeSet, cancellationToken);
            if (!response.Success || string.IsNullOrWhiteSpace(response.Id))
            {
                DateTimeOffset now = timeProvider.GetUtcNow();
                provisioning.MarkAttemptFailed(response.Message ?? "Unipass access rule assignment failed.", now.AddMinutes(5), now);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            provisioning.MarkProvisioned(response.Id, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            await conformityAuditService.EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
        }
        catch (Exception ex)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            provisioning.MarkAttemptFailed(ex.Message, now.AddMinutes(5), now);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAsync(PACSProvisioning provisioning, CancellationToken cancellationToken = default)
    {
        if (provisioning.Status != PACSProvisioningStatus.PendingRevocation)
            return;

        if (string.IsNullOrWhiteSpace(provisioning.NativeAssignmentId))
        {
            db.PACSProvisionings.Remove(provisioning);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == provisioning.AccessControlSystemId, cancellationToken);
        UnipassAccessLevelTarget? target = await db.AccessLevelTargets.OfType<UnipassAccessLevelTarget>().SingleOrDefaultAsync(item => item.Id == provisioning.AccessLevelTargetId, cancellationToken);
        PACSSubject? subject = await db.PACSSubjects.SingleOrDefaultAsync(item => item.IdentityId == provisioning.IdentityId && item.AccessControlSystemId == provisioning.AccessControlSystemId, cancellationToken);

        if (system is null || target is null || subject is null || system.UnipassConfig is null)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            provisioning.MarkAttemptFailed("Unable to revoke provisioning because provider state is incomplete.", now.AddMinutes(5), now);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);

        try
        {
            await api.ApplyChangeSet(AssignedAccessRuleChangeSet.Revoke(int.Parse(subject.NativeSubjectId), target.SiteId, int.Parse(provisioning.NativeAssignmentId)), cancellationToken);
            _ = await db.PACSProvisioningSourceAssignments
                .Where(item => item.PACSProvisioningId == provisioning.Id)
                .ExecuteDeleteAsync(cancellationToken);
            db.PACSProvisionings.Remove(provisioning);
            await db.SaveChangesAsync(cancellationToken);
            await conformityAuditService.EnqueueAsync(provisioning.IdentityId, provisioning.AccessControlSystemId, cancellationToken);
        }
        catch (Exception ex)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            provisioning.MarkAttemptFailed(ex.Message, now.AddMinutes(5), now);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
