using Fabric.Server.Identities.Application;
using Fabric.Server.Identities.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Identities;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection SetupIdentities(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<IdentitiesDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", IdentitiesDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(IdentitiesJsonSerializerContext.Default));

        collection.AddScoped<IdentityService>();
        return collection;
    }
}
