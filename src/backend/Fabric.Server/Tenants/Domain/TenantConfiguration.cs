using Fabric.Server.Notifications;

namespace Fabric.Server.Tenants.Domain;

public sealed record TenantConfiguration
{
    public OidcSettings Oidc { get; init; } = null!;
    public ThemeSettings Theme { get; init; } = ThemeSettings.Default;
    public LogoSettings? Logo { get; init; }
    public GraphEmailSettings? GraphEmail { get; init; }
    public HostSettings Host { get; init; } = new();
}
