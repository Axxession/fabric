using System.Collections.Concurrent;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Fabric.Server.Infrastructure.Authentication;

public sealed class PlatformOidcConfigurationStore : IPlatformOidcConfigurationStore
{
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _configurationManagers = new();

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        AdminOidcOptions settings,
        CancellationToken cancellationToken)
    {
        ConfigurationManager<OpenIdConnectConfiguration> manager = GetManager(settings);
        return await manager.GetConfigurationAsync(cancellationToken);
    }

    public void RequestRefresh(AdminOidcOptions settings) => GetManager(settings).RequestRefresh();

    private ConfigurationManager<OpenIdConnectConfiguration> GetManager(AdminOidcOptions settings) =>
        _configurationManagers.GetOrAdd(GetCacheKey(settings), _ =>
        {
            var documentRetriever = new HttpDocumentRetriever
            {
                RequireHttps = settings.RequireHttpsMetadata
            };

            return new ConfigurationManager<OpenIdConnectConfiguration>(
                settings.MetadataUrl!,
                new OpenIdConnectConfigurationRetriever(),
                documentRetriever);
        });

    private static string GetCacheKey(AdminOidcOptions settings) =>
        $"platform:{settings.MetadataUrl}:{settings.RequireHttpsMetadata}";
}
