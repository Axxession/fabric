namespace Fabric.Server.Tenants.Domain;

public sealed class Tenant
{
    private Tenant() { }

    public string Id { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public TenantConfiguration Configuration { get; private set; } = null!;

    public static Tenant Create(string id, string displayName, TenantConfiguration configuration, DateTimeOffset now) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Configuration = configuration
        };

    public void UpdateDisplayName(string displayName, DateTimeOffset now)
    {
        DisplayName = displayName;
        UpdatedAtUtc = now;
    }

    public void UpdateConfiguration(TenantConfiguration configuration, DateTimeOffset now)
    {
        Configuration = configuration;
        UpdatedAtUtc = now;
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAtUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }
}
