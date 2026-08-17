using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;

namespace Fabric.Server.Sagas.ContractorJobs;

public sealed class ContractorJobAccessAutomationWorker(
    IServiceScopeFactory scopeFactory,
    ContractorJobAccessAutomationTrigger trigger,
    ILogger<ContractorJobAccessAutomationWorker> logger,
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
        ContractorJobAccessAutomationService service = scope.ServiceProvider.GetRequiredService<ContractorJobAccessAutomationService>();
        IReadOnlyList<ContractorJobAccessAutomationWorkItem> workItems = await service.GetDueWorkItemsAsync(cancellationToken);
        foreach (ContractorJobAccessAutomationWorkItem workItem in workItems)
            await ProcessAsync(workItem, cancellationToken);
    }

    private async Task ProcessAsync(ContractorJobAccessAutomationWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            if (!await SetTenantAsync(scope.ServiceProvider, workItem.TenantId, cancellationToken))
                return;

            ContractorJobAccessAutomationService service = scope.ServiceProvider.GetRequiredService<ContractorJobAccessAutomationService>();
            await service.ReconcileAsync(workItem.AssignmentId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reconciling contractor access automation for assignment {AssignmentId} in tenant {TenantId}", workItem.AssignmentId, workItem.TenantId);
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
