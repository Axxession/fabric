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

internal sealed class MigrationRunner<T>(IServiceProvider serviceProvider) where T : DbContext
{
    public async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        T dbContext = serviceProvider.GetRequiredService<T>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}

public sealed class MigrationsRunner(IServiceScopeFactory scopeFactory)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        await new MigrationRunner<TenantsDbContext>(services).RunMigrationsAsync(cancellationToken);
        await services.GetRequiredService<TenantSeeder>().SeedAsync(cancellationToken);
        await new MigrationRunner<IdentitiesDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<EmployeesDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<CredentialManagementDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<AccessControlDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<AccessCatalogDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<VisitorsDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<SagasDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<DesfireDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<HardwareDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<KioskDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<LocationsDbContext>(services).RunMigrationsAsync(cancellationToken);
        await new MigrationRunner<ReceptionDbContext>(services).RunMigrationsAsync(cancellationToken);
    }
}
