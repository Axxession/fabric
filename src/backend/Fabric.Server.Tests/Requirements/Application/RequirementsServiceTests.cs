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
    public async Task EvaluateForLocationAsync_WhenMultipleEvidenceFulfillSameRequirement_UsesMaxThenMinExpiry()
    {
        DateTimeOffset now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);

        await using RequirementsDbContext requirementsDb = CreateRequirementsDbContext();
        await using LocationsDbContext locationsDb = CreateLocationsDbContext();
        await using ContractorsDbContext contractorsDb = CreateContractorsDbContext();
        await using IdentitiesDbContext identitiesDb = CreateIdentitiesDbContext();
        await using AccessControlDbContext accessControlDb = CreateAccessControlDbContext();

        Guid siteId = Guid.NewGuid();
        locationsDb.LocationLookups.Add(LocationLookup.Site(siteId));

        Result<Identity, IdentityErrors> identityCreate = Identity.Create("Ada", null, "Lovelace", null, "ada@example.com", null, now);
        identityCreate.IsSuccess(out Identity identity);
        identitiesDb.Identities.Add(identity);

        Result<EnforcementZone, EnforcementZoneErrors> zoneCreate = EnforcementZone.Create("HQ", "Headquarters", null, now);
        zoneCreate.IsSuccess(out EnforcementZone zone);
        requirementsDb.EnforcementZones.Add(zone);
        requirementsDb.EnforcementZoneLocations.Add(EnforcementZoneLocation.Create(zone.Id, siteId, now));

        Result<RequirementDefinition, RequirementDefinitionErrors> requirementOneCreate = RequirementDefinition.Create("site_training", "Site Training", null, RequirementEvaluatorKind.UploadedDocument, false, now);
        Result<RequirementDefinition, RequirementDefinitionErrors> requirementTwoCreate = RequirementDefinition.Create("onboarded", "Onboarded", null, RequirementEvaluatorKind.UploadedDocument, false, now);
        requirementOneCreate.IsSuccess(out RequirementDefinition requirementOne);
        requirementTwoCreate.IsSuccess(out RequirementDefinition requirementTwo);

        requirementsDb.RequirementDefinitions.AddRange(requirementOne, requirementTwo);
        requirementsDb.ZoneRequirementPolicies.AddRange(
            ZoneRequirementPolicy.Create(zone.Id, requirementOne.Id, RequirementSubjectKind.Visitor, true, now),
            ZoneRequirementPolicy.Create(zone.Id, requirementTwo.Id, RequirementSubjectKind.Visitor, true, now));

        Result<RequirementEvidence, RequirementEvidenceErrors> evidenceOneEarlyCreate = RequirementEvidence.Create(identity.Id, requirementOne.Id, RequirementEvidenceKind.UploadedDocument, RequirementEvidenceStatus.Valid, now.AddDays(-2), now.AddDays(1), null, "Old site training", false, now, null, null, now);
        Result<RequirementEvidence, RequirementEvidenceErrors> evidenceOneLateCreate = RequirementEvidence.Create(identity.Id, requirementOne.Id, RequirementEvidenceKind.UploadedDocument, RequirementEvidenceStatus.Valid, now.AddDays(-1), now.AddDays(3), null, "New site training", false, now, null, null, now);
        Result<RequirementEvidence, RequirementEvidenceErrors> evidenceTwoCreate = RequirementEvidence.Create(identity.Id, requirementTwo.Id, RequirementEvidenceKind.Onboarded, RequirementEvidenceStatus.Valid, now, now.AddDays(2), null, "Onboarded", false, now, null, null, now);
        evidenceOneEarlyCreate.IsSuccess(out RequirementEvidence evidenceOneEarly);
        evidenceOneLateCreate.IsSuccess(out RequirementEvidence evidenceOneLate);
        evidenceTwoCreate.IsSuccess(out RequirementEvidence evidenceTwo);

        requirementsDb.RequirementEvidence.AddRange(evidenceOneEarly, evidenceOneLate, evidenceTwo);

        await locationsDb.SaveChangesAsync();
        await identitiesDb.SaveChangesAsync();
        await requirementsDb.SaveChangesAsync();

        RequirementsLocationResolver resolver = new(requirementsDb, locationsDb);
        RequirementsService service = new(requirementsDb, locationsDb, contractorsDb, identitiesDb, accessControlDb, null!, resolver, new FixedTimeProvider(now));

        Result<IReadOnlyList<ZoneCompliance>, RequirementsEvaluationErrors> result = await service.EvaluateForLocationAsync(
            new EvaluateZoneComplianceRequest(identity.Id, RequirementSubjectKind.Visitor, siteId));

        Assert.True(result.IsSuccess(out IReadOnlyList<ZoneCompliance> compliances));
        ZoneCompliance compliance = Assert.Single(compliances);
        Assert.Equal(ZoneComplianceStatus.Compliant, compliance.CalculatedStatus);
        Assert.Equal(now.AddDays(2), compliance.ValidUntil);
        Assert.Equal(2, compliance.RequirementResults.Count);
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

    private static AccessControlDbContext CreateAccessControlDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<AccessControlDbContext> options = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase($"access-control-{Guid.NewGuid()}")
            .Options;
        return new AccessControlDbContext(options, tenantContext);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
