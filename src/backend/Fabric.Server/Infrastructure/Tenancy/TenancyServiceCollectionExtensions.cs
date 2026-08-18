namespace Fabric.Server.Infrastructure.Tenancy;

public static class TenancyServiceCollectionExtensions
{
    public static IServiceCollection AddTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TenancyOptions>()
            .Bind(configuration.GetSection(TenancyOptions.SectionName))
            .Validate(options => Enum.IsDefined(options.Mode), "Tenancy mode must be SingleTenant or MultiTenant.")
            .Validate(options => IsValidTenantBaseUrl(options.TenantBaseUrl), "Tenancy:TenantBaseUrl must be an absolute URL.")
            .Validate(options => options.Mode != TenancyMode.SingleTenant || !string.IsNullOrWhiteSpace(options.DefaultTenant.Id),
                "Tenancy:DefaultTenant:Id is required in SingleTenant mode.")
            .Validate(options => options.Mode != TenancyMode.SingleTenant || IsValidOidcOptions(options.DefaultTenant.Oidc),
                "Tenancy:DefaultTenant:Oidc must include MetadataUrl and ClientId. MetadataUrl must be HTTPS unless RequireHttpsMetadata is false.")
            .Validate(options => options.Mode != TenancyMode.SingleTenant || options.DefaultTenant.GraphEmail is null || options.DefaultTenant.GraphEmail.IsConfigured(),
                "Tenancy:DefaultTenant:GraphEmail must include FromEmail, FromName, AzureTenantId, ApplicationId and Secret when configured.")
            .Validate(options => options.Mode != TenancyMode.SingleTenant || options.DefaultTenant.Keycloak is null || options.DefaultTenant.Keycloak.IsConfigured(),
                "Tenancy:DefaultTenant:Keycloak must include Url, Realm, ClientId and ClientSecret when configured.")
            .ValidateOnStart();

        services.AddOptions<AdminOidcOptions>()
            .Bind(configuration.GetSection(AdminOidcOptions.SectionName))
            .Validate(options => GetTenancyMode(configuration) != TenancyMode.MultiTenant || IsValidOidcOptions(options),
                "AdminOidc must include MetadataUrl and ClientId. MetadataUrl must be HTTPS unless RequireHttpsMetadata is false.")
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextAccessor>(provider => provider.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantStore, TenantStore>();
        services.AddScoped<TenantBaseUrlResolver>();
        services.AddScoped<TenantSeeder>();

        return services;
    }

    private static TenancyMode GetTenancyMode(IConfiguration configuration) =>
        configuration.GetSection(TenancyOptions.SectionName).Get<TenancyOptions>()?.Mode ?? TenancyMode.SingleTenant;

    private static bool IsValidOidcOptions(OidcOptions options)
    {
        if (!options.IsConfigured())
            return false;

        if (!Uri.TryCreate(options.MetadataUrl, UriKind.Absolute, out Uri? metadataUrl))
            return false;

        return !options.RequireHttpsMetadata || metadataUrl.Scheme == Uri.UriSchemeHttps;
    }

    private static bool IsValidTenantBaseUrl(string tenantBaseUrl)
    {
        string sampleUrl = tenantBaseUrl.Replace("{tenant}", "tenant", StringComparison.OrdinalIgnoreCase);
        return Uri.TryCreate(sampleUrl, UriKind.Absolute, out _);
    }
}
