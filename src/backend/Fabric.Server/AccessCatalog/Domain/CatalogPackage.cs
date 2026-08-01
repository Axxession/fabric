namespace Fabric.Server.AccessCatalog.Domain;

public sealed class CatalogPackage
{
    private CatalogPackage() { }

    public Guid CatalogId { get; private set; }
    public Guid PackageId { get; private set; }
    public bool IsRequestable { get; private set; }

    public static CatalogPackage Create(Guid catalogId, Guid packageId, bool isRequestable) =>
        new()
        {
            CatalogId = catalogId,
            PackageId = packageId,
            IsRequestable = isRequestable
        };

    public void SetRequestable(bool isRequestable) => IsRequestable = isRequestable;
}
