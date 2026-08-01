using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.EmployeeLifecycle;

public sealed class EmployeeLifecycleAutomationWorker(
    IServiceScopeFactory scopeFactory,
    EmployeeLifecycleAutomationTrigger trigger,
    ILogger<EmployeeLifecycleAutomationWorker> logger,
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

                while (trigger.TryRead()) { }
                await ProcessDueAsync(stoppingToken);
                triggerReady = trigger.WaitToReadAsync(stoppingToken).AsTask();
            }

            if (completed == timerReady)
            {
                if (!await timerReady)
                    break;

                await ProcessDueAsync(stoppingToken);
                timerReady = timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }
        }
    }

    private async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        EmployeeLifecycleAutomationService service = scope.ServiceProvider.GetRequiredService<EmployeeLifecycleAutomationService>();
        IReadOnlyList<EmployeeAccessAutomationWorkItem> workItems = await service.GetDueWorkItemsAsync(cancellationToken);
        foreach (EmployeeAccessAutomationWorkItem workItem in workItems)
            await ProcessAsync(workItem, cancellationToken);
    }

    private async Task ProcessAsync(EmployeeAccessAutomationWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            if (!await SetTenantAsync(scope.ServiceProvider, workItem.TenantId, cancellationToken))
                return;

            EmployeeLifecycleAutomationService service = scope.ServiceProvider.GetRequiredService<EmployeeLifecycleAutomationService>();
            await service.ReconcileAsync(workItem.EmployeeId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reconciling employee access automation for employee {EmployeeId} in tenant {TenantId}", workItem.EmployeeId, workItem.TenantId);
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
