using Evolis;
using Fabric.Hardware.Agent.Options;

namespace Fabric.Hardware.Agent.Devices;

public sealed class EvolisEncoderDevice(EvolisEncoderOptions options, ILogger<EvolisEncoderDevice> logger)
    : BadgePrinterEncoderDeviceBase<EvolisEncoderOptions>(options, logger)
{
    private readonly object _gate = new();
    private Connection? _connection;

    protected override string Driver => "evolis-printer";

    protected override bool IsTransportDetected()
    {
        try
        {
            Connection connection = new(Options.PrinterName);
            connection.GetState(out State.MajorState _, out State.MinorState _);
            connection.Close();
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected override Task LoadCardAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Connection connection = EnsureConnection();
        if (!HasReader)
            return Task.CompletedTask;

        CardPos cardPosition = ParsePosition(Options.Station);
        bool result = connection.SetCardPos(cardPosition);
        if (!result)
            throw new InvalidOperationException($"Could not dock Evolis card to {cardPosition}: {connection.GetLastError()}");

        return Task.CompletedTask;
    }

    protected override Task PrintCardAsync(byte[] image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Connection connection = EnsureConnection();
        PrintSession session = new(ref connection);
        if (!session.Init())
            throw new InvalidOperationException($"Could not initialize Evolis print session: {connection.GetLastError()}");

        if (!session.SetImage(CardFace.FRONT, image))
            throw new InvalidOperationException($"Could not set Evolis print image: {connection.GetLastError()}");

        ReturnCode result = session.Print();
        if (result != ReturnCode.OK)
            throw new InvalidOperationException($"Evolis print failed: {result}");

        return Task.CompletedTask;
    }

    protected override Task EjectCardAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Connection connection = EnsureConnection();
        bool result = connection.EjectCard() || connection.RejectCard();
        if (!result)
            throw new InvalidOperationException($"Could not eject Evolis card: {connection.GetLastError()}");

        DisposeConnection();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        DisposeConnection();
        base.Dispose();
    }

    private Connection EnsureConnection()
    {
        lock (_gate)
        {
            if (_connection is not null)
                return _connection;

            var connection = new Connection(Options.PrinterName, OpenMode.AUTO);
            if (!connection.Reserve(500))
            {
                ReturnCode lastError = connection.GetLastError();
                connection.Close();
                throw new InvalidOperationException($"Could not reserve Evolis printer '{Options.PrinterName}': {lastError}");
            }

            _connection = connection;
            return _connection;
        }
    }

    private void DisposeConnection()
    {
        lock (_gate)
        {
            _connection?.Close();
            _connection = null;
        }
    }

    private static CardPos ParsePosition(string station) => station.Trim().ToLowerInvariant() switch
    {
        "contact" or "smartcard" => CardPos.CONTACT,
        "contactless" or "mifare" or "iclass" or "proximity" => CardPos.CONTACTLESS,
        "magnetic" => throw new InvalidOperationException("Magnetic station is not supported for APDU encoding."),
        _ => throw new InvalidOperationException($"Unsupported Evolis station '{station}'.")
    };
}
