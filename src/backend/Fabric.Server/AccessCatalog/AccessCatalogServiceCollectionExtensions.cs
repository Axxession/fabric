using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog;

public static class AccessCatalogServiceCollectionExtensions
{
    public static IServiceCollection SetupAccessCatalog(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<AccessCatalogDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", AccessCatalogDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(AccessCatalogJsonSerializerContext.Default));

        collection.AddScoped<CatalogService>();
        collection.AddScoped<PackageService>();
        collection.AddScoped<AccessGrantService>();
        collection.AddScoped<ApprovalConfigurationService>();
        collection.AddScoped<PackageRequestService>();
        collection.AddScoped<ApprovalDecisionService>();
        collection.AddHostedService<PackageRequestExpiryWorker>();

        return collection;
    }
}
