using Fabric.Server.Contractors.Application;
using Fabric.Server.Contractors.Contracts;
using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.Contractors.Application;

public sealed class ContractorsServiceTests
{
    [Fact]
    public async Task CreateAssignmentAsync_WhenContractorBelongsToDifferentCompany_ReturnsFailure()
    {
        await using ContractorsDbContext contractorsDb = CreateContractorsDbContext();
        await using LocationsDbContext locationsDb = CreateLocationsDbContext();
        await using IdentitiesDbContext identitiesDb = CreateIdentitiesDbContext();
        IdentityService identityService = new(identitiesDb, TimeProvider.System);
        ContractorsService service = new(contractorsDb, locationsDb, identityService, TimeProvider.System);

        DateTimeOffset now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);
        Result<Company, CompanyErrors> companyAResult = Company.Create("ACME", "Acme", null, now);
        Result<Company, CompanyErrors> companyBResult = Company.Create("BETA", "Beta", null, now);
        Result<JobType, JobTypeErrors> jobTypeResult = JobType.Create("WELD", "Welding", null, now);
        companyAResult.IsSuccess(out Company companyA);
        companyBResult.IsSuccess(out Company companyB);
        jobTypeResult.IsSuccess(out JobType jobType);

        contractorsDb.Companies.AddRange(companyA, companyB);
        contractorsDb.JobTypes.Add(jobType);

        Result<Contractor, ContractorErrors> contractorResult = Contractor.Create(companyA.Id, "Ada", "Lovelace", "ada@example.com", now);
        contractorResult.IsSuccess(out Contractor contractor);
        contractorsDb.Contractors.Add(contractor);

        Result<ContractorJob, ContractorJobErrors> jobResult = ContractorJob.Create(
            companyB.Id,
            jobType.Id,
            Guid.NewGuid(),
            "Server room repair",
            null,
            now,
            now.AddHours(8),
            now);
        jobResult.IsSuccess(out ContractorJob job);
        contractorsDb.ContractorJobs.Add(job);
        await contractorsDb.SaveChangesAsync();

        Result<ContractorJobAssignment, ContractorJobErrors> result = await service.CreateAssignmentAsync(
            job.Id,
            new CreateContractorJobAssignmentRequest(contractor.Id, now.AddHours(1), now.AddHours(4)));

        Assert.True(result.IsFailure(out ContractorJobErrors error));
        Assert.Equal(ContractorJobErrors.ContractorCompanyMismatch, error);
    }

    [Fact]
    public async Task CreateContractorAsync_WhenIdentityMissing_ReturnsFailureAndDoesNotPersist()
    {
        await using ContractorsDbContext contractorsDb = CreateContractorsDbContext();
        await using LocationsDbContext locationsDb = CreateLocationsDbContext();
        await using IdentitiesDbContext identitiesDb = CreateIdentitiesDbContext();
        IdentityService identityService = new(identitiesDb, TimeProvider.System);
        ContractorsService service = new(contractorsDb, locationsDb, identityService, TimeProvider.System);

        Result<Company, CompanyErrors> companyResult = Company.Create("ACME", "Acme", null, DateTimeOffset.UtcNow);
        companyResult.IsSuccess(out Company company);
        contractorsDb.Companies.Add(company);
        await contractorsDb.SaveChangesAsync();

        Result<Contractor, ContractorErrors> result = await service.CreateContractorAsync(
            new CreateContractorRequest("Ada", "Lovelace", "ada@example.com", company.Id, Guid.NewGuid()));

        Assert.True(result.IsFailure(out ContractorErrors error));
        Assert.Equal(ContractorErrors.IdentityNotFound, error);
        Assert.Empty(await contractorsDb.Contractors.ToListAsync());
    }

    private static ContractorsDbContext CreateContractorsDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<ContractorsDbContext> options = new DbContextOptionsBuilder<ContractorsDbContext>()
            .UseInMemoryDatabase($"contractors-{Guid.NewGuid()}")
            .Options;
        return new ContractorsDbContext(options, tenantContext);
    }

    private static LocationsDbContext CreateLocationsDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<LocationsDbContext> options = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase($"locations-{Guid.NewGuid()}")
            .Options;
        return new LocationsDbContext(options, tenantContext);
    }

    private static IdentitiesDbContext CreateIdentitiesDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<IdentitiesDbContext> options = new DbContextOptionsBuilder<IdentitiesDbContext>()
            .UseInMemoryDatabase($"identities-{Guid.NewGuid()}")
            .Options;
        return new IdentitiesDbContext(options, tenantContext);
    }
}
