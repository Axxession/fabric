using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.CredentialManagement.Persistence;
using Fabric.Server.Desfire.Persistence;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Hardware.Persistence;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Kiosk.Persistence;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Reception.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Tenants.Persistence;
using Fabric.Server.Visitors.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Infrastructure;

internal class MigrationRunner<T>(IServiceScope scope) where T : DbContext
{
    public async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        T dbContext = scope.ServiceProvider.GetRequiredService<T>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

public class MigrationsRunner(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        await new MigrationRunner<TenantsDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<TenantSeeder>().SeedAsync(cancellationToken);
        await new MigrationRunner<IdentitiesDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<EmployeesDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<CredentialManagementDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<AccessControlDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<AccessCatalogDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<VisitorsDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<SagasDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<DesfireDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<HardwareDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<KioskDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<LocationsDbContext>(scope).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<ReceptionDbContext>(scope).RunMigrationsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
