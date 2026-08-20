using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Tenants.Contracts;

public static class TenantSettingsMapper
{
    public static TenantSettingsResponse ToResponse(this TenantConfiguration configuration, string version) =>
        new(
            version,
            configuration.Oidc.ToResponse(),
            configuration.Logo?.ToResponse());

    public static AdminTenantSettingsResponse ToAdminResponse(this TenantConfiguration configuration, string version) =>
        new(
            version,
            configuration.Oidc.ToResponse(),
            configuration.Logo?.ToResponse());

    public static OidcSettingsResponse ToResponse(this OidcSettings oidc) =>
        new(oidc.MetadataUrl, oidc.ClientId, oidc.RequireHttpsMetadata);

    public static LogoSettingsResponse ToResponse(this LogoSettings logo) =>
        new(logo.ContentType, Convert.ToBase64String(logo.Data));

}
