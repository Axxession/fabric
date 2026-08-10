namespace Fabric.Server.Printing.Application;

public interface IRenderService
{
    Task<RenderedDocument> RenderAsync(
        IReadOnlyDictionary<string, string> data,
        PrintTemplate template,
        CancellationToken cancellationToken);

    Task<RenderedDocument> RenderManyAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        PrintTemplate template,
        CancellationToken cancellationToken);
}
