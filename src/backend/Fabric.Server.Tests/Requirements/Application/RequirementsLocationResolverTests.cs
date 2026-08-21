using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Contractors.Persistence;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.Requirements.Application;

public sealed class RequirementsLocationResolverTests
{
    [Fact]
    public async Task DeriveForGrantAsync_WhenNestedPoliciesExist_IncludesAncestorPolicies()
    {
        TenantContext tenantContext = new();
        DbContextOptions<RequirementsDbContext> requirementsOptions = new DbContextOptionsBuilder<RequirementsDbContext>()
            .UseInMemoryDatabase($"requirements-resolver-{Guid.NewGuid()}")
            .Options;
        DbContextOptions<LocationsDbContext> locationsOptions = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase($"requirements-locations-{Guid.NewGuid()}")
            .Options;

        await using RequirementsDbContext requirementsDb = new(requirementsOptions, tenantContext);
        await using LocationsDbContext locationsDb = new(locationsOptions, tenantContext);

        Guid siteId = Guid.NewGuid();
        Guid buildingId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();

        locationsDb.LocationLookups.AddRange(
            LocationLookup.Site(siteId),
            LocationLookup.Building(siteId, buildingId),
            LocationLookup.Room(siteId, buildingId, roomId));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Result<RequirementDefinition, RequirementDefinitionErrors> siteRequirementCreate = RequirementDefinition.Create("PERIM", "Perimeter", null, [RequirementEvidenceKind.Document], false, now);
        Result<RequirementDefinition, RequirementDefinitionErrors> roomRequirementCreate = RequirementDefinition.Create("ROOM", "Server Room", null, [RequirementEvidenceKind.Document], false, now);
        siteRequirementCreate.IsSuccess(out RequirementDefinition siteRequirement);
        roomRequirementCreate.IsSuccess(out RequirementDefinition roomRequirement);

        requirementsDb.RequirementDefinitions.AddRange(siteRequirement, roomRequirement);
        requirementsDb.LocationRequirementPolicies.AddRange(
            LocationRequirementPolicy.Create(siteId, siteRequirement.Id, RequirementSubjectKind.Visitor, true, now),
            LocationRequirementPolicy.Create(roomId, roomRequirement.Id, RequirementSubjectKind.Visitor, true, now));

        await locationsDb.SaveChangesAsync();
        await requirementsDb.SaveChangesAsync();

        GrantRequirementsService service = new(requirementsDb, locationsDb, CreateContractorsDbContext(), CreateIdentitiesDbContext(), TimeProvider.System);

        Result<IReadOnlyList<DerivedGrantRequirement>, RequirementsEvaluationErrors> result = await service.DeriveForGrantAsync(Guid.NewGuid(), RequirementSubjectKind.Visitor, roomId);

        Assert.True(result.IsSuccess(out IReadOnlyList<DerivedGrantRequirement> requirements));
        Assert.Equal(2, requirements.Count);
        Assert.Contains(requirements, item => item.RequirementDefinitionId == siteRequirement.Id);
        Assert.Contains(requirements, item => item.RequirementDefinitionId == roomRequirement.Id);
    }

    private static ContractorsDbContext CreateContractorsDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<ContractorsDbContext> options = new DbContextOptionsBuilder<ContractorsDbContext>()
            .UseInMemoryDatabase($"requirements-contractors-{Guid.NewGuid()}")
            .Options;
        return new ContractorsDbContext(options, tenantContext);
    }

    private static IdentitiesDbContext CreateIdentitiesDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<IdentitiesDbContext> options = new DbContextOptionsBuilder<IdentitiesDbContext>()
            .UseInMemoryDatabase($"requirements-identities-{Guid.NewGuid()}")
            .Options;
        return new IdentitiesDbContext(options, tenantContext);
    }
}
