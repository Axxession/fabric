using Fabric.Server.Hardware.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Fabric.Server.Desfire.Application;

public sealed class DesfireEncodingWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    DesfireEncodingWakeChannel wakeChannel,
    IOptions<DesfireEncodingOptions> options,
    ILogger<DesfireEncodingWorker> logger) : BackgroundService
{
    private const long SchedulerLeaderLockId = 3847216501;
    private static readonly TimeSpan FollowerRetryInterval = TimeSpan.FromSeconds(30);
    private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";
    private readonly string _connectionString = configuration.GetConnectionString("Database") ?? throw new InvalidOperationException("Database connection string is not configured.");
    private int _tenantCursor;
    private NpgsqlConnection? _leaderConnection;
    private DateTimeOffset _nextLeadershipAttemptAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.DesfireEncodingWorkerStarted(_workerId);
        bool shouldDispatch = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                bool isLeader = await EnsureLeadershipAsync(stoppingToken);
                if (isLeader)
                {
                    if (shouldDispatch)
                        await DispatchUntilDrainedAsync(stoppingToken);

                    shouldDispatch = true;
                    await wakeChannel.WaitAsync(TimeSpan.FromMilliseconds(options.Value.IdleSchedulerIntervalMilliseconds), stoppingToken);
                    continue;
                }

                shouldDispatch = true;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.DesfireEncodingWorkerFailed(_workerId, ex);
            }

            await Task.Delay(FollowerRetryInterval, stoppingToken);
        }

        await ReleaseLeadershipAsync(CancellationToken.None);
        logger.DesfireEncodingWorkerStopped(_workerId);
    }

    private async Task<bool> EnsureLeadershipAsync(CancellationToken cancellationToken)
    {
        if (_leaderConnection?.State == System.Data.ConnectionState.Open)
            return true;

        if (_leaderConnection is not null)
        {
            await _leaderConnection.DisposeAsync();
            _leaderConnection = null;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now < _nextLeadershipAttemptAt)
            return false;

        NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(cancellationToken);

        try
        {
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT pg_try_advisory_lock(@lockId)";
            command.Parameters.AddWithValue("lockId", SchedulerLeaderLockId);
            bool acquired = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
            if (!acquired)
            {
                await connection.DisposeAsync();
                _nextLeadershipAttemptAt = now.Add(FollowerRetryInterval);
                logger.DesfireEncodingWorkerFollower(_workerId, _nextLeadershipAttemptAt);
                return false;
            }

            _leaderConnection = connection;
            _nextLeadershipAttemptAt = now;
            logger.DesfireEncodingWorkerLeaderAcquired(_workerId);
            return true;
        }
        catch
        {
            await connection.DisposeAsync();
            _nextLeadershipAttemptAt = now.Add(FollowerRetryInterval);
            throw;
        }
    }

    private async Task ReleaseLeadershipAsync(CancellationToken cancellationToken)
    {
        if (_leaderConnection is null)
            return;

        try
        {
            if (_leaderConnection.State == System.Data.ConnectionState.Open)
            {
                await using NpgsqlCommand command = _leaderConnection.CreateCommand();
                command.CommandText = "SELECT pg_advisory_unlock(@lockId)";
                command.Parameters.AddWithValue("lockId", SchedulerLeaderLockId);
                await command.ExecuteScalarAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.DesfireEncodingWorkerLeaderReleaseFailed(_workerId, ex);
        }
        finally
        {
            await _leaderConnection.DisposeAsync();
            _leaderConnection = null;
        }
    }

    private async Task DispatchUntilDrainedAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            int dispatched = await DispatchOnceAsync(ct);
            if (dispatched == 0)
                return;

            logger.DesfireEncodingWorkerDrainedBatch(_workerId, dispatched);
            if (options.Value.SchedulerIntervalMilliseconds > 0)
                await Task.Delay(TimeSpan.FromMilliseconds(options.Value.SchedulerIntervalMilliseconds), ct);
        }
    }

    private async Task<int> DispatchOnceAsync(CancellationToken ct)
    {
        List<TenantInfo> tenants;
        using (IServiceScope scope = scopeFactory.CreateScope())
        {
            tenants = await scope.ServiceProvider.GetRequiredService<ITenantStore>().GetAllTenantsAsync(ct);
        }

        if (tenants.Count == 0)
        {
            logger.DesfireEncodingWorkerNoTenants(_workerId);
            return 0;
        }

        logger.DesfireEncodingWorkerDispatching(_workerId, tenants.Count, options.Value.MaxConcurrentRuns);

        List<Task> running = [];
        foreach (TenantInfo tenant in Rotate(tenants))
        {
            if (running.Count >= options.Value.MaxConcurrentRuns)
                break;

            using IServiceScope scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().SetTenant(tenant);
            DesfireEncodingService encodingService = scope.ServiceProvider.GetRequiredService<DesfireEncodingService>();
            List<HardwareDevice> devices = await encodingService.GetSchedulableEncodingDevicesAsync(ct);
            logger.DesfireEncodingWorkerTenantDevices(_workerId, tenant.Id, devices.Count);

            foreach (HardwareDevice device in devices)
            {
                if (running.Count >= options.Value.MaxConcurrentRuns)
                    break;

                Domain.BadgeJob? run = await encodingService.TryClaimNextRunForDeviceAsync(device.AgentId, device.DeviceId, _workerId, ct);
                if (run is null)
                {
                    logger.DesfireEncodingWorkerNoRunForDevice(_workerId, tenant.Id, device.AgentId, device.DeviceId);
                    continue;
                }

                logger.DesfireEncodingWorkerDispatchingRun(_workerId, tenant.Id, run.Id, device.AgentId, device.DeviceId);

                running.Add(ExecuteInTenantScopeAsync(tenant, run.Id, device.AgentId, device.DeviceId, ct));
            }
        }

        if (running.Count > 0)
            await Task.WhenAll(running);

        return running.Count;
    }

    private async Task ExecuteInTenantScopeAsync(TenantInfo tenant, Guid runId, string agentId, string deviceId, CancellationToken ct)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>().SetTenant(tenant);
        DesfireEncodingService encodingService = scope.ServiceProvider.GetRequiredService<DesfireEncodingService>();
        await encodingService.ExecuteRunAsync(runId, agentId, deviceId, requeueOnTransientFailure: true, ct);
    }

    private IEnumerable<TenantInfo> Rotate(IReadOnlyList<TenantInfo> tenants)
    {
        int start = Interlocked.Increment(ref _tenantCursor);
        for (int i = 0; i < tenants.Count; i++)
            yield return tenants[(start + i) % tenants.Count];
    }
}

internal static partial class DesfireEncodingWorkerLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding worker {WorkerId} is follower; next leadership attempt at {NextAttemptAt}")]
    public static partial void DesfireEncodingWorkerFollower(this ILogger logger, string workerId, DateTimeOffset nextAttemptAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding worker {WorkerId} acquired scheduler leadership")]
    public static partial void DesfireEncodingWorkerLeaderAcquired(this ILogger logger, string workerId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Desfire encoding worker {WorkerId} failed to release scheduler leadership cleanly")]
    public static partial void DesfireEncodingWorkerLeaderReleaseFailed(this ILogger logger, string workerId, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding worker {WorkerId} found no tenants")]
    public static partial void DesfireEncodingWorkerNoTenants(this ILogger logger, string workerId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding worker {WorkerId} dispatched {DispatchedRunCount} runs in current drain cycle")]
    public static partial void DesfireEncodingWorkerDrainedBatch(this ILogger logger, string workerId, int dispatchedRunCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding worker {WorkerId} dispatching across {TenantCount} tenants with max concurrency {MaxConcurrentRuns}")]
    public static partial void DesfireEncodingWorkerDispatching(this ILogger logger, string workerId, int tenantCount, int maxConcurrentRuns);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding worker {WorkerId} found {DeviceCount} schedulable devices for tenant {TenantId}")]
    public static partial void DesfireEncodingWorkerTenantDevices(this ILogger logger, string workerId, string tenantId, int deviceCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Desfire encoding worker {WorkerId} found no claimable run for tenant {TenantId} device {AgentId}/{DeviceId}")]
    public static partial void DesfireEncodingWorkerNoRunForDevice(this ILogger logger, string workerId, string tenantId, string agentId, string deviceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding worker {WorkerId} dispatching run {RunId} for tenant {TenantId} on device {AgentId}/{DeviceId}")]
    public static partial void DesfireEncodingWorkerDispatchingRun(this ILogger logger, string workerId, string tenantId, Guid runId, string agentId, string deviceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding worker {WorkerId} started")]
    public static partial void DesfireEncodingWorkerStarted(this ILogger logger, string workerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Desfire encoding worker {WorkerId} stopped")]
    public static partial void DesfireEncodingWorkerStopped(this ILogger logger, string workerId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Desfire encoding worker {WorkerId} failed")]
    public static partial void DesfireEncodingWorkerFailed(this ILogger logger, string workerId, Exception exception);
}
