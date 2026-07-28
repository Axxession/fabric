using Fabric.Server.Reception.Application;
using Fabric.Server.Reception.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception;

public static class ReceptionServiceCollectionExtensions
{
    public static IServiceCollection SetupReception(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<ReceptionDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", ReceptionDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(ReceptionJsonSerializerContext.Default));

        collection.AddScoped<ReceptionService>();
        collection.AddScoped<ReceptionTriggeredPackageAssignmentService>();
        collection.AddScoped<ReceptionKioskKeyHasher>();
        return collection;
    }
}
