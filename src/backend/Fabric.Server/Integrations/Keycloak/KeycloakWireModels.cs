using System.Text.Json.Serialization;

namespace Fabric.Server.Integrations.Keycloak;

internal sealed record KeycloakAccessTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

internal sealed record KeycloakRealmWriteRequest(
    [property: JsonPropertyName("realm")] string Realm,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("enabled")] bool Enabled);

internal sealed record KeycloakUserRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("firstName")] string? FirstName,
    [property: JsonPropertyName("lastName")] string? LastName,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("enabled")] bool Enabled);

internal sealed record KeycloakUserWriteRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("enabled")] bool Enabled);

internal sealed record KeycloakRoleRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description);

internal sealed record KeycloakClientRoleRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("clientRole")] bool ClientRole,
    [property: JsonPropertyName("containerId")] string ContainerId);

internal sealed record KeycloakRoleWriteRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description);

internal sealed record KeycloakClientRoleMappingWriteRequest(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("clientRole")] bool ClientRole,
    [property: JsonPropertyName("containerId")] string ContainerId);

internal sealed record KeycloakGroupRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string? Path);

internal sealed record KeycloakGroupWriteRequest(
    [property: JsonPropertyName("name")] string Name);

internal sealed record KeycloakRoleMappingRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

internal sealed record KeycloakClientRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("clientId")] string ClientId);

internal sealed record KeycloakClientWriteRequest(
    [property: JsonPropertyName("clientId")] string ClientId,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("standardFlowEnabled")] bool StandardFlowEnabled,
    [property: JsonPropertyName("implicitFlowEnabled")] bool ImplicitFlowEnabled,
    [property: JsonPropertyName("directAccessGrantsEnabled")] bool DirectAccessGrantsEnabled,
    [property: JsonPropertyName("publicClient")] bool PublicClient,
    [property: JsonPropertyName("serviceAccountsEnabled")] bool ServiceAccountsEnabled,
    [property: JsonPropertyName("baseUrl")] string? BaseUrl,
    [property: JsonPropertyName("rootUrl")] string? RootUrl,
    [property: JsonPropertyName("redirectUris")] string[] RedirectUris,
    [property: JsonPropertyName("webOrigins")] string[] WebOrigins);

internal sealed record KeycloakProtocolMapperWriteRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("protocolMapper")] string ProtocolMapper,
    [property: JsonPropertyName("config")] Dictionary<string, string> Config);

internal sealed record KeycloakClientSecretRepresentation(
    [property: JsonPropertyName("value")] string Value);

internal sealed record KeycloakUserIdRepresentation(
    [property: JsonPropertyName("id")] string Id);

internal sealed record KeycloakResetPasswordRequest(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("temporary")] bool Temporary);
