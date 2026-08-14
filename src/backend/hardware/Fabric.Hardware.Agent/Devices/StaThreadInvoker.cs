using System.Collections.Concurrent;

namespace Fabric.Hardware.Agent.Devices;

public sealed class StaThreadInvoker : IDisposable
{
    private readonly BlockingCollection<Action> _queue = [];
    private readonly Thread _thread;

    public StaThreadInvoker()
    {
        _thread = new Thread(() =>
        {
            foreach (Action job in _queue.GetConsumingEnumerable())
                job();
        });

        _thread.IsBackground = true;
#pragma warning disable CA1416
        _thread.SetApartmentState(ApartmentState.STA);
#pragma warning restore CA1416
        _thread.Start();
    }

    public Task<T> InvokeAsync<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queue.Add(() =>
        {
            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();
        _queue.Dispose();
    }
}
