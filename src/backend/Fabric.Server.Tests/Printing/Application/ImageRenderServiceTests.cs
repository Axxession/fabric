using System.IO.Compression;
using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fabric.Server.Tests.Printing.Application;

public sealed class ImageRenderServiceTests
{
    private readonly MailMerge _mailMerge = new();
    private readonly FontProvider _fontProvider = new(NullLogger<FontProvider>.Instance);

    [Fact(DisplayName = "RenderAsync_WhenBmpProfileConfigured_ReturnsBmpDocument")]
    public async Task RenderAsync_WhenBmpProfileConfigured_ReturnsBmpDocument()
    {
        ImageRenderService service = CreateService(new RenderProfile
        {
            Target = RenderTarget.BmpImage,
            Dpi = 300,
            Background = "#FFFFFF"
        });

        RenderedDocument document = await service.RenderAsync(
            new Dictionary<string, string> { ["Name"] = "Sverre" },
            CreateTextTemplate("Hi, {{ Name }}"),
            CancellationToken.None);

        Assert.Equal("image/bmp", document.ContentType);
        Assert.Equal("rendered.bmp", document.FileName);
        Assert.True(document.Content.Length > 2);
        Assert.Equal((byte)'B', document.Content[0]);
        Assert.Equal((byte)'M', document.Content[1]);
    }

    [Fact(DisplayName = "RenderAsync_WhenMergedDataChanges_OutputChanges")]
    public async Task RenderAsync_WhenMergedDataChanges_OutputChanges()
    {
        ImageRenderService service = CreateService(new RenderProfile
        {
            Target = RenderTarget.BmpImage,
            Dpi = 300,
            Background = "#FFFFFF"
        });

        PrintTemplate template = CreateTextTemplate("Hi, {{ Name }}");

        RenderedDocument first = await service.RenderAsync(new Dictionary<string, string> { ["Name"] = "Ada" }, template, CancellationToken.None);
        RenderedDocument second = await service.RenderAsync(new Dictionary<string, string> { ["Name"] = "Grace" }, template, CancellationToken.None);

        Assert.NotEqual(first.Content, second.Content);
    }

    [Fact(DisplayName = "RenderManyAsync_WhenRowsProvided_ReturnsZipWithOneFilePerRow")]
    public async Task RenderManyAsync_WhenRowsProvided_ReturnsZipWithOneFilePerRow()
    {
        ImageRenderService service = CreateService(new RenderProfile
        {
            Target = RenderTarget.PngImage,
            Dpi = 300,
            Background = "#FFFFFF"
        });

        RenderedDocument document = await service.RenderManyAsync(
        [
            new Dictionary<string, string> { ["Name"] = "Ada" },
            new Dictionary<string, string> { ["Name"] = "Grace" }
        ],
        CreateTextTemplate("Hi, {{ Name }}"),
        CancellationToken.None);

        Assert.Equal("application/zip", document.ContentType);
        Assert.Equal("rendered-images.zip", document.FileName);

        using MemoryStream stream = new(document.Content);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
        Assert.Equal("render-001.png", archive.Entries[0].Name);
        Assert.Equal("render-002.png", archive.Entries[1].Name);
    }

    private ImageRenderService CreateService(RenderProfile profile) => new(
        NullLogger<ImageRenderService>.Instance,
        _mailMerge,
        _fontProvider,
        profile.Target switch
        {
            RenderTarget.BmpImage => ImageType.Bmp,
            RenderTarget.JpegImage => ImageType.Jpeg,
            _ => ImageType.Png
        },
        profile);

    private static PrintTemplate CreateTextTemplate(string text) => new()
    {
        Dpi = 300,
        Media = new RenderMedia(40, 20, Orientation.Landscape, "Badge"),
        Objects =
        [
            new TemplateObject
            {
                Type = "text",
                Text = text,
                Left = 10,
                Top = 10,
                Width = 120,
                Height = 20,
                FontSize = 14,
                Fill = "#000000"
            }
        ]
    };
}
