using Fabric.Hardware.Agent.Options;
using PCSC;
using PCSC.Iso7816;

namespace Fabric.Hardware.Agent.Devices;

public abstract class PcscEncoderDeviceBase(string reader, PcscEncoderImplementation implementation) : IEncoderDevice, IDisposable
{
    private readonly object _gate = new();
    private PcscSession? _session;
    private bool _disposed;

    public abstract string DeviceId { get; }

    public abstract Fabric.Hardware.Contracts.Inventory.HardwareDeviceInventoryItem GetInventoryItem();

    public abstract Task WaitForCardPresentAsync(CancellationToken cancellationToken);

    public abstract Task WaitForCardRemovalAsync(CancellationToken cancellationToken);

    protected string Reader => reader;

    public virtual Task<byte[]> ExchangeApduAsync(byte[] command, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine("Encoding..");

            PcscSession session = GetSession();
            return session.ExchangeApduAsync(command);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException("Configured PCSC reader is not available.", ex);
        }
    }

    public virtual Task PrintAsync(byte[] image, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Configured encoder does not support card printing.");

    public virtual void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            _session?.Dispose();
            _session = null;
        }
    }

    public static string[] ListReaders()
    {
        try
        {
            using ISCardContext context = ContextFactory.Instance.Establish(SCardScope.System);
            return context.GetReaders();
        }
        catch (Exception)
        {
            return [];
        }
    }

    protected bool ReaderExists() => ListReaders().Contains(reader, StringComparer.OrdinalIgnoreCase);

    protected bool IsCardPresent()
    {
        using ISCardContext context = ContextFactory.Instance.Establish(SCardScope.System);
        SCardReaderState state = context.GetReaderStatus(reader);
        return state.EventState.HasFlag(SCRState.Present);
    }

    protected void EnsureSession()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_session is not null)
                return;

            _session = PcscSession.Create(reader, implementation);
        }
    }

    protected void DisposeSession()
    {
        lock (_gate)
        {
            _session?.Dispose();
            _session = null;
        }
    }

    private PcscSession GetSession()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_session is null)
                throw new InvalidOperationException("No card is present on the encoder.");

            return _session;
        }
    }

    private sealed class PcscSession : IDisposable
    {
        private readonly IApduTransport _transport;

        private PcscSession(IApduTransport transport)
        {
            _transport = transport;
        }

        public static PcscSession Create(string reader, PcscEncoderImplementation implementation)
        {
            return implementation switch
            {
                PcscEncoderImplementation.Iso => new PcscSession(new IsoApduTransport(reader)),
                PcscEncoderImplementation.Native => new PcscSession(new NativeApduTransport(reader)),
                _ => throw new InvalidOperationException($"Unsupported encoder implementation '{implementation}'.")
            };
        }

        public Task<byte[]> ExchangeApduAsync(byte[] command) => _transport.ExchangeApduAsync(command);

        public void Dispose() => _transport.Dispose();
    }

    private interface IApduTransport : IDisposable
    {
        Task<byte[]> ExchangeApduAsync(byte[] command);
    }

    private sealed class IsoApduTransport : IApduTransport
    {
        private readonly IsoReader _isoReader;

        public IsoApduTransport(string reader)
        {
            ISCardContext context = ContextFactory.Instance.Establish(SCardScope.User);
            _isoReader = new IsoReader(context, reader, SCardShareMode.Shared, SCardProtocol.Any, false);
        }

        public Task<byte[]> ExchangeApduAsync(byte[] command)
        {
            IsoCase isoCase = command.Length > 1 ? IsoCase.Case4Short : IsoCase.Case2Short;
            CommandApdu apdu = new(isoCase, _isoReader.ActiveProtocol)
            {
                CLA = 0x90,
                INS = command[0],
                P1 = 0x00,
                P2 = 0x00,
                Le = 0x00
            };

            if (command.Length > 1)
                apdu.Data = command[1..];

            Response response = _isoReader.Transmit(apdu);
            byte[] responseData = response.HasData ? response.GetData() : [];
            byte[] responseBuffer = new byte[responseData.Length + 1];
            responseBuffer[0] = response.SW2;
            Array.Copy(responseData, 0, responseBuffer, 1, responseData.Length);
            return Task.FromResult(responseBuffer);
        }

        public void Dispose() => _isoReader.Dispose();
    }

    private sealed class NativeApduTransport : IApduTransport
    {
        private readonly ISCardContext _context;
        private readonly SCardReader _reader;

        public NativeApduTransport(string reader)
        {
            _context = ContextFactory.Instance.Establish(SCardScope.System);
            _reader = new SCardReader(_context);
            SCardError error = _reader.Connect(reader, SCardShareMode.Shared, SCardProtocol.Any);
            if (error != SCardError.Success)
                throw new InvalidOperationException($"PCSC connect failed: {error}.");
        }

        public Task<byte[]> ExchangeApduAsync(byte[] command)
        {
            byte[] receiveBuffer = new byte[256];
            int bytesReceived = receiveBuffer.Length;
            SCardError error = _reader.Transmit(command, receiveBuffer, ref bytesReceived);

            if (error != SCardError.Success)
                throw new InvalidOperationException($"PCSC transmit failed: {error}.");

            return Task.FromResult(receiveBuffer[..bytesReceived]);
        }

        public void Dispose()
        {
            _reader.Dispose();
            _context.Dispose();
        }
    }
}
