using AccessControl.Unipass.ChangeSets;
using AccessControl.Unipass.Contracts;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class UnipassPACSAssignmentProvisioner(
    AccessControlDbContext db,
    PACSSubjectService subjectService,
    UnipassApiFactory apiFactory,
    TimeProvider timeProvider)
{
    public async Task ProvisionAsync(PACSAssignment assignment, CancellationToken cancellationToken = default)
    {
        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == assignment.AccessControlSystemId, cancellationToken);
        UnipassAccessLevelTarget? target = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .SingleOrDefaultAsync(item => item.Id == assignment.AccessLevelTargetId, cancellationToken);

        if (system is null || target is null || system.UnipassConfig is null)
        {
            assignment.MarkFailed("System or target not found.", timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        Result<PACSSubject, AccessControlErrors> subjectResult = await subjectService.GetOrCreateAsync(assignment.IdentityId, system, cancellationToken);
        if (subjectResult.IsFailure(out AccessControlErrors error))
        {
            assignment.MarkFailed(error.ToString(), timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        subjectResult.IsSuccess(out PACSSubject subject);

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);

        try
        {
            int personId = int.Parse(subject.NativeSubjectId);
            await api.ApplyChangeSet(PersonChangeSet.Update(personId).EnableSite(target.SiteId), cancellationToken);
            AssignedAccessRuleChangeSet changeSet = AssignedAccessRuleChangeSet.Assign(personId, target.SiteId, target.AccessRuleId)
                .StartTime(assignment.ValidFrom);

            if (assignment.ValidUntil.HasValue)
                changeSet.EndTime(assignment.ValidUntil.Value);

            var response = await api.ApplyChangeSet(changeSet, cancellationToken);

            if (!response.Success || string.IsNullOrWhiteSpace(response.Id))
            {
                assignment.MarkFailed(response.Message ?? "Unipass access rule assignment failed.", timeProvider.GetUtcNow());
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            assignment.MarkProvisioned(response.Id, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            assignment.MarkFailed(ex.Message, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RevokeAsync(PACSAssignment assignment, CancellationToken cancellationToken = default)
    {
        if (assignment.Status == PACSAssignmentStatus.Revoked)
            return;

        if (assignment.Status != PACSAssignmentStatus.Provisioned || string.IsNullOrWhiteSpace(assignment.NativeAssignmentId))
        {
            assignment.MarkRevoked(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == assignment.AccessControlSystemId, cancellationToken);
        UnipassAccessLevelTarget? target = await db.AccessLevelTargets
            .OfType<UnipassAccessLevelTarget>()
            .SingleOrDefaultAsync(item => item.Id == assignment.AccessLevelTargetId, cancellationToken);
        PACSSubject? subject = await db.PACSSubjects
            .SingleOrDefaultAsync(item => item.IdentityId == assignment.IdentityId && item.AccessControlSystemId == assignment.AccessControlSystemId, cancellationToken);

        if (system is null || target is null || subject is null || system.UnipassConfig is null)
        {
            assignment.MarkFailed("Unable to revoke assignment because provider state is incomplete.", timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);

        try
        {
            await api.ApplyChangeSet(
                AssignedAccessRuleChangeSet.Revoke(int.Parse(subject.NativeSubjectId), target.SiteId, int.Parse(assignment.NativeAssignmentId)),
                cancellationToken);

            assignment.MarkRevoked(timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            assignment.MarkFailed(ex.Message, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
