namespace Fabric.Server.Tenants.Contracts;

public sealed record TenantSettingsResponse(
    string Version,
    OidcSettingsResponse Oidc,
    LogoSettingsResponse? Logo);

public sealed record AdminTenantSettingsResponse(
    string Version,
    OidcSettingsResponse Oidc,
    LogoSettingsResponse? Logo);

public sealed record OidcSettingsResponse(
    string MetadataUrl,
    string ClientId,
    bool RequireHttpsMetadata);

public sealed record LogoSettingsResponse(string ContentType, string Data);

public sealed record UpdateTenantSettingsRequest(UpdateOidcSettingsRequest Oidc);

public sealed record UpdateOidcSettingsRequest(
    string MetadataUrl,
    string ClientId,
    bool RequireHttpsMetadata);
