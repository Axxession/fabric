using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Fabric.Server.Sagas.ContractorJobs;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.Sagas.ContractorJobs;

public sealed class ContractorJobSagaTests
{
    [Fact]
    public async Task ReconcileAsync_WhenPlannedAssignmentExists_RegistersAndCancelsExpectedArrival()
    {
        await using SagasDbContext sagasDb = CreateSagasDbContext();
        await using ContractorsDbContext contractorsDb = CreateContractorsDbContext();
        await using LocationsDbContext locationsDb = CreateLocationsDbContext();
        await using IdentitiesDbContext identitiesDb = CreateIdentitiesDbContext();
        await using ReceptionDbContext receptionDb = CreateReceptionDbContext();

        TimeProvider timeProvider = TimeProvider.System;
        Guid locationId = Guid.NewGuid();
        await SeedSiteLocationAsync(locationsDb, locationId);

        (ContractorJob job, ContractorJobAssignment assignment, Contractor contractor) = await SeedContractorAssignmentAsync(contractorsDb, locationId);
        await LinkContractorIdentityAsync(identitiesDb, contractor.Id);

        IdentityService identityService = new(identitiesDb, timeProvider);
        ReceptionLocationScopeService locationScopeService = new(locationsDb);
        ReceptionTriggeredPackageAssignmentService triggeredPackageAssignmentService = new(receptionDb, null!, identityService);
        ReceptionService receptionService = new(receptionDb, timeProvider, locationScopeService, triggeredPackageAssignmentService);
        ContractorJobOnboardingSagaService service = new(
            sagasDb,
            contractorsDb,
            receptionDb,
            receptionService,
            identityService,
            new ContractorJobOnboardingSagaTrigger(),
            timeProvider);

        await service.EnqueueAsync(assignment.Id, "AssignmentCreated");
        await service.ReconcileAsync(assignment.Id);

        ExpectedArrival arrival = await receptionDb.Arrivals.SingleAsync(item => item.JobAssignmentId == assignment.Id);
        Assert.Equal(ArrivalType.Contractor, arrival.Type);
        Assert.Equal(assignment.AssignedFrom, arrival.ExpectedArrivalTime);
        Assert.Equal(assignment.AssignedUntil, arrival.ExpectedOffboardTime);
        Assert.Equal(job.LocationId, arrival.LocationId);

        ContractorJob persistedJob = await contractorsDb.ContractorJobs.Include(item => item.Assignments).SingleAsync(item => item.Id == job.Id);
        Result<ContractorJobErrors> cancel = persistedJob.CancelAssignment(assignment.Id, timeProvider.GetUtcNow());
        Assert.True(cancel.IsSuccess(out _));
        await contractorsDb.SaveChangesAsync();

        await service.EnqueueAsync(assignment.Id, "AssignmentCancelled");
        await service.ReconcileAsync(assignment.Id);

        Assert.False(await receptionDb.Arrivals.AnyAsync(item => item.JobAssignmentId == assignment.Id));
    }

    [Fact]
    public async Task ReconcileAsync_WhenMatchingRuleExists_CreatesAndRevokesAutomaticGrant()
    {
        await using SagasDbContext sagasDb = CreateSagasDbContext();
        await using ContractorsDbContext contractorsDb = CreateContractorsDbContext();
        await using LocationsDbContext locationsDb = CreateLocationsDbContext();
        await using IdentitiesDbContext identitiesDb = CreateIdentitiesDbContext();
        await using AccessCatalogDbContext accessCatalogDb = CreateAccessCatalogDbContext();
        await using AccessControlDbContext accessControlDb = CreateAccessControlDbContext();
        await using RequirementsDbContext requirementsDb = CreateRequirementsDbContext();

        TimeProvider timeProvider = TimeProvider.System;
        Guid locationId = Guid.NewGuid();
        await SeedSiteLocationAsync(locationsDb, locationId);

        (ContractorJob job, ContractorJobAssignment assignment, Contractor contractor) = await SeedContractorAssignmentAsync(contractorsDb, locationId);
        Guid identityId = await LinkContractorIdentityAsync(identitiesDb, contractor.Id);

        Package package = Package.Create("Contractor package", null);
        AccessItem accessItem = AccessItem.Create("Contractor access", null, isComplianceRequired: false);
        accessCatalogDb.Packages.Add(package);
        accessCatalogDb.PackageAccessItems.Add(PackageAccessItem.Create(package.Id, accessItem.Id));
        accessControlDb.AccessItems.Add(accessItem);
        await accessCatalogDb.SaveChangesAsync();
        await accessControlDb.SaveChangesAsync();

        IdentityService identityService = new(identitiesDb, timeProvider);
        AccessGrantProvisioningSagaService provisioningSagaService = new(sagasDb, accessCatalogDb, accessControlDb, null!, new AccessGrantProvisioningSagaTrigger(), timeProvider);
        GrantRequirementsService grantRequirementsService = new(requirementsDb, locationsDb, contractorsDb, identitiesDb, timeProvider);
        AccessGrantComplianceService complianceService = new(accessCatalogDb, accessControlDb, grantRequirementsService, provisioningSagaService, timeProvider);
        AccessGrantService accessGrantService = new(accessCatalogDb, locationsDb, identitiesDb, grantRequirementsService, complianceService, provisioningSagaService, timeProvider);
        ContractorJobAccessAutomationService service = new(
            sagasDb,
            contractorsDb,
            accessCatalogDb,
            locationsDb,
            accessGrantService,
            identityService,
            new ContractorJobAccessAutomationTrigger(),
            timeProvider);

        Result<ContractorJobPackageRule, string> createRule = await service.CreateRuleAsync(job.JobTypeId, package.Id, locationId: null);
        Assert.True(createRule.IsSuccess(out _));

        await service.EnqueueAsync(assignment.Id, "AssignmentCreated");
        await service.ReconcileAsync(assignment.Id);

        AccessGrant grant = await accessCatalogDb.AccessGrants.SingleAsync(item => item.SourceId == assignment.Id && item.SourceKind == AssignmentSourceKind.ContractorJob);
        Assert.Equal(identityId, grant.IdentityId);
        Assert.Equal(package.Id, grant.PackageId);
        Assert.Equal(AccessGrantStatus.Active, grant.Status);
        Assert.Equal(assignment.AssignedFrom, grant.ValidFrom);
        Assert.Equal(assignment.AssignedUntil, grant.ValidUntil);

        ContractorJob persistedJob = await contractorsDb.ContractorJobs.Include(item => item.Assignments).SingleAsync(item => item.Id == job.Id);
        Result<ContractorJobErrors> cancel = persistedJob.CancelAssignment(assignment.Id, timeProvider.GetUtcNow());
        Assert.True(cancel.IsSuccess(out _));
        await contractorsDb.SaveChangesAsync();

        await service.EnqueueAsync(assignment.Id, "AssignmentCancelled");
        await service.ReconcileAsync(assignment.Id);

        grant = await accessCatalogDb.AccessGrants.SingleAsync(item => item.Id == grant.Id);
        Assert.Equal(AccessGrantStatus.Revoked, grant.Status);
        Assert.Equal(AccessGrantRevokeCause.ContractorJobAutomation, grant.RevokeCause);
    }

    private static async Task SeedSiteLocationAsync(LocationsDbContext locationsDb, Guid siteId)
    {
        locationsDb.LocationLookups.Add(LocationLookup.Site(siteId));
        await locationsDb.SaveChangesAsync();
    }

    private static async Task<(ContractorJob Job, ContractorJobAssignment Assignment, Contractor Contractor)> SeedContractorAssignmentAsync(ContractorsDbContext contractorsDb, Guid locationId)
    {
        DateTimeOffset now = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

        Result<Company, CompanyErrors> companyResult = Company.Create("ACME", "Acme Industrial", null, now);
        Result<JobType, JobTypeErrors> jobTypeResult = JobType.Create("WELD", "Welding", null, now);
        companyResult.IsSuccess(out Company company);
        jobTypeResult.IsSuccess(out JobType jobType);
        contractorsDb.Companies.Add(company);
        contractorsDb.JobTypes.Add(jobType);

        Result<Contractor, ContractorErrors> contractorResult = Contractor.Create(company.Id, "Ada", "Lovelace", "ada@example.com", now);
        contractorResult.IsSuccess(out Contractor contractor);
        contractorsDb.Contractors.Add(contractor);

        Result<ContractorJob, ContractorJobErrors> jobResult = ContractorJob.Create(
            company.Id,
            jobType.Id,
            locationId,
            Guid.NewGuid(),
            "Boiler repair",
            null,
            now,
            now.AddHours(8),
            now);
        jobResult.IsSuccess(out ContractorJob job);

        Result<ContractorJobAssignment, ContractorJobErrors> assignmentResult = job.AddAssignment(contractor.Id, now.AddHours(1), now.AddHours(4), now);
        assignmentResult.IsSuccess(out ContractorJobAssignment assignment);

        contractorsDb.ContractorJobs.Add(job);
        await contractorsDb.SaveChangesAsync();
        return (job, assignment, contractor);
    }

    private static async Task<Guid> LinkContractorIdentityAsync(IdentitiesDbContext identitiesDb, Guid contractorId)
    {
        DateTimeOffset now = new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);
        Result<Identity, IdentityErrors> identityResult = Identity.Create("Ada", null, "Lovelace", null, "ada@example.com", null, now);
        identityResult.IsSuccess(out Identity identity);
        Result<ContractorAffiliation, IdentityErrors> affiliationResult = identity.AddContractorAffiliation(contractorId, now, null, now);
        affiliationResult.IsSuccess(out _);
        identitiesDb.Identities.Add(identity);
        await identitiesDb.SaveChangesAsync();
        return identity.Id;
    }

    private static SagasDbContext CreateSagasDbContext() =>
        new(new DbContextOptionsBuilder<SagasDbContext>().UseInMemoryDatabase($"sagas-{Guid.NewGuid()}").Options, new TenantContext());

    private static ContractorsDbContext CreateContractorsDbContext() =>
        new(new DbContextOptionsBuilder<ContractorsDbContext>().UseInMemoryDatabase($"contractors-{Guid.NewGuid()}").Options, new TenantContext());

    private static LocationsDbContext CreateLocationsDbContext() =>
        new(new DbContextOptionsBuilder<LocationsDbContext>().UseInMemoryDatabase($"locations-{Guid.NewGuid()}").Options, new TenantContext());

    private static IdentitiesDbContext CreateIdentitiesDbContext() =>
        new(new DbContextOptionsBuilder<IdentitiesDbContext>().UseInMemoryDatabase($"identities-{Guid.NewGuid()}").Options, new TenantContext());

    private static ReceptionDbContext CreateReceptionDbContext() =>
        new(new DbContextOptionsBuilder<ReceptionDbContext>().UseInMemoryDatabase($"reception-{Guid.NewGuid()}").Options, new TenantContext());

    private static AccessCatalogDbContext CreateAccessCatalogDbContext() =>
        new(new DbContextOptionsBuilder<AccessCatalogDbContext>().UseInMemoryDatabase($"access-catalog-{Guid.NewGuid()}").Options, new TenantContext());

    private static AccessControlDbContext CreateAccessControlDbContext() =>
        new(new DbContextOptionsBuilder<AccessControlDbContext>().UseInMemoryDatabase($"access-control-{Guid.NewGuid()}").Options, new TenantContext());

    private static RequirementsDbContext CreateRequirementsDbContext() =>
        new(new DbContextOptionsBuilder<RequirementsDbContext>().UseInMemoryDatabase($"requirements-{Guid.NewGuid()}").Options, new TenantContext());
}
