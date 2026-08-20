using System.Text.Json.Serialization;
using Fabric.Server.Tenants.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Tenants;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(TenantSettingsResponse))]
[JsonSerializable(typeof(AdminTenantSettingsResponse))]
[JsonSerializable(typeof(OidcSettingsResponse))]
[JsonSerializable(typeof(ThemeSettingsResponse))]
[JsonSerializable(typeof(LogoSettingsResponse))]
[JsonSerializable(typeof(PlatformAuthSettingsResponse))]
[JsonSerializable(typeof(PlatformTenantListItemResponse))]
[JsonSerializable(typeof(List<PlatformTenantListItemResponse>))]
[JsonSerializable(typeof(PlatformTenantListItemResponse[]))]
[JsonSerializable(typeof(PlatformTenantResponse))]
[JsonSerializable(typeof(HostSettingsResponse))]
[JsonSerializable(typeof(PlatformTenantIntegrationSummaryResponse))]
[JsonSerializable(typeof(CreatePlatformTenantRequest))]
[JsonSerializable(typeof(UpdatePlatformTenantRequest))]
[JsonSerializable(typeof(UpdateOidcSettingsRequest))]
[JsonSerializable(typeof(UpdateTenantSettingsRequest))]
[JsonSerializable(typeof(KeycloakIntegrationResponse))]
[JsonSerializable(typeof(UpdateKeycloakIntegrationRequest))]
[JsonSerializable(typeof(MicrosoftGraphIntegrationResponse))]
[JsonSerializable(typeof(UpdateMicrosoftGraphIntegrationRequest))]
internal sealed partial class TenantsJsonSerializerContext : JsonSerializerContext;
