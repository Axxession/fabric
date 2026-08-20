using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Tenants.Contracts;

public static class TenantSettingsMapper
{
    public static TenantSettingsResponse ToResponse(this TenantConfiguration configuration, string version) =>
        new(
            version,
            configuration.Oidc.ToResponse(),
            configuration.Theme.ToResponse(),
            configuration.Logo?.ToResponse());

    public static AdminTenantSettingsResponse ToAdminResponse(this TenantConfiguration configuration, string version) =>
        new(
            version,
            configuration.Oidc.ToResponse(),
            configuration.Theme.ToResponse(),
            configuration.Logo?.ToResponse());

    public static OidcSettingsResponse ToResponse(this OidcSettings oidc) =>
        new(oidc.MetadataUrl, oidc.ClientId, oidc.RequireHttpsMetadata);

    public static ThemeSettingsResponse ToResponse(this ThemeSettings theme) =>
        new(
            theme.BackgroundColor,
            theme.ContentColor,
            theme.PrimaryColor,
            theme.TextColor,
            theme.TextMutedColor,
            theme.BorderColor,
            theme.HoverBlueColor,
            theme.ActiveBlueColor,
            theme.HoverGrayColor,
            theme.ErrorColor,
            theme.ErrorBackgroundColor,
            theme.DangerColor,
            theme.SuccessColor,
            theme.SuccessBackgroundColor);

    public static LogoSettingsResponse ToResponse(this LogoSettings logo) =>
        new(logo.ContentType, Convert.ToBase64String(logo.Data));

}
