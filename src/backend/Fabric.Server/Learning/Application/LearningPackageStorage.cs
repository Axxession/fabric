using System.Security.Cryptography;
using Fabric.Server.Infrastructure.Storage;
using ManagedCode.Communication;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;

namespace Fabric.Server.Learning.Application;

public interface ILearningPackageStorage
{
    Task<(string StoragePath, string? ManifestChecksum)> SavePackageAsync(Guid courseId, Guid courseVersionId, string sourceDirectory, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string packagePath, string relativePath, CancellationToken cancellationToken);
}

public sealed class LearningPackageStorage(IStorage storage, IStoragePathBuilder pathBuilder) : ILearningPackageStorage
{
    public async Task<(string StoragePath, string? ManifestChecksum)> SavePackageAsync(Guid courseId, Guid courseVersionId, string sourceDirectory, CancellationToken cancellationToken)
    {
        string storagePath = pathBuilder.BuildTenantScopedPath("learning", "courses", courseId.ToString("N"), "versions", courseVersionId.ToString("N"));
        string manifestPath = Path.Combine(sourceDirectory, "imsmanifest.xml");
        string? manifestChecksum = File.Exists(manifestPath) ? await ComputeSha256Async(manifestPath, cancellationToken) : null;

        foreach (string filePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDirectory, filePath).Replace('\\', '/');
            string targetPath = string.Join('/', [storagePath, relativePath]);
            string? directory = Path.GetDirectoryName(targetPath)?.Replace('\\', '/');
            await using FileStream stream = File.OpenRead(filePath);
            UploadOptions options = new(fileName: Path.GetFileName(targetPath), directory: directory, mimeType: ResolveContentType(filePath));
            Result<BlobMetadata> result = await storage.UploadAsync(stream, options, cancellationToken);
            if (!result.IsSuccess)
                throw new InvalidOperationException($"Could not store learning package file '{targetPath}'.");
        }

        return (storagePath, manifestChecksum);
    }

    public async Task<Stream?> OpenReadAsync(string packagePath, string relativePath, CancellationToken cancellationToken)
    {
        string normalizedRelativePath = NormalizeRelativePath(relativePath);
        string fullPath = string.Join('/', [packagePath.TrimEnd('/'), normalizedRelativePath]);
        Result<LocalFile> result = await storage.DownloadAsync(fullPath, cancellationToken);
        if (!result.IsSuccess || result.Value is null)
            return null;

        return result.Value.OpenReadStream();
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(filePath);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static string ResolveContentType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".html" or ".htm" => "text/html",
            ".js" => "application/javascript",
            ".css" => "text/css",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        string[] parts = relativePath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new InvalidOperationException("Learning package path cannot be empty.");

        if (parts.Any(part => part is "." or ".."))
            throw new InvalidOperationException("Learning package path contains invalid traversal tokens.");

        return string.Join('/', parts);
    }
}
