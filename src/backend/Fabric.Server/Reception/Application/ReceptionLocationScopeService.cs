using Fabric.Server.Locations.Domain;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Application;

public sealed class ReceptionLocationScopeService(LocationsDbContext locationsDb)
{
    public async Task<HashSet<Guid>?> GetScopedLocationIds(Guid ancestorLocationId, CancellationToken cancellationToken = default)
    {
        LocationLookup? ancestor = await locationsDb.LocationLookups
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == ancestorLocationId, cancellationToken);

        if (ancestor is null)
            return null;

        IQueryable<LocationLookup> query = locationsDb.LocationLookups
            .AsNoTracking()
            .Where(x => x.SiteId == ancestor.SiteId);

        query = ancestor.Type switch
        {
            LocationType.Site => query,
            LocationType.Building when ancestor.BuildingId.HasValue => query.Where(x => x.BuildingId == ancestor.BuildingId),
            LocationType.Room when ancestor.RoomId.HasValue => query.Where(x => x.RoomId == ancestor.RoomId),
            _ => query.Where(_ => false)
        };

        Guid[] locationIds = await query
            .Select(x => x.Id)
            .ToArrayAsync(cancellationToken);

        return locationIds.Length == 0 ? null : [.. locationIds];
    }
}
