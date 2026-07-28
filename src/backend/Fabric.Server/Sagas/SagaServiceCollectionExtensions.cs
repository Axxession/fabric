using Fabric.Server.Sagas.AccessGrantProvisioning;
using Fabric.Server.Sagas.EmployeeLifecycle;
using Fabric.Server.Sagas.Kiosk;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas;

public static class SagaServiceCollectionExtensions
{
    public static IServiceCollection SetupSagas(this IServiceCollection collection, IConfiguration configuration)
    {
        IConfigurationSection automationSection = configuration.GetSection("EnableAutomation");
        bool enableAutomation = automationSection.Exists() && automationSection.Get<bool>();

        collection.AddDbContext<SagasDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"), x =>
                    x.MigrationsHistoryTable("__EFMigrationsHistory", SagasDbContext.Schema)
                );

        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(SagasJsonSerializerContext.Default));

        collection.AddSingleton<AccessGrantProvisioningSagaTrigger>();
        collection.AddScoped<AccessGrantProvisioningSagaService>();
        collection.AddHostedService<AccessGrantProvisioningWorker>();
        collection.AddSingleton<EmployeeLifecycleAutomationTrigger>();
        collection.AddScoped<EmployeeLifecycleAutomationService>();
        collection.AddHostedService<EmployeeLifecycleAutomationWorker>();
        collection.AddSingleton<VisitorPreOnboardingSagaTrigger>();
        collection.AddScoped<VisitorPreOnboardingSagaService>();
        collection.AddHostedService<VisitorPreOnboardingWorker>();

        if (enableAutomation)
        {
            collection.AddSingleton<KioskSagaTrigger>();
            collection.AddScoped<KioskSagaService>();
            collection.AddHostedService<KioskWorker>();
        }

        return collection;
    }
}
