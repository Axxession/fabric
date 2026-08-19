using System.Text.Json.Serialization;

namespace Fabric.Server.Integrations.Keycloak;

internal sealed record KeycloakAccessTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

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

internal sealed record KeycloakRoleWriteRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description);

internal sealed record KeycloakGroupRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("path")] string? Path);

internal sealed record KeycloakGroupWriteRequest(
    [property: JsonPropertyName("name")] string Name);

internal sealed record KeycloakRoleMappingRepresentation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

internal sealed record KeycloakResetPasswordRequest(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("temporary")] bool Temporary);
