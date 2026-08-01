using Fabric.Server.Infrastructure.Tenancy;

namespace Fabric.Server.Employees.Application;

public sealed class EmployeeLifecycleWorker(
    IServiceScopeFactory scopeFactory,
    EmployeeLifecycleTrigger trigger,
    ILogger<EmployeeLifecycleWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SafetyInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        EmployeeLifecycleWorkerLog.WorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRecalculationsAsync(stoppingToken);
                await WaitForNextWakeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                EmployeeLifecycleWorkerLog.WorkerFailed(logger, ex);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        EmployeeLifecycleWorkerLog.WorkerStopped(logger);
    }

    private async Task ProcessDueRecalculationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            EmployeeLifecycleService service = scope.ServiceProvider.GetRequiredService<EmployeeLifecycleService>();
            IReadOnlyList<EmployeeLifecycleRecalculationWorkItem> workItems = await service.ClaimDueRecalculationsAsync(BatchSize, ProcessingTimeout, cancellationToken);

            if (workItems.Count == 0)
                break;

            foreach (EmployeeLifecycleRecalculationWorkItem workItem in workItems)
                await ProcessWorkItemAsync(workItem, cancellationToken);
        }
    }

    private async Task ProcessWorkItemAsync(EmployeeLifecycleRecalculationWorkItem workItem, CancellationToken cancellationToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            if (!await SetTenantAsync(scope.ServiceProvider, workItem.TenantId, cancellationToken))
                return;

            EmployeeLifecycleService service = scope.ServiceProvider.GetRequiredService<EmployeeLifecycleService>();
            await service.ReconcileNowAndRescheduleAsync(workItem.EmployeeId, Domain.EmployeeLifecycleSource.ScheduledRecalculation, workItem.Reason, cancellationToken);
            await service.MarkCompletedAsync(workItem.RecalculationId, cancellationToken);
        }
        catch (Exception ex)
        {
            EmployeeLifecycleWorkerLog.WorkItemFailed(logger, workItem.RecalculationId, workItem.EmployeeId, workItem.TenantId, ex);

            await using AsyncServiceScope resetScope = scopeFactory.CreateAsyncScope();
            EmployeeLifecycleService resetService = resetScope.ServiceProvider.GetRequiredService<EmployeeLifecycleService>();
            await resetService.ResetToPendingAsync(workItem.RecalculationId, cancellationToken);
        }
    }

    private async Task WaitForNextWakeAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        EmployeeLifecycleService service = scope.ServiceProvider.GetRequiredService<EmployeeLifecycleService>();
        DateTimeOffset? nextDue = await service.GetNextScheduledForAsync(cancellationToken);
        DateTimeOffset now = timeProvider.GetUtcNow();

        TimeSpan delay = nextDue.HasValue
            ? nextDue.Value <= now
                ? TimeSpan.Zero
                : TimeSpan.FromTicks(Math.Min((nextDue.Value - now).Ticks, SafetyInterval.Ticks))
            : SafetyInterval;

        if (delay <= TimeSpan.Zero)
            return;

        using CancellationTokenSource delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task delayTask = Task.Delay(delay, delayCancellation.Token);
        Task<bool> triggerTask = trigger.WaitToReadAsync(cancellationToken).AsTask();
        Task completed = await Task.WhenAny(delayTask, triggerTask);

        if (completed == triggerTask)
            while (trigger.TryRead()) { }

        delayCancellation.Cancel();
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

internal static partial class EmployeeLifecycleWorkerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Employee lifecycle worker started")]
    public static partial void WorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Employee lifecycle worker stopped")]
    public static partial void WorkerStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Employee lifecycle worker failed")]
    public static partial void WorkerFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Employee lifecycle recalculation {RecalculationId} failed for employee {EmployeeId} in tenant {TenantId}")]
    public static partial void WorkItemFailed(ILogger logger, Guid recalculationId, Guid employeeId, string tenantId, Exception exception);
}
