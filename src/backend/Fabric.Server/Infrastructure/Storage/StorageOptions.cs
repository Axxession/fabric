using System.ComponentModel.DataAnnotations;
using Azure.Storage.Blobs.Models;

namespace Fabric.Server.Infrastructure.Storage;

public enum StorageProviderKind
{
    FileSystem = 0,
    Azure = 1
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public StorageProviderKind Provider { get; init; } = StorageProviderKind.FileSystem;
    public FileSystemStorageSettings FileSystem { get; init; } = new();
    public AzureBlobStorageSettings Azure { get; init; } = new();
}

public sealed class FileSystemStorageSettings
{
    public string? BasePath { get; init; }
}

public sealed class AzureBlobStorageSettings
{
    public string? ConnectionString { get; init; }

    [Required]
    public string Container { get; init; } = "fabric";

    public PublicAccessType PublicAccessType { get; init; } = PublicAccessType.None;
}
