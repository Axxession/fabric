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
    UnipassApiFactory apiFactory,
    TimeProvider timeProvider)
{
    public async Task ProvisionAsync(PACSProvisioning provisioning, CancellationToken cancellationToken = default)
    {
        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == provisioning.AccessControlSystemId, cancellationToken);
        UnipassAccessLevelTarget? target = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .SingleOrDefaultAsync(item => item.Id == provisioning.AccessLevelTargetId, cancellationToken);

        if (system is null || target is null || system.UnipassConfig is null)
        {
            provisioning.MarkFailed("System or target not found.", timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        Result<PACSSubject, AccessControlErrors> subjectResult = await subjectService.GetOrCreateAsync(provisioning.IdentityId, system, cancellationToken);
        if (subjectResult.IsFailure(out AccessControlErrors error))
        {
            provisioning.MarkFailed(error.ToString(), timeProvider.GetUtcNow());
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
                provisioning.MarkFailed(response.Message ?? "Unipass access rule assignment failed.", timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            provisioning.MarkProvisioned(response.Id, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            provisioning.MarkFailed(ex.Message, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAsync(PACSProvisioning provisioning, CancellationToken cancellationToken = default)
    {
        if (provisioning.Status == PACSProvisioningStatus.Revoked)
            return;

        if (provisioning.Status != PACSProvisioningStatus.Provisioned || string.IsNullOrWhiteSpace(provisioning.NativeAssignmentId))
        {
            provisioning.MarkRevoked(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == provisioning.AccessControlSystemId, cancellationToken);
        UnipassAccessLevelTarget? target = await db.AccessLevelTargets.OfType<UnipassAccessLevelTarget>().SingleOrDefaultAsync(item => item.Id == provisioning.AccessLevelTargetId, cancellationToken);
        PACSSubject? subject = await db.PACSSubjects.SingleOrDefaultAsync(item => item.IdentityId == provisioning.IdentityId && item.AccessControlSystemId == provisioning.AccessControlSystemId, cancellationToken);

        if (system is null || target is null || subject is null || system.UnipassConfig is null)
        {
            provisioning.MarkFailed("Unable to revoke provisioning because provider state is incomplete.", timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);

        try
        {
            await api.ApplyChangeSet(AssignedAccessRuleChangeSet.Revoke(int.Parse(subject.NativeSubjectId), target.SiteId, int.Parse(provisioning.NativeAssignmentId)), cancellationToken);
            provisioning.MarkRevoked(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            provisioning.MarkFailed(ex.Message, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
