namespace Fabric.Server.AccessCatalog.Domain;

public sealed class Catalog
{
    private Catalog() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public CatalogStatus Status { get; private set; }

    public static Catalog Create(string name, string? description) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = NormalizeOptional(description),
            Status = CatalogStatus.Active
        };

    public void Update(string name, string? description, CatalogStatus status)
    {
        Name = name.Trim();
        Description = NormalizeOptional(description);
        Status = status;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
