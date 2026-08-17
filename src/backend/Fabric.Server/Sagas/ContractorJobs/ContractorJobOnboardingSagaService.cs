using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.ContractorJobs;

public sealed class ContractorJobOnboardingSagaService(
    SagasDbContext db,
    ContractorsDbContext contractorsDb,
    ReceptionDbContext receptionDb,
    ReceptionService receptionService,
    IdentityService identityService,
    ContractorJobOnboardingSagaTrigger trigger,
    TimeProvider timeProvider)
{
    public async Task EnqueueAsync(Guid assignmentId, string reason, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        ContractorJobOnboardingReconciliation? existing = await db.ContractorJobOnboardingReconciliations
            .SingleOrDefaultAsync(item => item.AssignmentId == assignmentId, cancellationToken);

        if (existing is null)
            db.ContractorJobOnboardingReconciliations.Add(ContractorJobOnboardingReconciliation.Create(assignmentId, reason, now, now));
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

    public async Task<IReadOnlyList<ContractorJobOnboardingWorkItem>> GetDueWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.ContractorJobOnboardingReconciliations
            .IgnoreQueryFilters()
            .Where(item => item.ScheduledFor <= now)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => new ContractorJobOnboardingWorkItem(
                EF.Property<string>(item, Infrastructure.Tenancy.TenantDbContext.TenantIdPropertyName),
                item.AssignmentId,
                item.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task ReconcileAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        ContractorJobOnboardingReconciliation? reconciliation = await db.ContractorJobOnboardingReconciliations
            .SingleOrDefaultAsync(item => item.AssignmentId == assignmentId, cancellationToken);
        if (reconciliation is null)
            return;

        try
        {
            await ReconcileInternalAsync(assignmentId, cancellationToken);
            db.ContractorJobOnboardingReconciliations.Remove(reconciliation);
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
                    pair.job.LocationId,
                    pair.assignment.AssignedFrom,
                    pair.assignment.AssignedUntil,
                    pair.assignment.Status,
                    pair.job.Status))
            .SingleOrDefaultAsync(cancellationToken);
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

    private sealed record AssignmentSnapshot(
        Guid AssignmentId,
        Guid ContractorId,
        string FirstName,
        string LastName,
        string CompanyName,
        Guid LocationId,
        DateTimeOffset AssignedFrom,
        DateTimeOffset AssignedUntil,
        ContractorJobAssignmentStatus AssignmentStatus,
        ContractorJobStatus JobStatus)
    {
        public bool IsArrivalRequired =>
            (AssignmentStatus == ContractorJobAssignmentStatus.Planned || AssignmentStatus == ContractorJobAssignmentStatus.Active)
            && (JobStatus == ContractorJobStatus.Planned || JobStatus == ContractorJobStatus.Active);
    }
}
