namespace Fabric.Server.AccessCatalog.Domain;

public sealed class AccessGrantLocation
{
    private AccessGrantLocation() { }

    public Guid AccessGrantId { get; private set; }
    public Guid LocationId { get; private set; }

    public static AccessGrantLocation Create(Guid accessGrantId, Guid locationId) =>
        new()
        {
            AccessGrantId = accessGrantId,
            LocationId = locationId
        };
}
