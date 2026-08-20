namespace Fabric.Server.Tenants.Domain;

public sealed record TenantConfiguration
{
    public OidcSettings Oidc { get; init; } = null!;
    public LogoSettings? Logo { get; init; }
    public HostSettings Host { get; init; } = new();
}
