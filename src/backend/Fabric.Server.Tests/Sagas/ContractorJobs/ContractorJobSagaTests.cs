using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.ContractorJobs;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.Sagas.ContractorJobs;

public sealed class ContractorJobSagaTests
{
    [Fact]
    public async Task GetDueWorkItemsAsync_WhenMailboxExists_ReturnsAssignmentWorkItem()
    {
        await using SagasDbContext sagasDb = CreateSagasDbContext();
        TimeProvider timeProvider = TimeProvider.System;
        ContractorAssignmentAutomationService service = new(
            sagasDb,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ContractorAssignmentAutomationTrigger(),
            timeProvider);

        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid assignmentId = Guid.NewGuid();
        sagasDb.ContractorAssignmentAutomationMailboxes.Add(new ContractorAssignmentAutomationMailbox
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId,
            Reason = "AssignmentCreated",
            ScheduledFor = now,
            AttemptCount = 0,
            CreatedAt = now,
            UpdatedAt = now,
            LeaseOwner = null,
            LeaseUntil = null,
        });
        await sagasDb.SaveChangesAsync();

        IReadOnlyList<ContractorAssignmentAutomationWorkItem> items = await service.GetDueWorkItemsAsync();

        ContractorAssignmentAutomationWorkItem item = Assert.Single(items);
        Assert.Equal(assignmentId, item.AssignmentId);
        Assert.Equal("AssignmentCreated", item.Reason);
    }

    [Fact]
    public async Task CreateRuleAsync_WhenDependenciesExist_PersistsRule()
    {
        await using SagasDbContext sagasDb = CreateSagasDbContext();
        await using ContractorsDbContext contractorsDb = CreateContractorsDbContext();
        await using AccessCatalogDbContext accessCatalogDb = CreateAccessCatalogDbContext();
        await using LocationsDbContext locationsDb = CreateLocationsDbContext();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Result<JobType, JobTypeErrors> jobTypeCreate = JobType.Create("WELD", "Welding", null, now);
        jobTypeCreate.IsSuccess(out JobType jobType);
        contractorsDb.JobTypes.Add(jobType);

        Package package = Package.Create("Contractor package", null);
        accessCatalogDb.Packages.Add(package);

        await contractorsDb.SaveChangesAsync();
        await accessCatalogDb.SaveChangesAsync();

        ContractorAssignmentAutomationService assignmentAutomationService = new(
            sagasDb,
            contractorsDb,
            null!,
            null!,
            null!,
            accessCatalogDb,
            locationsDb,
            null!,
            new ContractorAssignmentAutomationTrigger(),
            TimeProvider.System);
        ContractorJobAccessAutomationService service = new(
            sagasDb,
            contractorsDb,
            accessCatalogDb,
            locationsDb,
            assignmentAutomationService);

        Result<ContractorJobPackageRule, string> result = await service.CreateRuleAsync(jobType.Id, package.Id, locationId: null);

        Assert.True(result.IsSuccess(out ContractorJobPackageRule rule));
        ContractorJobPackageRule persisted = await sagasDb.ContractorJobPackageRules.SingleAsync(item => item.Id == rule.Id);
        Assert.Equal(jobType.Id, persisted.JobTypeId);
        Assert.Equal(package.Id, persisted.PackageId);
        Assert.Null(persisted.LocationId);
        Assert.True(persisted.IsEnabled);
    }

    private static SagasDbContext CreateSagasDbContext() =>
        new(new DbContextOptionsBuilder<SagasDbContext>().UseInMemoryDatabase($"sagas-{Guid.NewGuid()}").Options, new TenantContext());

    private static ContractorsDbContext CreateContractorsDbContext() =>
        new(new DbContextOptionsBuilder<ContractorsDbContext>().UseInMemoryDatabase($"contractors-{Guid.NewGuid()}").Options, new TenantContext());

    private static LocationsDbContext CreateLocationsDbContext() =>
        new(new DbContextOptionsBuilder<LocationsDbContext>().UseInMemoryDatabase($"locations-{Guid.NewGuid()}").Options, new TenantContext());

    private static AccessCatalogDbContext CreateAccessCatalogDbContext() =>
        new(new DbContextOptionsBuilder<AccessCatalogDbContext>().UseInMemoryDatabase($"access-catalog-{Guid.NewGuid()}").Options, new TenantContext());
}
