namespace Fabric.Server.Tenants.Domain;

public sealed class TenantIntegration
{
    private TenantIntegration() { }

    public string TenantId { get; private set; } = null!;
    public TenantIntegrationName Name { get; private set; }
    public string DataJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TenantIntegration Create(string tenantId, TenantIntegrationName name, string dataJson, DateTimeOffset now) =>
        new()
        {
            TenantId = tenantId,
            Name = name,
            DataJson = dataJson,
            CreatedAt = now,
            UpdatedAt = now,
        };

    public void UpdateData(string dataJson, DateTimeOffset now)
    {
        DataJson = dataJson;
        UpdatedAt = now;
    }
}

public enum TenantIntegrationName
{
    Keycloak,
    MicrosoftGraph,
}
