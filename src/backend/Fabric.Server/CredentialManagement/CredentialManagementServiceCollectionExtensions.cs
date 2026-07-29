using Fabric.Server.CredentialManagement.Application;
using Fabric.Server.CredentialManagement.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.CredentialManagement;

public static class CredentialManagementServiceCollectionExtensions
{
    public static IServiceCollection SetupCredentialManagement(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<CredentialManagementDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", CredentialManagementDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(CredentialManagementJsonSerializerContext.Default));

        collection.AddScoped<CredentialManagementService>();
        collection.AddHostedService<CredentialRecycleWorker>();
        return collection;
    }
}
