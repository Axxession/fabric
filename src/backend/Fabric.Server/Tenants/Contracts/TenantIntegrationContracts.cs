namespace Fabric.Server.Tenants.Contracts;

public sealed record KeycloakIntegrationResponse(KeycloakAdminApiIntegrationResponse AdminApi);

public sealed record KeycloakAdminApiIntegrationResponse(
    bool IsEnabled,
    string Url,
    string Realm,
    string ClientId,
    bool HasClientSecret);

public sealed record UpdateKeycloakIntegrationRequest(UpdateKeycloakAdminApiIntegrationRequest AdminApi);

public sealed record UpdateKeycloakAdminApiIntegrationRequest(
    bool IsEnabled,
    string Url,
    string Realm,
    string ClientId,
    string? ClientSecret);

public sealed record MicrosoftGraphIntegrationResponse(MicrosoftGraphEmailIntegrationResponse Email);

public sealed record MicrosoftGraphEmailIntegrationResponse(
    bool IsEnabled,
    string FromEmail,
    string FromName,
    string AzureTenantId,
    string ApplicationId,
    bool SaveSentItems,
    bool HasSecret);

public sealed record UpdateMicrosoftGraphIntegrationRequest(UpdateMicrosoftGraphEmailIntegrationRequest Email);

public sealed record UpdateMicrosoftGraphEmailIntegrationRequest(
    bool IsEnabled,
    string FromEmail,
    string FromName,
    string AzureTenantId,
    string ApplicationId,
    string? Secret,
    bool SaveSentItems);
