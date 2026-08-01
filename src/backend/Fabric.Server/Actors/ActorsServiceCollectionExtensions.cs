using Fabric.Server.Actors.Application;

namespace Fabric.Server.Actors;

public static class ActorsServiceCollectionExtensions
{
    public static IServiceCollection SetupActors(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(ActorsJsonSerializerContext.Default));

        services.AddScoped<CurrentActorService>();
        return services;
    }
}
