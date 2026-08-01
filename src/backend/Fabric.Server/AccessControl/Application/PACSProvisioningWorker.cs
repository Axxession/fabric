using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class PACSProvisioningWorker(
    IServiceScopeFactory scopeFactory,
    PACSProvisioningReconciliationTrigger trigger,
    ILogger<PACSProvisioningWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollInterval, timeProvider);
        Task<bool> triggerReady = trigger.WaitToReadAsync(stoppingToken).AsTask();
        Task<bool> timerReady = timer.WaitForNextTickAsync(stoppingToken).AsTask();

        while (!stoppingToken.IsCancellationRequested)
        {
            Task<bool> completed = await Task.WhenAny(triggerReady, timerReady);

            if (completed == triggerReady)
            {
                if (!await triggerReady)
                    break;

                while (trigger.TryRead(out PACSProvisioningReconciliationWorkItem? workItem) && workItem is not null)
                    await ProcessReconciliationAsync(workItem, stoppingToken);

                triggerReady = trigger.WaitToReadAsync(stoppingToken).AsTask();
            }

            if (completed == timerReady)
            {
                if (!await timerReady)
                    break;

                await ProcessDueReconciliationsAsync(stoppingToken);
                await ProcessDueProvisioningsAsync(stoppingToken);
                await ProcessExpiredProvisioningsAsync(stoppingToken);
                timerReady = timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }
        }
    }

    private async Task ProcessDueReconciliationsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        PACSProvisioningReconciliationService service = scope.ServiceProvider.GetRequiredService<PACSProvisioningReconciliationService>();
        IReadOnlyList<PACSProvisioningReconciliationWorkItem> workItems = await service.GetDueReconciliationWorkItemsAsync(cancellationToken);
        foreach (PACSProvisioningReconciliationWorkItem workItem in workItems)
            await ProcessReconciliationAsync(workItem, cancellationToken);
    }

    private async Task ProcessDueProvisioningsAsync(CancellationToken cancellationToken)
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

                PACSProvisioningReconciliationService service = tenantScope.ServiceProvider.GetRequiredService<PACSProvisioningReconciliationService>();
                IReadOnlyList<Guid> provisioningIds = await service.GetDueProvisioningIdsAsync(cancellationToken);
                foreach (Guid provisioningId in provisioningIds)
                    await service.ApplyProvisioningAsync(provisioningId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing PACS provisionings for tenant {TenantId}", tenantId);
            }
        }
    }

    private async Task ProcessExpiredProvisioningsAsync(CancellationToken cancellationToken)
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

                PACSProvisioningReconciliationService service = tenantScope.ServiceProvider.GetRequiredService<PACSProvisioningReconciliationService>();
                IReadOnlyList<Guid> expiredIds = await service.GetExpiredProvisioningIdsAsync(cancellationToken);
                foreach (Guid provisioningId in expiredIds)
                    await service.RevokeExpiredProvisioningAsync(provisioningId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing expired PACS provisionings for tenant {TenantId}", tenantId);
            }
        }
    }

    private async Task ProcessReconciliationAsync(PACSProvisioningReconciliationWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            if (!await SetTenantAsync(scope.ServiceProvider, workItem.TenantId, cancellationToken))
                return;

            PACSProvisioningReconciliationService service = scope.ServiceProvider.GetRequiredService<PACSProvisioningReconciliationService>();
            await service.ReconcileAsync(workItem.IdentityId, workItem.AccessControlSystemId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reconciling PACS provisionings for identity {IdentityId}, system {SystemId}, tenant {TenantId}", workItem.IdentityId, workItem.AccessControlSystemId, workItem.TenantId);
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
