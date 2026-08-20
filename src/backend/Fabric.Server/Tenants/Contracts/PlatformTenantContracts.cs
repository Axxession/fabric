using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Tenants.Contracts;

public sealed record PlatformAuthSettingsResponse(OidcSettingsResponse Oidc);

public sealed record PlatformTenantListItemResponse(
    string Id,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    OidcSettingsResponse Oidc,
    HostSettingsResponse Host);

public sealed record PlatformTenantResponse(
    string Id,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    OidcSettingsResponse Oidc,
    LogoSettingsResponse? Logo,
    HostSettingsResponse Host,
    bool CanProvisionKeycloakRealm,
    PlatformTenantIntegrationSummaryResponse Keycloak,
    PlatformTenantIntegrationSummaryResponse MicrosoftGraph);

public sealed record HostSettingsResponse(HostAssignmentMode AssignmentMode);

public sealed record PlatformTenantIntegrationSummaryResponse(
    bool IsConfigured,
    bool IsEnabled,
    bool HasSecret,
    DateTimeOffset? UpdatedAtUtc);

public sealed record CreatePlatformTenantRequest(
    string Id,
    string DisplayName,
    UpdateOidcSettingsRequest Oidc);

public sealed record UpdatePlatformTenantRequest(
    string DisplayName,
    UpdateOidcSettingsRequest Oidc);
