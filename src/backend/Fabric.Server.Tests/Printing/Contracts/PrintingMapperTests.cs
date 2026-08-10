using Fabric.Server.Printing.Contracts;
using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Tests.Printing.Contracts;

public sealed class PrintingMapperTests
{
    [Fact(DisplayName = "ToResponse_WhenDesignHasDefaultRenderProfile_MapsIt")]
    public void ToResponse_WhenDesignHasDefaultRenderProfile_MapsIt()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PrintDesign design = PrintDesign.Create(
            "Badge",
            1,
            null,
            PrintSurfaceKind.Card,
            "{}",
            "CR80",
            85.6,
            54,
            Orientation.Landscape,
            300,
            new RenderProfile
            {
                Target = RenderTarget.BmpImage,
                Dpi = 300,
                Background = "#FFFFFF"
            },
            now);

        PrintDesignResponse response = design.ToResponse();

        Assert.NotNull(response.DefaultRenderProfile);
        Assert.Equal(RenderTarget.BmpImage, response.DefaultRenderProfile!.Target);
        Assert.Equal(300, response.DefaultRenderProfile.Dpi);
        Assert.Equal("#FFFFFF", response.DefaultRenderProfile.Background);
    }
}
