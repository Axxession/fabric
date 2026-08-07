using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.AccessControl.Application;

public sealed class PACSAssignmentServiceTests
{
    [Fact]
    public async Task CreateAssignmentsForGrantAsync_WhenRoomTargetExists_PicksMostSpecificTarget()
    {
        using TestDbScope scope = CreateScope();
        SeedLocationTree(scope, out Guid siteId, out Guid buildingId, out Guid roomId);
        SeedAccessGraph(scope, siteId, out AccessItem accessItem, out AccessControlSystem system);

        UnipassAccessLevelTarget siteTarget = UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, siteId, "Site Target", 100, 10, "Site Rule", "Site", ProvisioningTiming.Eager);
        UnipassAccessLevelTarget buildingTarget = UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, buildingId, "Building Target", 101, 10, "Building Rule", "Site", ProvisioningTiming.Eager);
        UnipassAccessLevelTarget roomTarget = UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, roomId, "Room Target", 102, 10, "Room Rule", "Site", ProvisioningTiming.Eager);
        scope.AccessControlDb.AccessLevelTargets.AddRange(siteTarget, buildingTarget, roomTarget);
        await scope.AccessControlDb.SaveChangesAsync();

        PACSAssignmentService service = CreateService(scope);

        Result<IReadOnlyList<PACSAssignment>, AccessControlErrors> result = await service.CreateAssignmentsForGrantAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            accessItem.Id,
            roomId,
            PACSAssignmentDurationKind.Permanent,
            DateTimeOffset.UtcNow,
            null);

        Assert.True(result.IsSuccess(out IReadOnlyList<PACSAssignment>? assignments));
        Assert.NotNull(assignments);
        Assert.Single(assignments);
        Assert.Equal(roomTarget.Id, assignments[0].AccessLevelTargetId);
    }

    [Fact]
    public async Task CreateAssignmentsForGrantAsync_WhenBuildingTargetExists_PicksBuildingFallback()
    {
        using TestDbScope scope = CreateScope();
        SeedLocationTree(scope, out Guid siteId, out Guid buildingId, out Guid roomId);
        SeedAccessGraph(scope, siteId, out AccessItem accessItem, out AccessControlSystem system);

        UnipassAccessLevelTarget siteTarget = UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, siteId, "Site Target", 100, 10, "Site Rule", "Site", ProvisioningTiming.Eager);
        UnipassAccessLevelTarget buildingTarget = UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, buildingId, "Building Target", 101, 10, "Building Rule", "Site", ProvisioningTiming.Eager);
        scope.AccessControlDb.AccessLevelTargets.AddRange(siteTarget, buildingTarget);
        await scope.AccessControlDb.SaveChangesAsync();

        PACSAssignmentService service = CreateService(scope);

        Result<IReadOnlyList<PACSAssignment>, AccessControlErrors> result = await service.CreateAssignmentsForGrantAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            accessItem.Id,
            roomId,
            PACSAssignmentDurationKind.Permanent,
            DateTimeOffset.UtcNow,
            null);

        Assert.True(result.IsSuccess(out IReadOnlyList<PACSAssignment>? assignments));
        Assert.NotNull(assignments);
        Assert.Single(assignments);
        Assert.Equal(buildingTarget.Id, assignments[0].AccessLevelTargetId);
    }

    [Fact]
    public async Task CreateAssignmentsForGrantAsync_WhenOnlyGlobalTargetExists_PicksGlobalFallback()
    {
        using TestDbScope scope = CreateScope();
        SeedLocationTree(scope, out Guid siteId, out _, out Guid roomId);
        SeedAccessGraph(scope, siteId, out AccessItem accessItem, out AccessControlSystem system);

        UnipassAccessLevelTarget globalTarget = UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, null, "Global Target", 100, 10, "Global Rule", "Site", ProvisioningTiming.Eager);
        scope.AccessControlDb.AccessLevelTargets.Add(globalTarget);
        await scope.AccessControlDb.SaveChangesAsync();

        PACSAssignmentService service = CreateService(scope);

        Result<IReadOnlyList<PACSAssignment>, AccessControlErrors> result = await service.CreateAssignmentsForGrantAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            accessItem.Id,
            roomId,
            PACSAssignmentDurationKind.Permanent,
            DateTimeOffset.UtcNow,
            null);

        Assert.True(result.IsSuccess(out IReadOnlyList<PACSAssignment>? assignments));
        Assert.NotNull(assignments);
        Assert.Single(assignments);
        Assert.Equal(globalTarget.Id, assignments[0].AccessLevelTargetId);
    }

    [Fact]
    public async Task CreateAssignmentsForGrantAsync_WhenTwoTargetsMatchSameScope_ReturnsBothAssignments()
    {
        using TestDbScope scope = CreateScope();
        SeedLocationTree(scope, out Guid siteId, out Guid buildingId, out Guid roomId);
        SeedAccessGraph(scope, siteId, out AccessItem accessItem, out AccessControlSystem system);

        scope.AccessControlDb.AccessLevelTargets.AddRange(
            UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, buildingId, "Building Target A", 100, 10, "Rule A", "Site", ProvisioningTiming.Eager),
            UnipassAccessLevelTarget.Create(accessItem.Id, system.Id, buildingId, "Building Target B", 101, 10, "Rule B", "Site", ProvisioningTiming.Eager));
        await scope.AccessControlDb.SaveChangesAsync();

        PACSAssignmentService service = CreateService(scope);

        Result<IReadOnlyList<PACSAssignment>, AccessControlErrors> result = await service.CreateAssignmentsForGrantAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            accessItem.Id,
            roomId,
            PACSAssignmentDurationKind.Permanent,
            DateTimeOffset.UtcNow,
            null);

        Assert.True(result.IsSuccess(out IReadOnlyList<PACSAssignment>? assignments));
        Assert.NotNull(assignments);
        Assert.Equal(2, assignments.Count);
    }

    private static TestDbScope CreateScope()
    {
        TenantContext tenantContext = new();
        DbContextOptions<AccessControlDbContext> accessControlOptions = new DbContextOptionsBuilder<AccessControlDbContext>()
            .UseInMemoryDatabase($"access-control-{Guid.NewGuid()}")
            .Options;
        DbContextOptions<LocationsDbContext> locationOptions = new DbContextOptionsBuilder<LocationsDbContext>()
            .UseInMemoryDatabase($"locations-{Guid.NewGuid()}")
            .Options;

        return new TestDbScope(
            tenantContext,
            new AccessControlDbContext(accessControlOptions, tenantContext),
            new LocationsDbContext(locationOptions, tenantContext));
    }

    private static PACSAssignmentService CreateService(TestDbScope scope)
    {
        PACSProvisioningReconciliationService reconciliationService = new(
            scope.AccessControlDb,
            scope.TenantContext,
            new PACSProvisioningReconciliationTrigger(),
            null!,
            null!,
            null!,
            TimeProvider.System);

        return new PACSAssignmentService(
            scope.AccessControlDb,
            new AccessControlLocationResolver(scope.AccessControlDb, scope.LocationsDb),
            reconciliationService,
            TimeProvider.System);
    }

    private static void SeedLocationTree(TestDbScope scope, out Guid siteId, out Guid buildingId, out Guid roomId)
    {
        siteId = Guid.NewGuid();
        buildingId = Guid.NewGuid();
        roomId = Guid.NewGuid();

        scope.LocationsDb.LocationLookups.AddRange(
            LocationLookup.Site(siteId),
            LocationLookup.Building(siteId, buildingId),
            LocationLookup.Room(siteId, buildingId, roomId));
        scope.LocationsDb.SaveChanges();
    }

    private static void SeedAccessGraph(TestDbScope scope, Guid linkedLocationId, out AccessItem accessItem, out AccessControlSystem system)
    {
        accessItem = AccessItem.Create("IT Access", null);
        Result<UnipassSystemConfig, AccessControlErrors> configResult = UnipassSystemConfig.Create("https://unipass.local", false, "user", "pass");
        Assert.True(configResult.IsSuccess(out UnipassSystemConfig? config));
        Assert.NotNull(config);

        Result<AccessControlSystem, AccessControlErrors> systemResult = AccessControlSystem.CreateUnipass("Main PACS", config, AnomalyBlockMode.WarnOnly);
        Assert.True(systemResult.IsSuccess(out system));

        scope.AccessControlDb.AccessItems.Add(accessItem);
        scope.AccessControlDb.AccessControlSystems.Add(system);
        scope.AccessControlDb.AccessControlSystemLocations.Add(AccessControlSystemLocation.Create(system.Id, linkedLocationId));
        scope.AccessControlDb.SaveChanges();
    }

    private sealed record TestDbScope(
        TenantContext TenantContext,
        AccessControlDbContext AccessControlDb,
        LocationsDbContext LocationsDb) : IDisposable
    {
        public void Dispose()
        {
            AccessControlDb.Dispose();
            LocationsDb.Dispose();
        }
    }
}
