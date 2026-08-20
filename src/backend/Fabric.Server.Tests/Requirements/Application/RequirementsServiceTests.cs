using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.Requirements.Application;

public sealed class RequirementsServiceTests
{
    [Fact]
    public async Task EvaluateGrantRequirementsAsync_WhenMultipleEvidenceFulfillSameRequirement_UsesEarliestValidUntil()
    {
        DateTimeOffset now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);

        await using RequirementsDbContext requirementsDb = CreateRequirementsDbContext();
        await using LocationsDbContext locationsDb = CreateLocationsDbContext();
        await using ContractorsDbContext contractorsDb = CreateContractorsDbContext();
        await using IdentitiesDbContext identitiesDb = CreateIdentitiesDbContext();

        Result<Identity, IdentityErrors> identityCreate = Identity.Create("Ada", null, "Lovelace", null, "ada@example.com", null, now);
        identityCreate.IsSuccess(out Identity identity);
        identitiesDb.Identities.Add(identity);

        Result<RequirementDefinition, RequirementDefinitionErrors> requirementCreate = RequirementDefinition.Create("site_training", "Site Training", null, RequirementFulfillmentKind.Document, false, now);
        requirementCreate.IsSuccess(out RequirementDefinition requirement);
        requirementsDb.RequirementDefinitions.Add(requirement);

        Result<RequirementEvidence, RequirementEvidenceErrors> evidenceEarlyCreate = RequirementEvidence.Create(identity.Id, requirement.Id, RequirementEvidenceKind.UploadedDocument, RequirementEvidenceStatus.Valid, now.AddDays(-2), now.AddDays(1), null, "Old site training", false, now, null, null, now);
        Result<RequirementEvidence, RequirementEvidenceErrors> evidenceLateCreate = RequirementEvidence.Create(identity.Id, requirement.Id, RequirementEvidenceKind.UploadedDocument, RequirementEvidenceStatus.Valid, now.AddDays(-1), now.AddDays(3), null, "New site training", false, now, null, null, now);
        evidenceEarlyCreate.IsSuccess(out RequirementEvidence evidenceEarly);
        evidenceLateCreate.IsSuccess(out RequirementEvidence evidenceLate);

        requirementsDb.RequirementEvidence.AddRange(evidenceEarly, evidenceLate);

        await identitiesDb.SaveChangesAsync();
        await requirementsDb.SaveChangesAsync();

        GrantRequirementsService service = new(requirementsDb, locationsDb, contractorsDb, identitiesDb, new FixedTimeProvider(now));

        IReadOnlyList<EvaluatedGrantRequirement> result = await service.EvaluateGrantRequirementsAsync(identity.Id, [requirement.Id]);

        EvaluatedGrantRequirement evaluation = Assert.Single(result);
        Assert.Equal(RequirementResultStatus.Fulfilled, evaluation.Status);
        Assert.Equal(now.AddDays(1), evaluation.ValidUntil);
        Assert.Equal(RequirementEvidenceKind.UploadedDocument, evaluation.EvidenceKind);
    }

    private static RequirementsDbContext CreateRequirementsDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<RequirementsDbContext> options = new DbContextOptionsBuilder<RequirementsDbContext>()
            .UseInMemoryDatabase($"requirements-{Guid.NewGuid()}")
            .Options;
        return new RequirementsDbContext(options, tenantContext);
    }

    private static LocationsDbContext CreateLocationsDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<LocationsDbContext> options = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase($"locations-{Guid.NewGuid()}")
            .Options;
        return new LocationsDbContext(options, tenantContext);
    }

    private static ContractorsDbContext CreateContractorsDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<ContractorsDbContext> options = new DbContextOptionsBuilder<ContractorsDbContext>()
            .UseInMemoryDatabase($"contractors-{Guid.NewGuid()}")
            .Options;
        return new ContractorsDbContext(options, tenantContext);
    }

    private static IdentitiesDbContext CreateIdentitiesDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<IdentitiesDbContext> options = new DbContextOptionsBuilder<IdentitiesDbContext>()
            .UseInMemoryDatabase($"identities-{Guid.NewGuid()}")
            .Options;
        return new IdentitiesDbContext(options, tenantContext);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
