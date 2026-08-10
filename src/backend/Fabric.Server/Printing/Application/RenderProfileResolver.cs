using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Printing.Application;

internal sealed class RenderProfileResolver
{
    internal static readonly RenderProfile Fallback = new()
    {
        Target = RenderTarget.BmpImage,
        Dpi = 300,
        Background = "#FFFFFF"
    };

    public RenderProfile Resolve(RenderProfile? overrideProfile, RenderProfile? designDefaultProfile) =>
        overrideProfile
        ?? designDefaultProfile
        ?? Fallback;
}
