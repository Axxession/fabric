using Fabric.Server.Requirements.Application;
using Fabric.Server.Requirements.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements;

public static class RequirementsServiceCollectionExtensions
{
    public static IServiceCollection SetupRequirements(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<RequirementsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", RequirementsDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(RequirementsJsonSerializerContext.Default));

        collection.AddScoped<GrantRequirementsService>();
        collection.AddScoped<RequirementsService>();
        return collection;
    }
}
