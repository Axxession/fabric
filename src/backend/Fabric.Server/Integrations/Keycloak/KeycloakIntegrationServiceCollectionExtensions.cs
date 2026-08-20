namespace Fabric.Server.Integrations.Keycloak;

public static class KeycloakIntegrationServiceCollectionExtensions
{
    public const string HttpClientName = "KeycloakAdmin";

    public static IServiceCollection SetupKeycloakIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<KeycloakRealmProvisioningOptions>()
            .Bind(configuration.GetSection(KeycloakRealmProvisioningOptions.SectionName))
            .Validate(options => !options.HasAnyValue() || options.IsConfigured(),
                "KeycloakRealmProvisioning must include Url, Realm, ClientId and ClientSecret when configured.")
            .Validate(options => !options.HasAnyValue() || Uri.TryCreate(options.Url, UriKind.Absolute, out _),
                "KeycloakRealmProvisioning:Url must be an absolute URL.")
            .ValidateOnStart();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(KeycloakJsonSerializerContext.Default));

        services.AddHttpClient(HttpClientName);
        services.AddSingleton<KeycloakAdminTokenProvider>();
        services.AddScoped<IKeycloakTenantAdmin, KeycloakTenantAdmin>();
        services.AddScoped<KeycloakRealmProvisioningService>();
        return services;
    }
}
