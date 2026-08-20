using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Fabric.Server.Infrastructure.Authentication;

public interface IPlatformOidcConfigurationStore
{
    Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        AdminOidcOptions settings,
        CancellationToken cancellationToken);

    void RequestRefresh(AdminOidcOptions settings);
}
