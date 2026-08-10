using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Printing.Application;

internal sealed class RenderServiceFactory(IServiceProvider services)
{
    public IRenderService Create(RenderProfile profile)
    {
        return profile.Target switch
        {
            RenderTarget.BmpImage => CreateImageRenderer(ImageType.Bmp, profile),
            RenderTarget.PngImage => CreateImageRenderer(ImageType.Png, profile),
            RenderTarget.JpegImage => CreateImageRenderer(ImageType.Jpeg, profile),
            _ => throw new InvalidOperationException($"Unsupported render target '{profile.Target}'.")
        };
    }

    private IRenderService CreateImageRenderer(ImageType imageType, RenderProfile profile) =>
        ActivatorUtilities.CreateInstance<ImageRenderService>(services, imageType, profile);
}
