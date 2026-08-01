using Fabric.Server.Tenants.Domain;
using Fabric.Server.Notifications;

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
            configuration.Logo?.ToResponse(),
            configuration.GraphEmail?.ToResponse());

    private static OidcSettingsResponse ToResponse(this OidcSettings oidc) =>
        new(oidc.MetadataUrl, oidc.ClientId, oidc.RequireHttpsMetadata);

    private static ThemeSettingsResponse ToResponse(this ThemeSettings theme) =>
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

    private static LogoSettingsResponse ToResponse(this LogoSettings logo) =>
        new(logo.ContentType, Convert.ToBase64String(logo.Data));

    private static GraphEmailSettingsResponse ToResponse(this GraphEmailSettings email) =>
        new(
            email.FromEmail,
            email.FromName,
            email.AzureTenantId,
            email.ApplicationId,
            email.SaveSentItems,
            !string.IsNullOrWhiteSpace(email.Secret));
}
