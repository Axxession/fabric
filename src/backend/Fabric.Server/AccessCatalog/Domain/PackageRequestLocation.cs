namespace Fabric.Server.AccessCatalog.Domain;

public sealed class PackageRequestLocation
{
    private PackageRequestLocation() { }

    public Guid RequestId { get; private set; }
    public Guid LocationId { get; private set; }

    public static PackageRequestLocation Create(Guid requestId, Guid locationId) =>
        new()
        {
            RequestId = requestId,
            LocationId = locationId
        };
}
