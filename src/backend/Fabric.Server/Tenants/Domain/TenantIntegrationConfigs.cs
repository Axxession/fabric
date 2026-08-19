namespace Fabric.Server.Tenants.Domain;

public sealed record KeycloakIntegrationConfig
{
    public KeycloakAdminApiIntegrationConfig AdminApi { get; init; } = new();
}

public sealed record KeycloakAdminApiIntegrationConfig
{
    public bool IsEnabled { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Realm { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(Url)
        && !string.IsNullOrWhiteSpace(Realm)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}

public sealed record MicrosoftGraphIntegrationConfig
{
    public MicrosoftGraphEmailIntegrationConfig Email { get; init; } = new();
}

public sealed record MicrosoftGraphEmailIntegrationConfig
{
    public bool IsEnabled { get; init; }
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string AzureTenantId { get; init; } = string.Empty;
    public string ApplicationId { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public bool SaveSentItems { get; init; }

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(FromEmail)
        && !string.IsNullOrWhiteSpace(FromName)
        && !string.IsNullOrWhiteSpace(AzureTenantId)
        && !string.IsNullOrWhiteSpace(ApplicationId)
        && !string.IsNullOrWhiteSpace(Secret);
}
