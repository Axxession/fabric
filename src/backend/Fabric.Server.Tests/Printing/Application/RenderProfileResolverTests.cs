using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Tests.Printing.Application;

public sealed class RenderProfileResolverTests
{
    private readonly RenderProfileResolver _resolver = new();

    [Fact(DisplayName = "Resolve_WhenOverrideExists_ReturnsOverride")]
    public void Resolve_WhenOverrideExists_ReturnsOverride()
    {
        RenderProfile overrideProfile = new()
        {
            Target = RenderTarget.PngImage,
            Dpi = 600,
            Background = "#000000"
        };

        RenderProfile designDefault = new()
        {
            Target = RenderTarget.BmpImage,
            Dpi = 300,
            Background = "#FFFFFF"
        };

        RenderProfile resolved = _resolver.Resolve(overrideProfile, designDefault);

        Assert.Equal(overrideProfile, resolved);
    }

    [Fact(DisplayName = "Resolve_WhenOnlyDesignDefaultExists_ReturnsDesignDefault")]
    public void Resolve_WhenOnlyDesignDefaultExists_ReturnsDesignDefault()
    {
        RenderProfile designDefault = new()
        {
            Target = RenderTarget.JpegImage,
            Dpi = 200,
            Quality = 85
        };

        RenderProfile resolved = _resolver.Resolve(null, designDefault);

        Assert.Equal(designDefault, resolved);
    }

    [Fact(DisplayName = "Resolve_WhenNoProfileExists_ReturnsFallback")]
    public void Resolve_WhenNoProfileExists_ReturnsFallback()
    {
        RenderProfile resolved = _resolver.Resolve(null, null);

        Assert.Equal(RenderProfileResolver.Fallback, resolved);
    }
}
