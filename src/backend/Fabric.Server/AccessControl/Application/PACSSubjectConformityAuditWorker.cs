using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class PACSSubjectConformityAuditWorker(
    IServiceScopeFactory scopeFactory,
    PACSSubjectConformityAuditTrigger trigger,
    ILogger<PACSSubjectConformityAuditWorker> logger,
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

                while (trigger.TryRead(out PACSSubjectConformityAuditWorkItem? workItem) && workItem is not null)
                    await ProcessAuditAsync(workItem, stoppingToken);

                triggerReady = trigger.WaitToReadAsync(stoppingToken).AsTask();
            }

            if (completed == timerReady)
            {
                if (!await timerReady)
                    break;

                await ProcessDueAuditsAsync(stoppingToken);
                timerReady = timer.WaitForNextTickAsync(stoppingToken).AsTask();
            }
        }
    }

    private async Task ProcessDueAuditsAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        PACSSubjectConformityAuditService service = scope.ServiceProvider.GetRequiredService<PACSSubjectConformityAuditService>();
        IReadOnlyList<PACSSubjectConformityAuditWorkItem> workItems = await service.GetDueWorkItemsAsync(cancellationToken);
        foreach (PACSSubjectConformityAuditWorkItem workItem in workItems)
            await ProcessAuditAsync(workItem, cancellationToken);
    }

    private async Task ProcessAuditAsync(PACSSubjectConformityAuditWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            if (!await SetTenantAsync(scope.ServiceProvider, workItem.TenantId, cancellationToken))
                return;

            PACSSubjectConformityAuditService service = scope.ServiceProvider.GetRequiredService<PACSSubjectConformityAuditService>();
            await service.AuditAsync(workItem.IdentityId, workItem.AccessControlSystemId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auditing PACS subject conformity for identity {IdentityId}, system {SystemId}, tenant {TenantId}", workItem.IdentityId, workItem.AccessControlSystemId, workItem.TenantId);
        }
        finally
        {
            trigger.Complete(workItem);
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
