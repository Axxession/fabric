using Fabric.Server.Desfire.Domain;
using Fabric.Server.Desfire.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Desfire.Application;

public sealed class DesfireDeviceLeaseStore(DesfireDbContext db, TimeProvider timeProvider)
{
    public async Task<DesfireDeviceLeaseResult> TryAcquireAsync(string agentId, string deviceId, Guid encodingRunId, TimeSpan leaseDuration, CancellationToken ct)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        string normalizedAgentId = agentId.Trim().ToLowerInvariant();
        string normalizedDeviceId = deviceId.Trim().ToLowerInvariant();
        string lockKey = $"desfire:device-lease:{db.TenantId}:{normalizedAgentId}:{normalizedDeviceId}";

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({lockKey}))", ct);

        bool busy = await db.DeviceLeases.AnyAsync(lease =>
            lease.AgentId == normalizedAgentId
            && lease.DeviceId == normalizedDeviceId
            && lease.ReleasedAt == null
            && lease.ExpiresAt > now,
            ct);

        if (busy)
            return DesfireDeviceLeaseResult.Busy;

        DesfireDeviceLease lease = DesfireDeviceLease.Create(normalizedAgentId, normalizedDeviceId, encodingRunId, now, now.Add(leaseDuration));
        db.DeviceLeases.Add(lease);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return DesfireDeviceLeaseResult.Acquired;
    }

    public async Task ReleaseAsync(string agentId, string deviceId, Guid encodingRunId, CancellationToken ct)
    {
        string normalizedAgentId = agentId.Trim().ToLowerInvariant();
        string normalizedDeviceId = deviceId.Trim().ToLowerInvariant();
        DesfireDeviceLease? lease = await db.DeviceLeases
            .Where(candidate => candidate.AgentId == normalizedAgentId
                && candidate.DeviceId == normalizedDeviceId
                && candidate.BadgeJobId == encodingRunId
                && candidate.ReleasedAt == null)
            .OrderByDescending(candidate => candidate.AcquiredAt)
            .FirstOrDefaultAsync(ct);

        if (lease is null)
            return;

        lease.Release(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }
}

public enum DesfireDeviceLeaseResult
{
    Acquired,
    Busy
}
