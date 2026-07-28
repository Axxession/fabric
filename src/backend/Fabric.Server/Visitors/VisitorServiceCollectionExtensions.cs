using Fabric.Server.Visitors.Application;
using Fabric.Server.Visitors.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Visitors;

public static class VisitorServiceCollectionExtensions
{
    public static IServiceCollection SetupVisitors(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<VisitorsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", VisitorsDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(VisitorsJsonSerializerContext.Default));

        collection.AddScoped<HostService>();
        collection.AddScoped<VisitService>();
        return collection;
    }
}
