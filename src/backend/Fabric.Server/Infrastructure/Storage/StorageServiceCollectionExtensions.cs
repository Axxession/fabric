using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;

namespace Fabric.Server.Infrastructure.Storage;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection SetupStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(ValidateProviderOptions, "Storage provider configuration is invalid.")
            .ValidateOnStart();

        StorageOptions storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();

        services.AddScoped<IStoragePathBuilder, StoragePathBuilder>();

        switch (storageOptions.Provider)
        {
            case StorageProviderKind.Azure:
                services.AddAzureStorageAsDefault(new AzureStorageOptions
                {
                    ConnectionString = storageOptions.Azure.ConnectionString,
                    Container = storageOptions.Azure.Container,
                    PublicAccessType = storageOptions.Azure.PublicAccessType
                });
                break;
            default:
                services.AddFileSystemStorageAsDefault(new FileSystemStorageOptions
                {
                    BaseFolder = ResolveFileSystemBasePath(storageOptions.FileSystem.BasePath)
                });
                break;
        }

        return services;
    }

    internal static string ResolveFileSystemBasePath(string? configuredBasePath)
    {
        if (!string.IsNullOrWhiteSpace(configuredBasePath))
            return Path.GetFullPath(configuredBasePath);

        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(basePath))
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

        return Path.Combine(basePath, "fabric", "storage");
    }

    private static bool ValidateProviderOptions(StorageOptions options)
    {
        return options.Provider != StorageProviderKind.Azure
            || !string.IsNullOrWhiteSpace(options.Azure.ConnectionString);
    }
}
