using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl;

public static class AccessControlServiceCollectionExtensions
{
    public static IServiceCollection SetupAccessControl(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.AddDbContext<AccessControlDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", AccessControlDbContext.Schema));
        });

        collection.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Add(AccessControlJsonSerializerContext.Default));

        collection.AddScoped<AccessControlSystemService>();
        collection.AddScoped<AccessItemService>();
        collection.AddScoped<AccessLevelTargetService>();
        collection.AddScoped<CredentialPACSAssignmentService>();
        collection.AddScoped<UnipassCredentialPacsProvisioner>();
        collection.AddScoped<AccessControlLocationResolver>();
        collection.AddScoped<PACSSubjectService>();
        collection.AddScoped<UnipassPACSSubjectProvisioner>();
        collection.AddScoped<PACSSubjectProvisioningService>();
        collection.AddSingleton<PACSProvisioningReconciliationTrigger>();
        collection.AddScoped<UnipassPACSProvisioner>();
        collection.AddScoped<PACSProvisioningReconciliationService>();
        collection.AddHostedService<PACSProvisioningWorker>();
        collection.AddScoped<UnipassPACSAssignmentProvisioner>();
        collection.AddScoped<PACSAssignmentService>();
        collection.AddScoped<UnipassApiFactory>();
        collection.AddHostedService<CredentialPacsAssignmentWorker>();
        collection.AddHostedService<PACSSubjectProvisioningWorker>();

        return collection;
    }
}
