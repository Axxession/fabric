using Fabric.Server.Locations.Persistence;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Application;

public sealed record ResolvedRequirementLocationPath(Guid LocationId, Guid[] CandidateLocationIds);

public sealed class RequirementsLocationResolver(
    RequirementsDbContext db,
    LocationsDbContext locationsDb)
{
    public async Task<ResolvedRequirementLocationPath?> ResolveLocationPathAsync(
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        LocationLookup? lookup = await locationsDb.LocationLookups
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == locationId, cancellationToken);

        if (lookup is null)
            return null;

        Guid[] candidateIds = lookup.Type switch
        {
            LocationType.Room when lookup.BuildingId.HasValue => [locationId, lookup.BuildingId.Value, lookup.SiteId],
            LocationType.Building => [locationId, lookup.SiteId],
            LocationType.Site => [locationId],
            _ => [locationId]
        };

        return new ResolvedRequirementLocationPath(locationId, candidateIds);
    }

    public async Task<Guid[]?> ResolveApplicableZoneIdsAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        ResolvedRequirementLocationPath? path = await ResolveLocationPathAsync(locationId, cancellationToken);
        if (path is null)
            return null;

        EnforcementZoneLocation[] links = await db.EnforcementZoneLocations
            .AsNoTracking()
            .Where(item => path.CandidateLocationIds.Contains(item.LocationId))
            .ToArrayAsync(cancellationToken);

        Guid[] zoneIds = path.CandidateLocationIds
            .Select(candidateId => links.Where(link => link.LocationId == candidateId).Select(link => link.EnforcementZoneId))
            .SelectMany(item => item)
            .Distinct()
            .ToArray();

        return zoneIds;
    }
}
