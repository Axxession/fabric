using Fabric.Server.Core;
using Fabric.Server.Locations.Domain;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Locations.Application;

public class LocationService(LocationsDbContext db)
{
    private async Task<Site?> GetSiteAggregate(Guid siteId, CancellationToken cancellationToken = default)
    {
        return await db.Sites
            .Include(site => site.Buildings)
            .ThenInclude(building => building.Rooms)
            .SingleOrDefaultAsync(site => site.Id == siteId, cancellationToken);
    }

    public async Task<LocationLookup?> FindLookup(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await db.LocationLookups
            .AsNoTracking()
            .SingleOrDefaultAsync(lookup => lookup.Id == locationId, cancellationToken);
    }

    public async Task<bool> IsPartOfLocationTree(Guid locationId, Guid ancestorLocationId, CancellationToken cancellationToken = default)
    {
        if (locationId == ancestorLocationId)
            return await db.LocationLookups.AnyAsync(lookup => lookup.Id == locationId, cancellationToken);

        LocationLookup? location = await FindLookup(locationId, cancellationToken);
        LocationLookup? ancestor = await FindLookup(ancestorLocationId, cancellationToken);

        if (location is null || ancestor is null || location.SiteId != ancestor.SiteId)
            return false;

        return ancestor.Type switch
        {
            LocationType.Site => true,
            LocationType.Building => location.BuildingId == ancestor.BuildingId,
            LocationType.Room => location.RoomId == ancestor.RoomId,
            _ => false
        };
    }

    public async Task<Location?> GetLocationById(Guid locationId, CancellationToken cancellationToken = default)
    {
        LocationLookup? lookup = await FindLookup(locationId, cancellationToken);
        if (lookup is null)
            return null;

        Site? site = await GetSiteAggregate(lookup.SiteId, cancellationToken);
        if (site is null)
            return null;

        return lookup.Type switch
        {
            LocationType.Site => new Location.SiteLocation(site),
            LocationType.Building => GetBuildingLocation(site, lookup.BuildingId),
            LocationType.Room => GetRoomLocation(site, lookup.BuildingId, lookup.RoomId),
            _ => null
        };
    }

    public async Task<IReadOnlyList<Location>> GetLocationsByIds(Guid[] locationIds, CancellationToken cancellationToken = default)
    {
        if (locationIds.Length == 0)
            return [];

        LocationLookup[] lookups = await db.LocationLookups
            .AsNoTracking()
            .Where(lookup => locationIds.Contains(lookup.Id))
            .ToArrayAsync(cancellationToken);

        if (lookups.Length == 0)
            return [];

        Dictionary<Guid, Site> sitesById = await db.Sites
            .AsNoTracking()
            .Include(site => site.Buildings)
            .ThenInclude(building => building.Rooms)
            .Where(site => lookups.Select(lookup => lookup.SiteId).Contains(site.Id))
            .ToDictionaryAsync(site => site.Id, cancellationToken);

        return lookups
            .Select(lookup =>
            {
                if (!sitesById.TryGetValue(lookup.SiteId, out Site? site))
                    return null;

                return lookup.Type switch
                {
                    LocationType.Site => new Location.SiteLocation(site),
                    LocationType.Building => GetBuildingLocation(site, lookup.BuildingId),
                    LocationType.Room => GetRoomLocation(site, lookup.BuildingId, lookup.RoomId),
                    _ => null
                };
            })
            .Where(location => location is not null)
            .Select(location => location!)
            .OrderBy(location => Array.IndexOf(locationIds, location.Id))
            .ToArray();
    }

    public async Task<Result<Location, LocationErrors>> CreateSite(
        Guid id,
        string name,
        string? address,
        CancellationToken cancellationToken = default)
    {
        var site = Site.Create(id, name, address);

        db.Sites.Add(site);
        db.LocationLookups.Add(LocationLookup.Site(site.Id));
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success<Location, LocationErrors>(new Location.SiteLocation(site));
    }

    public async Task<Result<Location, LocationErrors>> AddBuilding(
        Guid siteId,
        string name,
        string? address,
        CancellationToken cancellationToken = default)
    {
        Site? site = await GetSiteAggregate(siteId, cancellationToken);
        if (site is null)
            return Result.Failure<Location, LocationErrors>(LocationErrors.SiteNotFound);

        Result<Location, LocationErrors> result = site.AddBuilding(name, address);
        if (result.IsSuccess(out Location location) && location is Location.BuildingLocation buildingLocation)
        {
            db.LocationLookups.Add(LocationLookup.Building(site.Id, buildingLocation.Building.Id));
            await db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<Result<Location, LocationErrors>> UpdateSite(
        Guid siteId,
        string name,
        CancellationToken cancellationToken = default)
    {
        Site? site = await GetSiteAggregate(siteId, cancellationToken);
        if (site is null)
            return Result.Failure<Location, LocationErrors>(LocationErrors.SiteNotFound);

        Location location = site.Rename(name);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success<Location, LocationErrors>(location);
    }

    public async Task<Result<Location, LocationErrors>> UpdateBuilding(
        Guid siteId,
        Guid buildingId,
        string name,
        CancellationToken cancellationToken = default)
    {
        Site? site = await GetSiteAggregate(siteId, cancellationToken);
        if (site is null)
            return Result.Failure<Location, LocationErrors>(LocationErrors.SiteNotFound);

        Result<Location, LocationErrors> result = site.RenameBuilding(buildingId, name);
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<Location, LocationErrors>> RemoveBuilding(
        Guid siteId,
        Guid buildingId,
        CancellationToken cancellationToken = default)
    {
        Site? site = await GetSiteAggregate(siteId, cancellationToken);
        if (site is null)
            return Result.Failure<Location, LocationErrors>(LocationErrors.SiteNotFound);

        Result<Location, LocationErrors> result = site.RemoveBuilding(buildingId);
        if (result.IsSuccess(out _))
        {
            await db.LocationLookups
                .Where(lookup => lookup.BuildingId == buildingId)
                .ExecuteDeleteAsync(cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<Result<Location, LocationErrors>> AddRoom(
        Guid siteId,
        Guid buildingId,
        string name,
        int capacity,
        bool wheelchairAccessible,
        CancellationToken cancellationToken = default)
    {
        Site? site = await GetSiteAggregate(siteId, cancellationToken);
        if (site is null)
            return Result.Failure<Location, LocationErrors>(LocationErrors.SiteNotFound);

        Result<Location, LocationErrors> result = site.AddRoom(buildingId, name, capacity, wheelchairAccessible);
        if (result.IsSuccess(out Location location) && location is Location.RoomLocation roomLocation)
        {
            db.LocationLookups.Add(LocationLookup.Room(site.Id, buildingId, roomLocation.Room.Id));
            await db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<Result<Location, LocationErrors>> UpdateRoom(
        Guid siteId,
        Guid buildingId,
        Guid roomId,
        string name,
        int capacity,
        bool wheelchairAccessible,
        CancellationToken cancellationToken = default)
    {
        Site? site = await GetSiteAggregate(siteId, cancellationToken);
        if (site is null)
            return Result.Failure<Location, LocationErrors>(LocationErrors.SiteNotFound);

        Result<Location, LocationErrors> result = site.UpdateRoom(buildingId, roomId, name, capacity, wheelchairAccessible);
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<Location, LocationErrors>> RemoveRoom(
        Guid siteId,
        Guid buildingId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        Site? site = await GetSiteAggregate(siteId, cancellationToken);
        if (site is null)
            return Result.Failure<Location, LocationErrors>(LocationErrors.SiteNotFound);

        Result<Location, LocationErrors> result = site.RemoveRoom(buildingId, roomId);
        if (result.IsSuccess(out _))
        {
            await db.LocationLookups
                .Where(lookup => lookup.Id == roomId)
                .ExecuteDeleteAsync(cancellationToken);

            await db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static Location? GetBuildingLocation(Site site, Guid? buildingId)
    {
        if (!buildingId.HasValue)
            return null;

        Building? building = site.Buildings.SingleOrDefault(x => x.Id == buildingId.Value);
        return building is null ? null : new Location.BuildingLocation(site, building);
    }

    private static Location? GetRoomLocation(Site site, Guid? buildingId, Guid? roomId)
    {
        if (!buildingId.HasValue || !roomId.HasValue)
            return null;

        Building? building = site.Buildings.SingleOrDefault(x => x.Id == buildingId.Value);
        Room? room = building?.Rooms.SingleOrDefault(x => x.Id == roomId.Value);

        return building is null || room is null ? null : new Location.RoomLocation(site, building, room);
    }
}
