namespace Fabric.Server.Printing.Domain;

public sealed class PrintDesign
{
    private PrintDesign() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public int Version { get; private set; }
    public string? Description { get; private set; }
    public PrintSurfaceKind SurfaceKind { get; private set; }
    public string DesignJson { get; private set; } = default!;
    public string MediaLabel { get; private set; } = default!;
    public double MediaWidth { get; private set; }
    public double MediaHeight { get; private set; }
    public Orientation MediaOrientation { get; private set; }
    public int Dpi { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PrintDesign Create(
        string name,
        int version,
        string? description,
        PrintSurfaceKind surfaceKind,
        string designJson,
        string mediaLabel,
        double mediaWidth,
        double mediaHeight,
        Orientation mediaOrientation,
        int dpi,
        DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        Version = version,
        Description = NormalizeOptional(description),
        SurfaceKind = surfaceKind,
        DesignJson = designJson,
        MediaLabel = mediaLabel,
        MediaWidth = mediaWidth,
        MediaHeight = mediaHeight,
        MediaOrientation = mediaOrientation,
        Dpi = dpi,
        CreatedAt = now,
        UpdatedAt = now
    };

    public void Update(
        string name,
        int version,
        string? description,
        PrintSurfaceKind surfaceKind,
        string designJson,
        string mediaLabel,
        double mediaWidth,
        double mediaHeight,
        Orientation mediaOrientation,
        int dpi,
        DateTimeOffset now)
    {
        Name = name.Trim();
        Version = version;
        Description = NormalizeOptional(description);
        SurfaceKind = surfaceKind;
        DesignJson = designJson;
        MediaLabel = mediaLabel;
        MediaWidth = mediaWidth;
        MediaHeight = mediaHeight;
        MediaOrientation = mediaOrientation;
        Dpi = dpi;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
