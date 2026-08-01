using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class PACSAssignmentService(
    AccessControlDbContext db,
    AccessControlLocationResolver resolver,
    PACSProvisioningReconciliationService reconciliationService,
    TimeProvider timeProvider)
{
    public async Task<Result<IReadOnlyList<PACSAssignment>, AccessControlErrors>> CreateAssignmentsForGrantAsync(
        Guid sourceAssignmentId,
        Guid identityId,
        Guid accessItemId,
        Guid locationId,
        PACSAssignmentDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        CancellationToken cancellationToken = default)
    {
        Result<ResolvedAccessControlSystem, AccessControlErrors> resolvedResult = await resolver.ResolveSystemForLocationAsync(locationId, cancellationToken);
        if (resolvedResult.IsFailure(out AccessControlErrors resolveError))
            return Result.Failure<IReadOnlyList<PACSAssignment>, AccessControlErrors>(resolveError);

        resolvedResult.IsSuccess(out ResolvedAccessControlSystem resolved);

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == resolved.AccessControlSystemId, cancellationToken);
        if (system is null)
            return Result.Failure<IReadOnlyList<PACSAssignment>, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        if (system.Status != AccessControlSystemStatus.Active)
            return Result.Failure<IReadOnlyList<PACSAssignment>, AccessControlErrors>(AccessControlErrors.AccessControlSystemInactive);

        AccessLevelTarget[] targets = await db.AccessLevelTargets
            .Where(target => target.AccessItemId == accessItemId)
            .Where(target => target.AccessControlSystemId == resolved.AccessControlSystemId)
            .Where(target => target.IsEnabled)
            .OrderBy(target => target.Name)
            .ToArrayAsync(cancellationToken);

        if (targets.Length == 0)
            return Result.Failure<IReadOnlyList<PACSAssignment>, AccessControlErrors>(AccessControlErrors.NoAccessLevelTargetsResolved);

        List<PACSAssignment> assignments = [];
        foreach (AccessLevelTarget target in targets)
        {
            PACSAssignment? existing = await db.PACSAssignments.SingleOrDefaultAsync(
                item => item.SourceAssignmentId == sourceAssignmentId && item.AccessLevelTargetId == target.Id && item.IdentityId == identityId,
                cancellationToken);

            if (existing is not null)
            {
                assignments.Add(existing);
                continue;
            }

            Result<PACSAssignment, AccessControlErrors> create = PACSAssignment.Create(
                sourceAssignmentId,
                target.Id,
                resolved.AccessControlSystemId,
                identityId,
                durationKind,
                validFrom,
                validUntil,
                timeProvider.GetUtcNow());

            if (create.IsFailure(out AccessControlErrors error))
                return Result.Failure<IReadOnlyList<PACSAssignment>, AccessControlErrors>(error);

            create.IsSuccess(out PACSAssignment assignment);
            db.PACSAssignments.Add(assignment);
            assignments.Add(assignment);
        }

        await db.SaveChangesAsync(cancellationToken);
        await reconciliationService.EnqueueAsync(identityId, system.Id, cancellationToken);

        return Result.Success<IReadOnlyList<PACSAssignment>, AccessControlErrors>(assignments);
    }

    public async Task<Result<PACSAssignment, AccessControlErrors>> RevokeAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        PACSAssignment? assignment = await db.PACSAssignments.SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        if (assignment is null)
            return Result.Failure<PACSAssignment, AccessControlErrors>(AccessControlErrors.PACSAssignmentNotFound);

        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == assignment.AccessControlSystemId, cancellationToken);
        if (system is null)
            return Result.Failure<PACSAssignment, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        assignment.MarkRevoked(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        await reconciliationService.EnqueueAsync(assignment.IdentityId, system.Id, cancellationToken);
        return Result.Success<PACSAssignment, AccessControlErrors>(assignment);
    }

    public async Task<Result<IReadOnlyList<PACSAssignment>, AccessControlErrors>> RevokeBySourceAssignmentIdAsync(
        Guid sourceAssignmentId,
        CancellationToken cancellationToken = default)
    {
        PACSAssignment[] assignments = await db.PACSAssignments
            .Where(item => item.SourceAssignmentId == sourceAssignmentId)
            .ToArrayAsync(cancellationToken);

        foreach (PACSAssignment assignment in assignments)
        {
            AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == assignment.AccessControlSystemId, cancellationToken);
            if (system is null)
                return Result.Failure<IReadOnlyList<PACSAssignment>, AccessControlErrors>(AccessControlErrors.SystemNotFound);

            assignment.MarkRevoked(timeProvider.GetUtcNow());
            await reconciliationService.EnqueueAsync(assignment.IdentityId, system.Id, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PACSAssignment>, AccessControlErrors>(assignments);
    }
}
