using System.Threading.Channels;
using System.Collections.Concurrent;

namespace Fabric.Server.AccessControl.Application;

public sealed record PACSSubjectConformityAuditWorkItem(string TenantId, Guid IdentityId, Guid AccessControlSystemId);

public sealed class PACSSubjectConformityAuditTrigger
{
    private readonly ConcurrentDictionary<string, byte> _activeKeys = new();
    private readonly Channel<PACSSubjectConformityAuditWorkItem> _channel = Channel.CreateUnbounded<PACSSubjectConformityAuditWorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public async ValueTask<bool> EnqueueAsync(PACSSubjectConformityAuditWorkItem workItem, CancellationToken cancellationToken = default)
    {
        string key = GetKey(workItem);
        if (!_activeKeys.TryAdd(key, 0))
            return false;

        await _channel.Writer.WriteAsync(workItem, cancellationToken);
        return true;
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) =>
        _channel.Reader.WaitToReadAsync(cancellationToken);

    public bool TryRead(out PACSSubjectConformityAuditWorkItem? workItem) =>
        _channel.Reader.TryRead(out workItem);

    public void Complete(PACSSubjectConformityAuditWorkItem workItem) =>
        _activeKeys.TryRemove(GetKey(workItem), out _);

    private static string GetKey(PACSSubjectConformityAuditWorkItem workItem) =>
        $"{workItem.TenantId}:{workItem.IdentityId}:{workItem.AccessControlSystemId}";
}
