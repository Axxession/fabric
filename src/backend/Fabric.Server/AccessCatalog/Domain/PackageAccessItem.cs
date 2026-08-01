namespace Fabric.Server.AccessCatalog.Domain;

public sealed class PackageAccessItem
{
    private PackageAccessItem() { }

    public Guid PackageId { get; private set; }
    public Guid AccessItemId { get; private set; }

    public static PackageAccessItem Create(Guid packageId, Guid accessItemId) =>
        new()
        {
            PackageId = packageId,
            AccessItemId = accessItemId
        };
}
