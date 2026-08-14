using Fabric.Server.Contractors.Application;
using Fabric.Server.Contractors.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors;

public static class ContractorServiceCollectionExtensions
{
    public static IServiceCollection SetupContractors(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<ContractorsDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", ContractorsDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(ContractorsJsonSerializerContext.Default));

        collection.AddScoped<ContractorsService>();
        return collection;
    }
}
