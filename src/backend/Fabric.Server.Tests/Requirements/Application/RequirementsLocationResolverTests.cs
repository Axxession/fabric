using Fabric.Server.Infrastructure.Tenancy;
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
    public async Task ResolveApplicableZoneIdsAsync_WhenNestedMappingsExist_ReturnsAncestorOrder()
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
        Result<EnforcementZone, EnforcementZoneErrors> perimeterCreate = EnforcementZone.Create("PERIM", "Perimeter", null, now);
        Result<EnforcementZone, EnforcementZoneErrors> roomCreate = EnforcementZone.Create("ROOM", "Server Room", null, now);
        perimeterCreate.IsSuccess(out EnforcementZone perimeterZone);
        roomCreate.IsSuccess(out EnforcementZone roomZone);

        requirementsDb.EnforcementZones.AddRange(perimeterZone, roomZone);
        requirementsDb.EnforcementZoneLocations.AddRange(
            EnforcementZoneLocation.Create(perimeterZone.Id, siteId, now),
            EnforcementZoneLocation.Create(roomZone.Id, roomId, now));

        await locationsDb.SaveChangesAsync();
        await requirementsDb.SaveChangesAsync();

        RequirementsLocationResolver resolver = new(requirementsDb, locationsDb);

        Guid[]? zoneIds = await resolver.ResolveApplicableZoneIdsAsync(roomId);

        Assert.NotNull(zoneIds);
        Assert.Equal(2, zoneIds!.Length);
        Assert.Contains(perimeterZone.Id, zoneIds);
        Assert.Contains(roomZone.Id, zoneIds);
    }
}
