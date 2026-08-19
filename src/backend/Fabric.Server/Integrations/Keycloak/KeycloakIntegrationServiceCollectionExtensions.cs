namespace Fabric.Server.Integrations.Keycloak;

public static class KeycloakIntegrationServiceCollectionExtensions
{
    public const string HttpClientName = "KeycloakAdmin";

    public static IServiceCollection SetupKeycloakIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(KeycloakJsonSerializerContext.Default));

        services.AddHttpClient(HttpClientName);
        services.AddSingleton<KeycloakAdminTokenProvider>();
        services.AddScoped<IKeycloakTenantAdmin, KeycloakTenantAdmin>();
        return services;
    }
}
