using Fabric.Server.Infrastructure.Storage;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace Fabric.Server.Kiosk.Application;

public interface IKioskAssetStorage
{
    Task<string> SaveAsync(Guid profileId, Guid assetId, string fileName, string? contentType, Stream stream, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}

public sealed class KioskAssetStorage(IStorage storage, IStoragePathBuilder pathBuilder) : IKioskAssetStorage
{
    public async Task<string> SaveAsync(Guid profileId, Guid assetId, string fileName, string? contentType, Stream stream, CancellationToken cancellationToken)
    {
        string relativePath = pathBuilder.BuildTenantScopedPath("kiosk", "profiles", profileId.ToString("N"), "assets", assetId.ToString("N"));
        UploadOptions options = new(fileName: assetId.ToString("N"), directory: Path.GetDirectoryName(relativePath)?.Replace('\\', '/'), mimeType: ResolveContentType(contentType));
        Result<BlobMetadata> result = await storage.UploadAsync(stream, options, cancellationToken);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Could not store kiosk asset '{relativePath}'.");

        return relativePath;
    }

    public async Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        Result<LocalFile> result = await storage.DownloadAsync(relativePath, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return null;

        return result.Value.OpenReadStream();
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        await storage.DeleteAsync(relativePath, cancellationToken);
    }

    private static string ResolveContentType(string? contentType) => string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
}
