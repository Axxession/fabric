using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed record PACSSubjectProvisioningResult(PACSSubject Subject, PACSSubjectProvisioning? Provisioning);

public sealed class PACSSubjectProvisioningService(
    AccessControlDbContext db,
    PACSSubjectService subjectService,
    UnipassPACSSubjectProvisioner unipassProvisioner,
    TimeProvider timeProvider)
{
    public async Task<Result<PACSSubjectProvisioningResult, AccessControlErrors>> UpsertAsync(
        Guid identityId,
        Guid accessControlSystemId,
        PACSSubjectState desiredState,
        string desiredFirstName,
        string desiredLastName,
        string? desiredEmail,
        PACSSubjectProvisioningReason reason,
        PACSSubjectProvisioningSourceKind sourceKind,
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        AccessControlSystem? system = await db.AccessControlSystems.SingleOrDefaultAsync(item => item.Id == accessControlSystemId, cancellationToken);
        if (system is null)
            return Result.Failure<PACSSubjectProvisioningResult, AccessControlErrors>(AccessControlErrors.SystemNotFound);

        Result<PACSSubject, AccessControlErrors> subjectResult = await subjectService.GetOrCreateAsync(identityId, system, cancellationToken);
        if (subjectResult.IsFailure(out AccessControlErrors subjectError))
            return Result.Failure<PACSSubjectProvisioningResult, AccessControlErrors>(subjectError);

        subjectResult.IsSuccess(out PACSSubject subject);

        DateTimeOffset now = timeProvider.GetUtcNow();
        PACSSubjectProvisioning? existing = await db.PACSSubjectProvisionings
            .SingleOrDefaultAsync(item => item.PACSSubjectId == subject.Id, cancellationToken);

        if (existing is null)
        {
            Result<PACSSubjectProvisioning, AccessControlErrors> create = PACSSubjectProvisioning.Create(
                subject.Id,
                desiredState,
                desiredFirstName,
                desiredLastName,
                desiredEmail,
                reason,
                sourceKind,
                sourceId,
                now,
                now);

            if (create.IsFailure(out AccessControlErrors createError))
                return Result.Failure<PACSSubjectProvisioningResult, AccessControlErrors>(createError);

            create.IsSuccess(out existing);
            db.PACSSubjectProvisionings.Add(existing);
        }
        else
        {
            Result<AccessControlErrors> overwrite = existing.Overwrite(
                desiredState,
                desiredFirstName,
                desiredLastName,
                desiredEmail,
                reason,
                sourceKind,
                sourceId,
                now,
                now);

            if (overwrite.IsFailure(out AccessControlErrors overwriteError))
                return Result.Failure<PACSSubjectProvisioningResult, AccessControlErrors>(overwriteError);
        }

        await db.SaveChangesAsync(cancellationToken);
        PACSSubjectProvisioning? remaining = await ApplyAsync(existing.Id, cancellationToken);
        return Result.Success<PACSSubjectProvisioningResult, AccessControlErrors>(new PACSSubjectProvisioningResult(subject, remaining));
    }

    public async Task<PACSSubjectProvisioning?> ApplyAsync(Guid provisioningId, CancellationToken cancellationToken = default)
    {
        PACSSubjectProvisioning? provisioning = await db.PACSSubjectProvisionings
            .SingleOrDefaultAsync(item => item.Id == provisioningId, cancellationToken);
        if (provisioning is null)
            return null;

        PACSSubject? subject = await db.PACSSubjects
            .SingleOrDefaultAsync(item => item.Id == provisioning.PACSSubjectId, cancellationToken);
        if (subject is null)
            return provisioning;

        provisioning.MarkInProgress(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);

        Result<bool, string> result = await ApplyProviderProvisioningAsync(subject.AccessControlSystemId, provisioningId, cancellationToken);
        if (result.IsFailure(out string error))
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            provisioning.MarkFailed(error, GetRetryAt(provisioning.AttemptCount + 1, now), now);
            await db.SaveChangesAsync(cancellationToken);
            return provisioning;
        }

        DateTimeOffset syncedAt = timeProvider.GetUtcNow();
        subject.ApplySynchronizedRepresentation(
            provisioning.DesiredState,
            provisioning.DesiredFirstName,
            provisioning.DesiredLastName,
            provisioning.DesiredEmail,
            syncedAt);
        db.PACSSubjectProvisionings.Remove(provisioning);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<IReadOnlyList<Guid>> GetDueProvisioningIdsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.PACSSubjectProvisionings
            .AsNoTracking()
            .Where(item => item.Status == PACSSubjectProvisioningStatus.Pending || item.Status == PACSSubjectProvisioningStatus.Failed)
            .Where(item => item.ScheduledFor <= now)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<Result<bool, string>> ApplyProviderProvisioningAsync(
        Guid accessControlSystemId,
        Guid provisioningId,
        CancellationToken cancellationToken)
    {
        AccessControlSystem? system = await db.AccessControlSystems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == accessControlSystemId, cancellationToken);

        return system?.ProviderKind switch
        {
            AccessControlProviderKind.Unipass => await unipassProvisioner.ApplyAsync(provisioningId, cancellationToken),
            _ => Result.Failure<bool, string>("System provider not supported.")
        };
    }

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
