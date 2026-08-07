using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.AccessControl.Application;

public sealed class AccessControlLocationResolverTests
{
    [Fact]
    public async Task ResolveSystemForLocationAsync_WhenBuildingLinkExists_PicksNearestLinkedAncestor()
    {
        TenantContext tenantContext = new();
        DbContextOptions<AccessControlDbContext> accessControlOptions = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase($"resolver-access-{Guid.NewGuid()}")
            .Options;
        DbContextOptions<LocationsDbContext> locationOptions = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase($"resolver-locations-{Guid.NewGuid()}")
            .Options;

        await using AccessControlDbContext accessControlDb = new(accessControlOptions, tenantContext);
        await using LocationsDbContext locationsDb = new(locationOptions, tenantContext);

        Guid siteId = Guid.NewGuid();
        Guid buildingId = Guid.NewGuid();
        Guid roomId = Guid.NewGuid();
        Guid siteSystemId = Guid.NewGuid();
        Guid buildingSystemId = Guid.NewGuid();

        locationsDb.LocationLookups.AddRange(
            LocationLookup.Site(siteId),
            LocationLookup.Building(siteId, buildingId),
            LocationLookup.Room(siteId, buildingId, roomId));

        accessControlDb.AccessControlSystemLocations.AddRange(
            AccessControlSystemLocation.Create(siteSystemId, siteId),
            AccessControlSystemLocation.Create(buildingSystemId, buildingId));

        await locationsDb.SaveChangesAsync();
        await accessControlDb.SaveChangesAsync();

        AccessControlLocationResolver resolver = new(accessControlDb, locationsDb);

        Result<ResolvedAccessControlSystem, AccessControlErrors> result = await resolver.ResolveSystemForLocationAsync(roomId);

        Assert.True(result.IsSuccess(out ResolvedAccessControlSystem? resolved));
        Assert.NotNull(resolved);
        Assert.Equal(buildingId, resolved.LocationId);
        Assert.Equal(buildingSystemId, resolved.AccessControlSystemId);
    }
}
