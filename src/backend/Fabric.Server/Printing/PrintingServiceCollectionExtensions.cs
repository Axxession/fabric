using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Printing;

public static class PrintingServiceCollectionExtensions
{
    public static IServiceCollection SetupPrinting(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<PrintingDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", PrintingDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(PrintingJsonSerializerContext.Default));

        collection.AddScoped<PrintDesignParser>();

        return collection;
    }
}
