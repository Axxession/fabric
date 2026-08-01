using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Application;

public sealed class PackageRequestExpiryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PackageRequestExpiryWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await ExpireRequestsAsync(stoppingToken);
    }

    private async Task ExpireRequestsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        TenantsDbContext tenantsDb = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();
        List<string> tenantIds = await tenantsDb.Tenants.AsNoTracking().Select(item => item.Id).ToListAsync(cancellationToken);

        foreach (string tenantId in tenantIds)
        {
            try
            {
                await using AsyncServiceScope tenantScope = scopeFactory.CreateAsyncScope();
                if (!await SetTenantAsync(tenantScope.ServiceProvider, tenantId, cancellationToken))
                    continue;

                PackageRequestService service = tenantScope.ServiceProvider.GetRequiredService<PackageRequestService>();
                IReadOnlyList<Guid> requestIds = await service.GetExpirableRequestIdsAsync(cancellationToken);

                foreach (Guid requestId in requestIds)
                    _ = await service.ExpireAsync(requestId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error expiring package requests for tenant {TenantId}", tenantId);
            }
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
