using System.Threading.Channels;

namespace Fabric.Server.AccessControl.Application;

public sealed record PACSProvisioningReconciliationWorkItem(string TenantId, Guid IdentityId, Guid AccessControlSystemId);

public sealed class PACSProvisioningReconciliationTrigger
{
    private readonly Channel<PACSProvisioningReconciliationWorkItem> _channel = Channel.CreateUnbounded<PACSProvisioningReconciliationWorkItem>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    public ValueTask EnqueueAsync(PACSProvisioningReconciliationWorkItem workItem, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(workItem, cancellationToken);

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) =>
        _channel.Reader.WaitToReadAsync(cancellationToken);

    public bool TryRead(out PACSProvisioningReconciliationWorkItem? workItem) =>
        _channel.Reader.TryRead(out workItem);
}
