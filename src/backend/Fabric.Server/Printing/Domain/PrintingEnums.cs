namespace Fabric.Server.Printing.Domain;

public enum PrintSurfaceKind
{
    Card,
    Label
}

public enum Orientation
{
    Portrait,
    Landscape
}

public enum RenderTarget
{
    BmpImage,
    PngImage,
    JpegImage
}
