using Fabric.Server.Infrastructure.Storage;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace Fabric.Server.Reception.Application;

public interface IReceptionKioskSessionStorage
{
    Task<string> SaveAsync(Guid sessionId, string name, byte[] content, CancellationToken cancellationToken);
    Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}

public sealed class ReceptionKioskSessionStorage(IStorage storage, IStoragePathBuilder pathBuilder) : IReceptionKioskSessionStorage
{
    public async Task<string> SaveAsync(Guid sessionId, string name, byte[] content, CancellationToken cancellationToken)
    {
        string normalizedName = name.Trim().ToLowerInvariant();
        string relativePath = pathBuilder.BuildTenantScopedPath("reception", "kiosk-sessions", sessionId.ToString("N"), normalizedName);
        await using MemoryStream stream = new(content, writable: false);
        UploadOptions options = new(fileName: normalizedName, directory: Path.GetDirectoryName(relativePath)?.Replace('\\', '/'), mimeType: "image/jpeg");
        Result<BlobMetadata> result = await storage.UploadAsync(stream, options, cancellationToken);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Could not store reception kiosk session artifact '{relativePath}'.");

        return relativePath;
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        Result<LocalFile> result = await storage.DownloadAsync(relativePath, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return null;

        await using Stream stream = result.Value.OpenReadStream();
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    public async Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        await storage.DeleteAsync(relativePath, cancellationToken);
    }
}
