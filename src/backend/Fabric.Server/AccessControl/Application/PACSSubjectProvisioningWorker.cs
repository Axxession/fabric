using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class PACSSubjectProvisioningWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PACSSubjectProvisioningWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ProcessDueProvisioningsAsync(stoppingToken);
    }

    private async Task ProcessDueProvisioningsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        TenantsDbContext tenantsDb = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();
        List<string> tenantIds = await tenantsDb.Tenants
            .AsNoTracking()
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken);

        foreach (string tenantId in tenantIds)
            await ProcessDueProvisioningsAsync(tenantId, cancellationToken);
    }

    private async Task ProcessDueProvisioningsAsync(string tenantId, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            if (!await SetTenantAsync(scope.ServiceProvider, tenantId, cancellationToken))
                return;

            PACSSubjectProvisioningService service = scope.ServiceProvider.GetRequiredService<PACSSubjectProvisioningService>();
            IReadOnlyList<Guid> provisioningIds = await service.GetDueProvisioningIdsAsync(cancellationToken);
            foreach (Guid provisioningId in provisioningIds)
                await service.ApplyAsync(provisioningId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PACS subject provisionings for tenant {TenantId}", tenantId);
        }
    }

    private static async Task<bool> SetTenantAsync(IServiceProvider serviceProvider, string tenantId, CancellationToken cancellationToken)
    {
        ITenantStore tenantStore = serviceProvider.GetRequiredService<ITenantStore>();
        TenantInfo? tenant = await tenantStore.GetTenantAsync(tenantId, cancellationToken);
        if (tenant is null)
            return false;

        ITenantContextAccessor tenantContext = serviceProvider.GetRequiredService<ITenantContextAccessor>();
        tenantContext.SetTenant(tenant);
        return true;
    }
}
