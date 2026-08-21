using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.ContractorJobs;

public sealed class ContractorAssignmentAutomationService(
    SagasDbContext db,
    ContractorsDbContext contractorsDb,
    ReceptionDbContext receptionDb,
    ReceptionService receptionService,
    IdentityService identityService,
    AccessCatalogDbContext accessCatalogDb,
    LocationsDbContext locationsDb,
    AccessGrantService accessGrantService,
    ContractorAssignmentAutomationTrigger trigger,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    public async Task EnqueueAsync(Guid assignmentId, string reason, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO sagas.contractor_assignment_automation_mailboxes
    (id, assignment_id, reason, scheduled_for, last_retry_at, last_known_error, attempt_count, created_at, updated_at, lease_owner, lease_until, tenant_id)
VALUES
    ({Guid.NewGuid()}, {assignmentId}, {reason}, {now}, {null as DateTimeOffset?}, {null as string}, {0}, {now}, {now}, {null as string}, {null as DateTimeOffset?}, {db.TenantId})
ON CONFLICT (tenant_id, assignment_id)
DO UPDATE SET
    reason = EXCLUDED.reason,
    scheduled_for = EXCLUDED.scheduled_for,
    last_retry_at = NULL,
    last_known_error = NULL,
    attempt_count = 0,
    updated_at = EXCLUDED.updated_at;", cancellationToken);
        trigger.Notify();
    }

    public async Task EnqueueAssignmentsAsync(IEnumerable<Guid> assignmentIds, string reason, CancellationToken cancellationToken = default)
    {
        Guid[] distinctAssignmentIds = assignmentIds.Distinct().ToArray();
        foreach (Guid assignmentId in distinctAssignmentIds)
            await EnqueueAsync(assignmentId, reason, cancellationToken);
    }

    public async Task<IReadOnlyList<ContractorAssignmentAutomationWorkItem>> GetDueWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.ContractorAssignmentAutomationMailboxes
            .IgnoreQueryFilters()
            .Where(item => item.ScheduledFor <= now)
            .Where(item => !item.LeaseUntil.HasValue || item.LeaseUntil <= now)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => new ContractorAssignmentAutomationWorkItem(
                EF.Property<string>(item, Infrastructure.Tenancy.TenantDbContext.TenantIdPropertyName),
                item.AssignmentId,
                item.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryAcquireLeaseAsync(Guid assignmentId, string leaseOwner, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        int updated = await db.ContractorAssignmentAutomationMailboxes
            .Where(item => item.AssignmentId == assignmentId)
            .Where(item => item.ScheduledFor <= now)
            .Where(item => !item.LeaseUntil.HasValue || item.LeaseUntil <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.LeaseOwner, leaseOwner)
                .SetProperty(item => item.LeaseUntil, now.Add(LeaseDuration)), cancellationToken);
        return updated == 1;
    }

    public async Task ReconcileAsync(Guid assignmentId, string leaseOwner, CancellationToken cancellationToken = default)
    {
        ContractorAssignmentAutomationMailbox? mailbox = await db.ContractorAssignmentAutomationMailboxes
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.AssignmentId == assignmentId && item.LeaseOwner == leaseOwner, cancellationToken);
        if (mailbox is null)
            return;

        DateTimeOffset baselineUpdatedAt = mailbox.UpdatedAt;

        try
        {
            AssignmentSnapshot? snapshot = await GetAssignmentSnapshotAsync(assignmentId, cancellationToken);
            await ReconcileArrivalAsync(snapshot, assignmentId, cancellationToken);
            await ReconcileAccessAsync(snapshot, assignmentId, cancellationToken);

            int deleted = await db.ContractorAssignmentAutomationMailboxes
                .Where(item => item.AssignmentId == assignmentId)
                .Where(item => item.LeaseOwner == leaseOwner)
                .Where(item => item.UpdatedAt <= baselineUpdatedAt)
                .ExecuteDeleteAsync(cancellationToken);
            if (deleted == 0)
            {
                await ReleaseLeaseAsync(assignmentId, leaseOwner, cancellationToken);
                return;
            }
        }
        catch (Exception ex)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            int updated = await db.ContractorAssignmentAutomationMailboxes
                .Where(item => item.AssignmentId == assignmentId)
                .Where(item => item.LeaseOwner == leaseOwner)
                .Where(item => item.UpdatedAt <= baselineUpdatedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.LastRetryAt, now)
                    .SetProperty(item => item.LastKnownError, ex.Message)
                    .SetProperty(item => item.AttemptCount, item => item.AttemptCount + 1)
                    .SetProperty(item => item.ScheduledFor, GetRetryAt(mailbox.AttemptCount + 1, now))
                    .SetProperty(item => item.UpdatedAt, now)
                    .SetProperty(item => item.LeaseOwner, (string?)null)
                    .SetProperty(item => item.LeaseUntil, (DateTimeOffset?)null), cancellationToken);
            if (updated == 0)
            {
                await ReleaseLeaseAsync(assignmentId, leaseOwner, cancellationToken);
            }
        }
    }

    private async Task ReleaseLeaseAsync(Guid assignmentId, string leaseOwner, CancellationToken cancellationToken)
    {
        _ = await db.ContractorAssignmentAutomationMailboxes
            .Where(item => item.AssignmentId == assignmentId)
            .Where(item => item.LeaseOwner == leaseOwner)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.LeaseOwner, (string?)null)
                .SetProperty(item => item.LeaseUntil, (DateTimeOffset?)null), cancellationToken);
    }

    private async Task ReconcileArrivalAsync(AssignmentSnapshot? snapshot, Guid assignmentId, CancellationToken cancellationToken)
    {
        ExpectedArrival? arrival = await receptionDb.Arrivals
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.JobAssignmentId == assignmentId, cancellationToken);

        if (snapshot is null || !snapshot.IsArrivalRequired)
        {
            if (arrival is not null)
                await receptionService.Cancel(arrival.Id, cancellationToken);

            return;
        }

        Guid? identityId = await identityService.GetIdentityIdForContractorAsync(snapshot.ContractorId, cancellationToken);
        if (arrival is null)
        {
            Result<ExpectedArrival, ReceptionErrors> result = await receptionService.RegisterContractorArrival(
                snapshot.FirstName,
                snapshot.LastName,
                snapshot.CompanyName,
                identityId,
                snapshot.ContractorId,
                snapshot.AssignmentId,
                snapshot.AssignedFrom,
                snapshot.AssignedUntil,
                arrivalCode: null,
                snapshot.LocationId,
                cancellationToken);

            if (result.IsFailure(out ReceptionErrors error))
                throw new InvalidOperationException($"Failed to register contractor arrival for assignment {assignmentId}: {error}.");

            return;
        }

        if (arrival.LocationId != snapshot.LocationId)
        {
            Result<ReceptionErrors> relocate = await receptionService.Relocate(arrival.Id, snapshot.LocationId, cancellationToken);
            if (relocate.IsFailure(out ReceptionErrors error))
                throw new InvalidOperationException($"Failed to relocate contractor arrival for assignment {assignmentId}: {error}.");
        }

        if (arrival.ExpectedArrivalTime != snapshot.AssignedFrom || arrival.ExpectedOffboardTime != snapshot.AssignedUntil)
        {
            Result<ReceptionErrors> reschedule = await receptionService.Reschedule(arrival.Id, snapshot.AssignedFrom, snapshot.AssignedUntil, cancellationToken);
            if (reschedule.IsFailure(out ReceptionErrors error))
                throw new InvalidOperationException($"Failed to reschedule contractor arrival for assignment {assignmentId}: {error}.");
        }
    }

    private async Task ReconcileAccessAsync(AssignmentSnapshot? snapshot, Guid assignmentId, CancellationToken cancellationToken)
    {
        AccessGrant[] existingGrants = await accessCatalogDb.AccessGrants
            .Where(item => item.AssignmentChannel == AssignmentChannel.AutomaticConfiguration)
            .Where(item => item.SourceKind == AssignmentSourceKind.ContractorAssignment)
            .Where(item => item.SourceId == assignmentId)
            .ToArrayAsync(cancellationToken);

        HashSet<(Guid PackageId, Guid LocationId)> desired = [];
        if (snapshot is not null && snapshot.IsGrantRequired)
        {
            Guid? identityId = await identityService.GetIdentityIdForContractorAsync(snapshot.ContractorId, cancellationToken);
            if (identityId.HasValue)
            {
                Guid[] packageIds = await ResolveDesiredPackageIdsAsync(snapshot.JobTypeId, snapshot.LocationId, cancellationToken);
                foreach (Guid packageId in packageIds)
                {
                    desired.Add((packageId, snapshot.LocationId));

                    AccessGrant[] matchingGrants = existingGrants
                        .Where(item => item.Status == AccessGrantStatus.Active)
                        .Where(item => item.PackageId == packageId)
                        .Where(item => item.LocationId == snapshot.LocationId)
                        .ToArray();

                    if (matchingGrants.Length == 0)
                    {
                        Result<IReadOnlyList<AccessGrant>, AccessCatalogErrors> create = await accessGrantService.CreateAsync(
                            packageId,
                            identityId.Value,
                            snapshot.LocationId,
                            AssignmentChannel.AutomaticConfiguration,
                            AssignmentSourceKind.ContractorAssignment,
                            assignmentId,
                            AccessDurationKind.Temporary,
                            snapshot.AssignedFrom,
                            snapshot.AssignedUntil,
                            "Automatic contractor access from contractor job assignment.",
                            cancellationToken);

                        if (create.IsFailure(out AccessCatalogErrors error))
                            throw new InvalidOperationException($"Failed to create automatic contractor access grant for assignment {assignmentId}: {error}.");

                        continue;
                    }

                    foreach (AccessGrant grant in matchingGrants)
                    {
                        if (grant.ValidFrom == snapshot.AssignedFrom && grant.ValidUntil == snapshot.AssignedUntil)
                            continue;

                        Result<AccessGrant, AccessCatalogErrors> update = await accessGrantService.UpdateValidityAsync(
                            grant.Id,
                            snapshot.AssignedFrom,
                            snapshot.AssignedUntil,
                            cancellationToken);

                        if (update.IsFailure(out AccessCatalogErrors error))
                            throw new InvalidOperationException($"Failed to update contractor access grant validity for assignment {assignmentId}: {error}.");
                    }
                }
            }
        }

        foreach (AccessGrant grant in existingGrants.Where(item => item.Status == AccessGrantStatus.Active))
        {
            if (desired.Contains((grant.PackageId, grant.LocationId)))
                continue;

            Result<AccessGrant, AccessCatalogErrors> revoke = await accessGrantService.RevokeAsync(
                grant.Id,
                AccessGrantRevokeCause.ContractorJobAutomation,
                "Contractor job automation",
                cancellationToken);

            if (revoke.IsFailure(out AccessCatalogErrors error))
                throw new InvalidOperationException($"Failed to revoke contractor access grant for assignment {assignmentId}: {error}.");
        }
    }

    private async Task<Guid[]> ResolveDesiredPackageIdsAsync(Guid jobTypeId, Guid locationId, CancellationToken cancellationToken)
    {
        ContractorJobPackageRule[] rules = await db.ContractorJobPackageRules
            .Where(item => item.JobTypeId == jobTypeId && item.IsEnabled)
            .ToArrayAsync(cancellationToken);
        if (rules.Length == 0)
            return [];

        LocationLookup jobLocation = await locationsDb.LocationLookups.SingleAsync(item => item.Id == locationId, cancellationToken);
        Guid[] scopedLocationIds = rules
            .Where(item => item.LocationId.HasValue)
            .Select(item => item.LocationId!.Value)
            .Distinct()
            .ToArray();
        Dictionary<Guid, LocationLookup> scopedLocations = scopedLocationIds.Length == 0
            ? []
            : await locationsDb.LocationLookups
                .Where(item => scopedLocationIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);

        return rules
            .Where(rule => !rule.LocationId.HasValue || (scopedLocations.TryGetValue(rule.LocationId.Value, out LocationLookup? scopedLocation) && IsInScope(jobLocation, scopedLocation)))
            .Select(rule => rule.PackageId)
            .Distinct()
            .ToArray();
    }

    private async Task<AssignmentSnapshot?> GetAssignmentSnapshotAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        return await contractorsDb.ContractorJobAssignments
            .Where(item => item.Id == assignmentId)
            .Join(contractorsDb.ContractorJobs,
                assignment => assignment.ContractorJobId,
                job => job.Id,
                (assignment, job) => new { assignment, job })
            .Join(contractorsDb.Contractors,
                pair => pair.assignment.ContractorId,
                contractor => contractor.Id,
                (pair, contractor) => new { pair.assignment, pair.job, contractor })
            .Join(contractorsDb.Companies,
                pair => pair.contractor.CompanyId,
                company => company.Id,
                (pair, company) => new AssignmentSnapshot(
                    pair.assignment.Id,
                    pair.contractor.Id,
                    pair.contractor.FirstName,
                    pair.contractor.LastName,
                    company.Name,
                    pair.job.JobTypeId,
                    pair.job.LocationId,
                    pair.assignment.AssignedFrom,
                    pair.assignment.AssignedUntil,
                    pair.assignment.Status,
                    pair.job.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static bool IsInScope(LocationLookup target, LocationLookup scope) =>
        scope.Type switch
        {
            LocationType.Site => target.SiteId == scope.SiteId,
            LocationType.Building when scope.BuildingId.HasValue => target.BuildingId == scope.BuildingId,
            LocationType.Room when scope.RoomId.HasValue => target.RoomId == scope.RoomId,
            _ => false
        };

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

    private sealed record AssignmentSnapshot(
        Guid AssignmentId,
        Guid ContractorId,
        string FirstName,
        string LastName,
        string CompanyName,
        Guid JobTypeId,
        Guid LocationId,
        DateTimeOffset AssignedFrom,
        DateTimeOffset AssignedUntil,
        ContractorJobAssignmentStatus AssignmentStatus,
        ContractorJobStatus JobStatus)
    {
        public bool IsArrivalRequired =>
            (AssignmentStatus == ContractorJobAssignmentStatus.Planned || AssignmentStatus == ContractorJobAssignmentStatus.Active)
            && (JobStatus == ContractorJobStatus.Planned || JobStatus == ContractorJobStatus.Active);

        public bool IsGrantRequired => IsArrivalRequired;
    }
}
