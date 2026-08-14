using Fabric.Server.Hardware.Application;
using Fabric.Server.Hardware.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fabric.Server.Hardware;

public static class HardwareServiceCollectionExtensions
{
    public static IServiceCollection SetupHardware(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<HardwareDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", HardwareDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(HardwareJsonSerializerContext.Default));

        collection.AddOptions<HardwareConnectionOptions>()
            .Bind(configuration.GetSection(HardwareConnectionOptions.SectionName))
            .Validate(options => options.StaleAfter > TimeSpan.Zero, "Hardware connection stale timeout must be greater than zero.")
            .Validate(options => options.OfflineAfter > options.StaleAfter, "Hardware connection offline timeout must be greater than stale timeout.")
            .ValidateOnStart();

        collection.AddScoped<HardwareAgentKeyHasher>();
        collection.AddScoped<IQrScanner, QrScanner>();
        collection.AddScoped<ILabelPrinter, LabelPrinter>();
        collection.AddScoped<ICardPrinter, CardPrinter>();
        collection.AddSingleton<HardwareCommandStore>();
        collection.AddSingleton<HardwareAgentConnectionManager>();
        collection.AddSingleton<HardwareConnectionStatusResolver>();

        return collection;
    }
}
