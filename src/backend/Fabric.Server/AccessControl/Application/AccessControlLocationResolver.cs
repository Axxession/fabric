using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Locations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed record ResolvedAccessControlSystem(Guid LocationId, Guid AccessControlSystemId);

public sealed class AccessControlLocationResolver(
    AccessControlDbContext db,
    LocationsDbContext locationsDb)
{
    public async Task<Result<ResolvedAccessControlSystem, AccessControlErrors>> ResolveSystemForLocationAsync(
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        LocationLookup? lookup = await locationsDb.LocationLookups
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == locationId, cancellationToken);

        if (lookup is null)
            return Result.Failure<ResolvedAccessControlSystem, AccessControlErrors>(AccessControlErrors.LocationNotFound);

        Guid[] candidateIds = lookup.Type switch
        {
            LocationType.Room when lookup.BuildingId.HasValue => [locationId, lookup.BuildingId.Value, lookup.SiteId],
            LocationType.Building => [locationId, lookup.SiteId],
            LocationType.Site => [locationId],
            _ => [locationId]
        };

        AccessControlSystemLocation[] links = await db.AccessControlSystemLocations
            .AsNoTracking()
            .Where(link => candidateIds.Contains(link.LocationId))
            .ToArrayAsync(cancellationToken);

        AccessControlSystemLocation? match = candidateIds
            .Select(candidateId => links.SingleOrDefault(link => link.LocationId == candidateId))
            .FirstOrDefault(link => link is not null);

        return match is null
            ? Result.Failure<ResolvedAccessControlSystem, AccessControlErrors>(AccessControlErrors.SystemLocationNotFound)
            : Result.Success<ResolvedAccessControlSystem, AccessControlErrors>(new ResolvedAccessControlSystem(match.LocationId, match.AccessControlSystemId));
    }
}
