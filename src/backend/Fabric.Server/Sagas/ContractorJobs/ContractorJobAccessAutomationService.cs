using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.ContractorJobs;

public sealed class ContractorJobAccessAutomationService(
    SagasDbContext db,
    ContractorsDbContext contractorsDb,
    AccessCatalogDbContext accessCatalogDb,
    LocationsDbContext locationsDb,
    AccessGrantService accessGrantService,
    IdentityService identityService,
    ContractorJobAccessAutomationTrigger trigger,
    TimeProvider timeProvider)
{
    public async Task<Result<ContractorJobPackageRule, string>> CreateRuleAsync(Guid jobTypeId, Guid packageId, Guid? locationId, CancellationToken cancellationToken = default)
    {
        if (!await contractorsDb.JobTypes.AnyAsync(item => item.Id == jobTypeId, cancellationToken))
            return Result.Failure<ContractorJobPackageRule, string>("Job type not found.");
        if (!await accessCatalogDb.Packages.AnyAsync(item => item.Id == packageId, cancellationToken))
            return Result.Failure<ContractorJobPackageRule, string>("Package not found.");
        if (locationId.HasValue && !await locationsDb.LocationLookups.AnyAsync(item => item.Id == locationId.Value, cancellationToken))
            return Result.Failure<ContractorJobPackageRule, string>("Location not found.");

        bool exists = await db.ContractorJobPackageRules
            .AnyAsync(item => item.JobTypeId == jobTypeId && item.PackageId == packageId && item.LocationId == locationId, cancellationToken);
        if (exists)
            return Result.Failure<ContractorJobPackageRule, string>("Rule already exists.");

        ContractorJobPackageRule rule = new()
        {
            Id = Guid.NewGuid(),
            JobTypeId = jobTypeId,
            PackageId = packageId,
            LocationId = locationId,
            IsEnabled = true
        };

        db.ContractorJobPackageRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        await EnqueueAssignmentsForJobTypeAsync(jobTypeId, "RuleCreated", cancellationToken);
        return Result.Success<ContractorJobPackageRule, string>(rule);
    }

    public async Task<bool> DeleteRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ContractorJobPackageRule? rule = await db.ContractorJobPackageRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return false;

        Guid jobTypeId = rule.JobTypeId;
        db.ContractorJobPackageRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
        await EnqueueAssignmentsForJobTypeAsync(jobTypeId, "RuleDeleted", cancellationToken);
        return true;
    }

    public async Task ToggleRuleAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        ContractorJobPackageRule rule = await db.ContractorJobPackageRules.SingleAsync(item => item.Id == id, cancellationToken);
        if (rule.IsEnabled == isEnabled)
            return;

        rule.IsEnabled = isEnabled;
        await db.SaveChangesAsync(cancellationToken);
        await EnqueueAssignmentsForJobTypeAsync(rule.JobTypeId, isEnabled ? "RuleEnabled" : "RuleDisabled", cancellationToken);
    }

    public async Task EnqueueAsync(Guid assignmentId, string reason, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        ContractorJobAccessAutomationReconciliation? existing = await db.ContractorJobAccessAutomationReconciliations
            .SingleOrDefaultAsync(item => item.AssignmentId == assignmentId, cancellationToken);

        if (existing is null)
            db.ContractorJobAccessAutomationReconciliations.Add(ContractorJobAccessAutomationReconciliation.Create(assignmentId, reason, now, now));
        else
            existing.RescheduleNow(reason, now);

        await db.SaveChangesAsync(cancellationToken);
        trigger.Notify();
    }

    public async Task EnqueueAssignmentsAsync(IEnumerable<Guid> assignmentIds, string reason, CancellationToken cancellationToken = default)
    {
        Guid[] distinctAssignmentIds = assignmentIds.Distinct().ToArray();
        foreach (Guid assignmentId in distinctAssignmentIds)
            await EnqueueAsync(assignmentId, reason, cancellationToken);
    }

    public async Task EnqueueAssignmentsForJobTypeAsync(Guid jobTypeId, string reason, CancellationToken cancellationToken = default)
    {
        Guid[] assignmentIds = await contractorsDb.ContractorJobAssignments
            .Join(contractorsDb.ContractorJobs,
                assignment => assignment.ContractorJobId,
                job => job.Id,
                (assignment, job) => new { assignment.Id, job.JobTypeId })
            .Where(item => item.JobTypeId == jobTypeId)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);

        await EnqueueAssignmentsAsync(assignmentIds, reason, cancellationToken);
    }

    public async Task<IReadOnlyList<ContractorJobAccessAutomationWorkItem>> GetDueWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.ContractorJobAccessAutomationReconciliations
            .IgnoreQueryFilters()
            .Where(item => item.ScheduledFor <= now)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => new ContractorJobAccessAutomationWorkItem(
                EF.Property<string>(item, Infrastructure.Tenancy.TenantDbContext.TenantIdPropertyName),
                item.AssignmentId,
                item.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task ReconcileAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        ContractorJobAccessAutomationReconciliation? reconciliation = await db.ContractorJobAccessAutomationReconciliations
            .SingleOrDefaultAsync(item => item.AssignmentId == assignmentId, cancellationToken);
        if (reconciliation is null)
            return;

        try
        {
            await ReconcileInternalAsync(assignmentId, cancellationToken);
            db.ContractorJobAccessAutomationReconciliations.Remove(reconciliation);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            reconciliation.MarkFailed(ex.Message, GetRetryAt(reconciliation.AttemptCount + 1, now), now);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ReconcileInternalAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        AssignmentSnapshot? snapshot = await GetAssignmentSnapshotAsync(assignmentId, cancellationToken);
        AccessGrant[] existingGrants = await accessCatalogDb.AccessGrants
            .Where(item => item.AssignmentChannel == AssignmentChannel.AutomaticConfiguration)
            .Where(item => item.SourceKind == AssignmentSourceKind.ContractorJob)
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
                            AssignmentSourceKind.ContractorJob,
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
            .Select(item => new AssignmentSnapshot(
                item.assignment.Id,
                item.assignment.ContractorId,
                item.job.JobTypeId,
                item.job.LocationId,
                item.assignment.AssignedFrom,
                item.assignment.AssignedUntil,
                item.assignment.Status,
                item.job.Status))
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
        Guid JobTypeId,
        Guid LocationId,
        DateTimeOffset AssignedFrom,
        DateTimeOffset AssignedUntil,
        ContractorJobAssignmentStatus AssignmentStatus,
        ContractorJobStatus JobStatus)
    {
        public bool IsGrantRequired =>
            (AssignmentStatus == ContractorJobAssignmentStatus.Planned || AssignmentStatus == ContractorJobAssignmentStatus.Active)
            && (JobStatus == ContractorJobStatus.Planned || JobStatus == ContractorJobStatus.Active);
    }
}
