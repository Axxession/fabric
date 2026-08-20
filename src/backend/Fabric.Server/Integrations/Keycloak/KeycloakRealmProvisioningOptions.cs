using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Integrations.Keycloak;

public sealed class KeycloakRealmProvisioningOptions
{
    public const string SectionName = "KeycloakRealmProvisioning";

    public string Url { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Url)
        && !string.IsNullOrWhiteSpace(Realm)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);

    public bool HasAnyValue() =>
        !string.IsNullOrWhiteSpace(Url)
        || !string.IsNullOrWhiteSpace(Realm)
        || !string.IsNullOrWhiteSpace(ClientId)
        || !string.IsNullOrWhiteSpace(ClientSecret);

    public KeycloakAdminApiIntegrationConfig ToAdminApiConfig() => new()
    {
        IsEnabled = true,
        Url = Url.Trim(),
        Realm = Realm.Trim(),
        ClientId = ClientId.Trim(),
        ClientSecret = ClientSecret.Trim(),
    };
}
