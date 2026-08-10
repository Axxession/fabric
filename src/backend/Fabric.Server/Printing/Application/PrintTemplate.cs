using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Printing.Application;

public sealed record PrintTemplate
{
    public int Version { get; init; }
    public RenderMedia Media { get; init; } = default!;
    public int Dpi { get; init; }
    public List<TemplateObject> Objects { get; init; } = [];
}

public sealed record RenderMedia
{
    public RenderMedia()
    {
        Label = string.Empty;
    }

    public RenderMedia(double width, double height, Orientation orientation, string label = "")
    {
        Width = width;
        Height = height;
        Orientation = orientation;
        Label = label;
    }

    public string Label { get; init; } = string.Empty;
    public double Width { get; init; }
    public double Height { get; init; }
    public Orientation Orientation { get; init; }
}

public sealed record TemplateObject
{
    public string? Type { get; init; }
    public string? Src { get; init; }
    public string? Text { get; init; }
    public string? Field { get; init; }
    public bool Placeholder { get; init; }
    public string? Fill { get; init; }
    public float Left { get; init; }
    public float Top { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public float ScaleX { get; init; } = 1;
    public float ScaleY { get; init; } = 1;
    public float FontSize { get; init; } = 12;
    public string? FontStyle { get; init; }
    public string? FontWeight { get; init; }
    public bool Underline { get; init; }
    public string? FontFamily { get; init; }
    public float Angle { get; init; }
    public string? FixedImageSrc { get; init; }
    public string? DataField { get; init; }
    public string? ResolvedSrc { get; init; }
}

public sealed record RenderedDocument(string ContentType, string FileName, byte[] Content);
