using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.CredentialManagement.Application;

public sealed class CredentialRecycleWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CredentialRecycleWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CredentialRecycleWorkerLog.WorkerStarted(logger);

        using PeriodicTimer timer = new(PollInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessTenantsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                CredentialRecycleWorkerLog.WorkerFailed(logger, ex);
            }
        }

        CredentialRecycleWorkerLog.WorkerStopped(logger);
    }

    private async Task ProcessTenantsAsync(CancellationToken cancellationToken)
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

                CredentialManagementService service = tenantScope.ServiceProvider.GetRequiredService<CredentialManagementService>();
                int expiredCount = await service.ProcessExpiredCredentialsAsync(cancellationToken);
                int releasedReservations = await service.ReleaseExpiredReservationsAsync(cancellationToken);

                if (expiredCount > 0 || releasedReservations > 0)
                    CredentialRecycleWorkerLog.ProcessedTenant(logger, tenantId, expiredCount, releasedReservations);
            }
            catch (Exception ex)
            {
                CredentialRecycleWorkerLog.TenantFailed(logger, tenantId, ex);
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

internal static partial class CredentialRecycleWorkerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Credential recycle worker started")]
    public static partial void WorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Credential recycle worker stopped")]
    public static partial void WorkerStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Credential recycle worker failed")]
    public static partial void WorkerFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Credential recycle worker processed tenant {TenantId}: expired work items {ExpiredCount}, released reservations {ReleasedReservationCount}")]
    public static partial void ProcessedTenant(ILogger logger, string tenantId, int expiredCount, int releasedReservationCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Credential recycle worker failed for tenant {TenantId}")]
    public static partial void TenantFailed(ILogger logger, string tenantId, Exception exception);
}
