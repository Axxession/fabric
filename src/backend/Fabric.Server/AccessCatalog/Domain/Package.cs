namespace Fabric.Server.AccessCatalog.Domain;

public sealed class Package
{
    private Package() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public PackageStatus Status { get; private set; }

    public static Package Create(string name, string? description) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = NormalizeOptional(description),
            Status = PackageStatus.Active
        };

    public void Update(string name, string? description, PackageStatus status)
    {
        Name = name.Trim();
        Description = NormalizeOptional(description);
        Status = status;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
