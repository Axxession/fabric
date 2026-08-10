using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Printing.Application;

public static class StandardMedia
{
    public static readonly RenderMedia CR79Portrait = new(84.0, 52.0, Orientation.Portrait, "CR79 Portrait");
    public static readonly RenderMedia CR79Landscape = new(84.0, 52.0, Orientation.Landscape, "CR79 Landscape");
    public static readonly RenderMedia CR80Portrait = new(85.6, 54.0, Orientation.Portrait, "CR80 Portrait");
    public static readonly RenderMedia CR80Landscape = new(85.6, 54.0, Orientation.Landscape, "CR80 Landscape");
    public static readonly RenderMedia VisitorLabel = new(50.0, 75.0, Orientation.Portrait, "Visitor Label");
    public static readonly RenderMedia SkinnyVisitorLabel = new(25.0, 37.5, Orientation.Portrait, "Skinny Visitor Label");

    public static IReadOnlyList<RenderMedia> All =>
    [
        CR79Portrait,
        CR79Landscape,
        CR80Portrait,
        CR80Landscape,
        VisitorLabel,
        SkinnyVisitorLabel
    ];
}
