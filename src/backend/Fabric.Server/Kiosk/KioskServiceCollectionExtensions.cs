using Fabric.Server.Kiosk.Application;
using Fabric.Server.Kiosk.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Kiosk;

public static class KioskServiceCollectionExtensions
{
    public static IServiceCollection SetupKiosk(this IServiceCollection collection, IConfiguration configuration)
    {
        IConfigurationSection automationSection = configuration.GetSection("EnableAutomation");
        bool enableAutomation = automationSection.Exists() && automationSection.Get<bool>();

        collection.AddDbContext<KioskDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", KioskDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(KioskJsonSerializerContext.Default));

        collection.Configure<KioskAssetStorageOptions>(configuration.GetSection("Kiosk:AssetStorage"));
        collection.AddScoped<KioskKeyHasher>();
        collection.AddScoped<IKioskAssetStorage, KioskAssetStorage>();
        collection.AddScoped<KioskHardwareBindingResolver>();
        collection.AddScoped<KioskDeviceResolver>();
        collection.AddScoped<KioskInstructionService>();
        collection.AddScoped<KioskSessionCleanupService>();

        if (enableAutomation)
        {
            collection.AddScoped<KioskSessionCancellationService>();
        }

        return collection;
    }
}
