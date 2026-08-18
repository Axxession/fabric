using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.ContractorJobs;

public sealed class ContractorJobAccessAutomationService(
    SagasDbContext db,
    ContractorsDbContext contractorsDb,
    AccessCatalogDbContext accessCatalogDb,
    LocationsDbContext locationsDb,
    ContractorAssignmentAutomationService contractorAssignmentAutomationService)
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
        await contractorAssignmentAutomationService.EnqueueAssignmentsAsync(await GetAssignmentIdsForJobTypeAsync(jobTypeId, cancellationToken), "RuleCreated", cancellationToken);
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
        await contractorAssignmentAutomationService.EnqueueAssignmentsAsync(await GetAssignmentIdsForJobTypeAsync(jobTypeId, cancellationToken), "RuleDeleted", cancellationToken);
        return true;
    }

    public async Task ToggleRuleAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        ContractorJobPackageRule rule = await db.ContractorJobPackageRules.SingleAsync(item => item.Id == id, cancellationToken);
        if (rule.IsEnabled == isEnabled)
            return;

        rule.IsEnabled = isEnabled;
        await db.SaveChangesAsync(cancellationToken);
        await contractorAssignmentAutomationService.EnqueueAssignmentsAsync(await GetAssignmentIdsForJobTypeAsync(rule.JobTypeId, cancellationToken), isEnabled ? "RuleEnabled" : "RuleDisabled", cancellationToken);
    }

    public async Task EnqueueAsync(Guid assignmentId, string reason, CancellationToken cancellationToken = default)
        => await contractorAssignmentAutomationService.EnqueueAsync(assignmentId, reason, cancellationToken);

    public async Task EnqueueAssignmentsAsync(IEnumerable<Guid> assignmentIds, string reason, CancellationToken cancellationToken = default)
        => await contractorAssignmentAutomationService.EnqueueAssignmentsAsync(assignmentIds, reason, cancellationToken);

    public async Task EnqueueAssignmentsForJobTypeAsync(Guid jobTypeId, string reason, CancellationToken cancellationToken = default)
    {
        await contractorAssignmentAutomationService.EnqueueAssignmentsAsync(await GetAssignmentIdsForJobTypeAsync(jobTypeId, cancellationToken), reason, cancellationToken);
    }

    private async Task<Guid[]> GetAssignmentIdsForJobTypeAsync(Guid jobTypeId, CancellationToken cancellationToken) =>
        await contractorsDb.ContractorJobAssignments
            .Join(contractorsDb.ContractorJobs,
                assignment => assignment.ContractorJobId,
                job => job.Id,
                (assignment, job) => new { assignment.Id, job.JobTypeId })
            .Where(item => item.JobTypeId == jobTypeId)
            .Select(item => item.Id)
            .ToArrayAsync(cancellationToken);
}
