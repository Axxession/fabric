namespace Fabric.Server.Tenants.Contracts;

public sealed record TenantSettingsResponse(
    string Version,
    OidcSettingsResponse Oidc,
    ThemeSettingsResponse Theme,
    LogoSettingsResponse? Logo);

public sealed record AdminTenantSettingsResponse(
    string Version,
    OidcSettingsResponse Oidc,
    ThemeSettingsResponse Theme,
    LogoSettingsResponse? Logo);

public sealed record OidcSettingsResponse(
    string MetadataUrl,
    string ClientId,
    bool RequireHttpsMetadata);

public sealed record ThemeSettingsResponse(
    string BackgroundColor,
    string ContentColor,
    string PrimaryColor,
    string TextColor,
    string TextMutedColor,
    string BorderColor,
    string HoverBlueColor,
    string ActiveBlueColor,
    string HoverGrayColor,
    string ErrorColor,
    string ErrorBackgroundColor,
    string DangerColor,
    string SuccessColor,
    string SuccessBackgroundColor);

public sealed record LogoSettingsResponse(string ContentType, string Data);

public sealed record UpdateTenantSettingsRequest(
    UpdateOidcSettingsRequest Oidc,
    UpdateThemeSettingsRequest Theme);

public sealed record UpdateOidcSettingsRequest(
    string MetadataUrl,
    string ClientId,
    bool RequireHttpsMetadata);

public sealed record UpdateThemeSettingsRequest(
    string BackgroundColor,
    string ContentColor,
    string PrimaryColor,
    string TextColor,
    string TextMutedColor,
    string BorderColor,
    string HoverBlueColor,
    string ActiveBlueColor,
    string HoverGrayColor,
    string ErrorColor,
    string ErrorBackgroundColor,
    string DangerColor,
    string SuccessColor,
    string SuccessBackgroundColor);
