namespace Fabric.Server.Integrations.Keycloak;

public enum KeycloakAdminErrorCode
{
    IntegrationDisabled,
    IntegrationNotConfigured,
    InvalidRequest,
    NotFound,
    Conflict,
    ExternalServiceError,
}

public sealed record KeycloakAdminError(KeycloakAdminErrorCode Code, string Detail);
