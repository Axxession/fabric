using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Printing.Contracts;

public static class PrintingMapper
{
    public static PrintDesignSummaryResponse ToSummaryResponse(this PrintDesign design) => new(
        design.Id,
        design.Name,
        design.Version,
        design.Description,
        design.SurfaceKind,
        new RenderMediaResponse(design.MediaLabel, design.MediaWidth, design.MediaHeight, design.MediaOrientation),
        design.Dpi,
        design.CreatedAt,
        design.UpdatedAt);

    public static PrintDesignResponse ToResponse(this PrintDesign design) => new(
        design.Id,
        design.Name,
        design.Version,
        design.Description,
        design.SurfaceKind,
        design.DesignJson,
        new RenderMediaResponse(design.MediaLabel, design.MediaWidth, design.MediaHeight, design.MediaOrientation),
        design.Dpi,
        design.CreatedAt,
        design.UpdatedAt);

    public static RenderMediaResponse ToResponse(this RenderMedia media) => new(media.Label, media.Width, media.Height, media.Orientation);
}
