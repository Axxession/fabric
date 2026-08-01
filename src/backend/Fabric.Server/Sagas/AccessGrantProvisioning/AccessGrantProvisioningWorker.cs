using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.AccessGrantProvisioning;

public sealed class AccessGrantProvisioningWorker(
    IServiceScopeFactory scopeFactory,
    AccessGrantProvisioningSagaTrigger trigger,
    ILogger<AccessGrantProvisioningWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollInterval, timeProvider);
        Task<bool> triggerReady = trigger.WaitToReadAsync(stoppingToken).AsTask();
        Task<bool> timerReady = timer.WaitForNextTickAsync(stoppingToken).AsTask();
        DateTimeOffset nextCleanupAt = timeProvider.GetUtcNow().Add(CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            Task<bool> completed = await Task.WhenAny(triggerReady, timerReady);

            if (completed == triggerReady)
            {
                if (!await triggerReady)
                    break;

                while (trigger.TryRead()) { }
                await ProcessDueEventsAsync(stoppingToken);
                triggerReady = trigger.WaitToReadAsync(stoppingToken).AsTask();
            }

            if (completed == timerReady)
            {
                if (!await timerReady)
                    break;

                await ProcessDueEventsAsync(stoppingToken);
                if (timeProvider.GetUtcNow() >= nextCleanupAt)
                {
                    await CleanupProcessedEventsAsync(stoppingToken);
                    nextCleanupAt = timeProvider.GetUtcNow().Add(CleanupInterval);
                }

                timerReady = timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }
        }
    }

    private async Task ProcessDueEventsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            AccessGrantProvisioningSagaService service = scope.ServiceProvider.GetRequiredService<AccessGrantProvisioningSagaService>();
            IReadOnlyList<AccessGrantProvisioningSagaEventWorkItem> workItems = await service.GetDueEventWorkItemsAsync(cancellationToken);

            foreach (AccessGrantProvisioningSagaEventWorkItem workItem in workItems)
                await ProcessEventAsync(workItem, cancellationToken);

            if (workItems.Count == 0)
                break;
        }
    }

    private async Task ProcessEventAsync(AccessGrantProvisioningSagaEventWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            if (!await SetTenantAsync(scope.ServiceProvider, workItem.TenantId, cancellationToken))
                return;

            AccessGrantProvisioningSagaService service = scope.ServiceProvider.GetRequiredService<AccessGrantProvisioningSagaService>();
            await service.ProcessEventAsync(workItem.EventId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing access grant provisioning event {EventId} for tenant {TenantId}", workItem.EventId, workItem.TenantId);
        }
    }

    private async Task CleanupProcessedEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            TenantsDbContext tenantsDb = scope.ServiceProvider.GetRequiredService<TenantsDbContext>();
            List<string> tenantIds = await tenantsDb.Tenants
                .AsNoTracking()
                .Select(tenant => tenant.Id)
                .ToListAsync(cancellationToken);

            foreach (string tenantId in tenantIds)
            {
                await using AsyncServiceScope tenantScope = scopeFactory.CreateAsyncScope();
                if (!await SetTenantAsync(tenantScope.ServiceProvider, tenantId, cancellationToken))
                    continue;

                AccessGrantProvisioningSagaService service = tenantScope.ServiceProvider.GetRequiredService<AccessGrantProvisioningSagaService>();
                await service.CleanupProcessedEventsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up processed access grant provisioning saga events");
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
