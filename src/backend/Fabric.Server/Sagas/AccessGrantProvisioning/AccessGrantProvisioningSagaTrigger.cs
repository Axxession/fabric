using System.Threading.Channels;

namespace Fabric.Server.Sagas.AccessGrantProvisioning;

public sealed record AccessGrantProvisioningSagaEventWorkItem(string TenantId, Guid EventId);

public sealed class AccessGrantProvisioningSagaTrigger
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropWrite,
    });

    public void Notify() => _channel.Writer.TryWrite(true);

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) =>
        _channel.Reader.WaitToReadAsync(cancellationToken);

    public bool TryRead() => _channel.Reader.TryRead(out _);
}
