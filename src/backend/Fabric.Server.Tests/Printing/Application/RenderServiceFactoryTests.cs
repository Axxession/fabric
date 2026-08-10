using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Fabric.Server.Tests.Printing.Application;

public sealed class RenderServiceFactoryTests
{
    [Fact(DisplayName = "Create_WhenBmpProfileRequested_ReturnsBmpRenderer")]
    public async Task Create_WhenBmpProfileRequested_ReturnsBmpRenderer()
    {
        using ServiceProvider services = BuildServices();
        RenderServiceFactory factory = new(services);

        IRenderService renderer = factory.Create(new RenderProfile
        {
            Target = RenderTarget.BmpImage,
            Dpi = 300,
            Background = "#FFFFFF"
        });

        RenderedDocument document = await renderer.RenderAsync(
            new Dictionary<string, string>(),
            new PrintTemplate
            {
                Dpi = 300,
                Media = new RenderMedia(10, 10, Orientation.Landscape, "Badge"),
                Objects = []
            },
            CancellationToken.None);

        Assert.Equal("image/bmp", document.ContentType);
        Assert.Equal("rendered.bmp", document.FileName);
    }

    [Fact(DisplayName = "Create_WhenPngProfileRequested_ReturnsPngRenderer")]
    public async Task Create_WhenPngProfileRequested_ReturnsPngRenderer()
    {
        using ServiceProvider services = BuildServices();
        RenderServiceFactory factory = new(services);

        IRenderService renderer = factory.Create(new RenderProfile
        {
            Target = RenderTarget.PngImage,
            Dpi = 300,
            Background = "#FFFFFF"
        });

        RenderedDocument document = await renderer.RenderAsync(
            new Dictionary<string, string>(),
            new PrintTemplate
            {
                Dpi = 300,
                Media = new RenderMedia(10, 10, Orientation.Landscape, "Badge"),
                Objects = []
            },
            CancellationToken.None);

        Assert.Equal("image/png", document.ContentType);
        Assert.Equal("rendered.png", document.FileName);
    }

    private static ServiceProvider BuildServices()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<FontProvider>();
        services.AddTransient<MailMerge>();
        return services.BuildServiceProvider();
    }
}
