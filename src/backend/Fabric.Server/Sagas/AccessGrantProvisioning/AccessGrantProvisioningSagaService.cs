using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.AccessGrantProvisioning;

public sealed class AccessGrantProvisioningSagaService(
    SagasDbContext db,
    AccessCatalogDbContext accessCatalogDb,
    PACSAssignmentService pacsAssignmentService,
    AccessGrantProvisioningSagaTrigger trigger,
    TimeProvider timeProvider)
{
    private const int EventBatchSize = 100;
    private static readonly TimeSpan SuccessfulEventRetention = TimeSpan.FromDays(7);

    public async Task EnqueueAccessGrantCreatedAsync(Guid accessGrantId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        AccessGrantProvisioningSaga saga = await GetOrCreateSagaAsync(accessGrantId, now, cancellationToken);
        saga.State = AccessGrantProvisioningSagaState.PendingProvision;
        saga.FailureReason = null;
        saga.UpdatedAt = now;

        db.AccessGrantProvisioningSagaEvents.Add(AccessGrantProvisioningSagaEvent.Create(
            saga.Id,
            accessGrantId,
            AccessGrantProvisioningSagaEventType.AccessGrantCreated,
            now));

        await db.SaveChangesAsync(cancellationToken);
        trigger.Notify();
    }

    public async Task EnqueueAccessGrantRevokedAsync(Guid accessGrantId, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        AccessGrantProvisioningSaga saga = await GetOrCreateSagaAsync(accessGrantId, now, cancellationToken);
        saga.State = AccessGrantProvisioningSagaState.PendingRevocation;
        saga.FailureReason = null;
        saga.UpdatedAt = now;

        db.AccessGrantProvisioningSagaEvents.Add(AccessGrantProvisioningSagaEvent.Create(
            saga.Id,
            accessGrantId,
            AccessGrantProvisioningSagaEventType.AccessGrantRevoked,
            now));

        await db.SaveChangesAsync(cancellationToken);
        trigger.Notify();
    }

    public async Task<IReadOnlyList<AccessGrantProvisioningSagaEventWorkItem>> GetDueEventWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        return await db.AccessGrantProvisioningSagaEvents
            .IgnoreQueryFilters()
            .Where(item => item.ProcessedAt == null)
            .Where(item => item.NextRetryAt == null || item.NextRetryAt <= now)
            .OrderBy(item => item.CreatedAt)
            .Take(EventBatchSize)
            .Select(item => new AccessGrantProvisioningSagaEventWorkItem(
                EF.Property<string>(item, TenantDbContext.TenantIdPropertyName),
                item.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task ProcessEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        AccessGrantProvisioningSagaEvent? sagaEvent = await db.AccessGrantProvisioningSagaEvents
            .SingleOrDefaultAsync(item => item.Id == eventId && item.ProcessedAt == null, cancellationToken);
        if (sagaEvent is null)
            return;

        AccessGrantProvisioningSaga? saga = await db.AccessGrantProvisioningSagas
            .SingleOrDefaultAsync(item => item.Id == sagaEvent.SagaId, cancellationToken);
        if (saga is null)
        {
            sagaEvent.ProcessedAt = timeProvider.GetUtcNow();
            sagaEvent.FailureReason = "Saga no longer exists.";
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            switch (sagaEvent.Type)
            {
                case AccessGrantProvisioningSagaEventType.AccessGrantCreated:
                    await ProcessCreatedAsync(saga, sagaEvent, cancellationToken);
                    break;
                case AccessGrantProvisioningSagaEventType.AccessGrantRevoked:
                    await ProcessRevokedAsync(saga, sagaEvent, cancellationToken);
                    break;
            }

            DateTimeOffset now = timeProvider.GetUtcNow();
            sagaEvent.ProcessedAt = now;
            sagaEvent.FailureReason = null;
            sagaEvent.NextRetryAt = null;
            if (saga.State != AccessGrantProvisioningSagaState.Failed)
                saga.FailureReason = null;
            saga.RetryCount = 0;
            saga.NextRetryAt = null;
            saga.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            sagaEvent.RetryCount++;
            sagaEvent.FailureReason = ex.Message;
            sagaEvent.NextRetryAt = GetRetryAt(sagaEvent.RetryCount, now);
            saga.FailureReason = ex.Message;
            saga.RetryCount = sagaEvent.RetryCount;
            saga.NextRetryAt = sagaEvent.NextRetryAt;
            saga.State = sagaEvent.Type == AccessGrantProvisioningSagaEventType.AccessGrantRevoked
                ? AccessGrantProvisioningSagaState.PendingRevocation
                : AccessGrantProvisioningSagaState.Failed;
            saga.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CleanupProcessedEventsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset cutoff = timeProvider.GetUtcNow().Subtract(SuccessfulEventRetention);

        _ = await db.AccessGrantProvisioningSagaEvents
            .Where(item => item.ProcessedAt.HasValue)
            .Where(item => item.ProcessedAt < cutoff)
            .Where(item => item.FailureReason == null)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task ProcessCreatedAsync(
        AccessGrantProvisioningSaga saga,
        AccessGrantProvisioningSagaEvent sagaEvent,
        CancellationToken cancellationToken)
    {
        AccessGrant? grant = await accessCatalogDb.AccessGrants
            .SingleOrDefaultAsync(item => item.Id == sagaEvent.AccessGrantId, cancellationToken);
        if (grant is null)
            throw new InvalidOperationException("Access grant not found.");

        if (grant.Status == AccessGrantStatus.Revoked)
        {
            saga.State = AccessGrantProvisioningSagaState.Revoked;
            return;
        }

        Guid[] accessItemIds = grant.AccessItemId.HasValue
            ? [grant.AccessItemId.Value]
            : await accessCatalogDb.PackageAccessItems
                .Where(item => item.PackageId == grant.PackageId)
                .Select(item => item.AccessItemId)
                .ToArrayAsync(cancellationToken);

        Guid[] locationIds = await accessCatalogDb.AccessGrantLocations
            .Where(item => item.AccessGrantId == grant.Id)
            .Select(item => item.LocationId)
            .ToArrayAsync(cancellationToken);

        AccessGrantMaterializationOutcome[] existingOutcomes = await db.AccessGrantMaterializationOutcomes
            .Where(item => item.AccessGrantId == grant.Id)
            .ToArrayAsync(cancellationToken);

        HashSet<(Guid AccessItemId, Guid LocationId)> desiredKeys = [];
        List<string> hardFailureReasons = [];
        DateTimeOffset now = timeProvider.GetUtcNow();

        foreach (Guid locationId in locationIds)
        {
            foreach (Guid accessItemId in accessItemIds)
            {
                desiredKeys.Add((accessItemId, locationId));

                Result<IReadOnlyList<AccessControl.Domain.PACSAssignment>, AccessControl.Domain.AccessControlErrors> result =
                    await pacsAssignmentService.CreateAssignmentsForGrantAsync(
                        grant.Id,
                        grant.IdentityId,
                        accessItemId,
                        locationId,
                        ToDurationKind(grant.DurationKind),
                        grant.ValidFrom,
                        grant.ValidUntil,
                        cancellationToken);

                if (result.IsFailure(out AccessControl.Domain.AccessControlErrors error))
                {
                    if (error == AccessControl.Domain.AccessControlErrors.NoAccessLevelTargetsResolved)
                    {
                        UpsertOutcome(existingOutcomes, grant.Id, accessItemId, locationId, AccessGrantMaterializationOutcomeStatus.SkippedNoTarget, "No enabled access level target is configured for this access item.", now);
                        continue;
                    }

                    string failureReason = $"Failed to create PACS assignments: {error}.";
                    UpsertOutcome(existingOutcomes, grant.Id, accessItemId, locationId, AccessGrantMaterializationOutcomeStatus.Failed, failureReason, now);
                    hardFailureReasons.Add(failureReason);
                    continue;
                }

                UpsertOutcome(existingOutcomes, grant.Id, accessItemId, locationId, AccessGrantMaterializationOutcomeStatus.Created, null, now);
            }
        }

        foreach (AccessGrantMaterializationOutcome obsoleteOutcome in existingOutcomes.Where(item => !desiredKeys.Contains((item.AccessItemId, item.LocationId))))
            db.AccessGrantMaterializationOutcomes.Remove(obsoleteOutcome);

        saga.State = hardFailureReasons.Count == 0
            ? AccessGrantProvisioningSagaState.Provisioned
            : AccessGrantProvisioningSagaState.Failed;
        saga.FailureReason = hardFailureReasons.Count == 0
            ? null
            : $"Failed to create PACS assignments for {hardFailureReasons.Count} grant item(s).";
    }

    private async Task ProcessRevokedAsync(
        AccessGrantProvisioningSaga saga,
        AccessGrantProvisioningSagaEvent sagaEvent,
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<AccessControl.Domain.PACSAssignment>, AccessControl.Domain.AccessControlErrors> result =
            await pacsAssignmentService.RevokeBySourceAssignmentIdAsync(sagaEvent.AccessGrantId, cancellationToken);

        if (result.IsFailure(out AccessControl.Domain.AccessControlErrors error))
            throw new InvalidOperationException($"Failed to revoke PACS assignments: {error}.");

        saga.State = AccessGrantProvisioningSagaState.Revoked;
    }

    private async Task<AccessGrantProvisioningSaga> GetOrCreateSagaAsync(Guid accessGrantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        AccessGrantProvisioningSaga? saga = await db.AccessGrantProvisioningSagas
            .SingleOrDefaultAsync(item => item.AccessGrantId == accessGrantId, cancellationToken);

        if (saga is not null)
            return saga;

        saga = new AccessGrantProvisioningSaga
        {
            Id = Guid.NewGuid(),
            AccessGrantId = accessGrantId,
            State = AccessGrantProvisioningSagaState.PendingProvision,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.AccessGrantProvisioningSagas.Add(saga);
        return saga;
    }

    private void UpsertOutcome(
        AccessGrantMaterializationOutcome[] existingOutcomes,
        Guid accessGrantId,
        Guid accessItemId,
        Guid locationId,
        AccessGrantMaterializationOutcomeStatus status,
        string? failureReason,
        DateTimeOffset now)
    {
        AccessGrantMaterializationOutcome? existing = existingOutcomes.SingleOrDefault(item => item.AccessItemId == accessItemId && item.LocationId == locationId);
        if (existing is null)
        {
            db.AccessGrantMaterializationOutcomes.Add(new AccessGrantMaterializationOutcome
            {
                Id = Guid.NewGuid(),
                AccessGrantId = accessGrantId,
                AccessItemId = accessItemId,
                LocationId = locationId,
                Status = status,
                FailureReason = failureReason,
                CreatedAt = now,
                UpdatedAt = now
            });
            return;
        }

        existing.Status = status;
        existing.FailureReason = failureReason;
        existing.UpdatedAt = now;
    }

    private static DateTimeOffset GetRetryAt(int retryCount, DateTimeOffset now)
    {
        TimeSpan delay = retryCount switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(15)
        };

        return now.Add(delay);
    }

    private static PACSAssignmentDurationKind ToDurationKind(AccessDurationKind durationKind) => durationKind switch
    {
        AccessDurationKind.Permanent => PACSAssignmentDurationKind.Permanent,
        _ => PACSAssignmentDurationKind.Temporary
    };
}
