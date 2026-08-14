using Fabric.Hardware.Agent;
using Fabric.Hardware.Agent.Devices;
using Fabric.Hardware.Agent.Gateway;
using Fabric.Hardware.Agent.Options;
using Fabric.Hardware.RfidEas;
using Fabric.Hardware.RfidEas.Infrastructure;

if (ShouldListEncoders(args))
{
    foreach (string reader in PcscEncoderDeviceBase.ListReaders())
        Console.WriteLine(reader);

    return;
}

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
IReadOnlyList<EncoderOptions> encoderOptions = EncoderOptionsParser.Parse(builder.Configuration);

builder.Services.AddTransient<TimeProvider>(_ => TimeProvider.System);

builder.Services.AddWindowsService(options => options.ServiceName = builder.Configuration.GetValue<string>("HardwareAgent:ServiceName") ?? "Fabric Hardware Agent");
builder.Services.AddSystemd();

builder.Services.AddOptions<HardwareAgentOptions>()
    .Bind(builder.Configuration.GetSection(HardwareAgentOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IReadOnlyList<EncoderOptions>>(encoderOptions);
builder.Services.AddSingleton<IValidateOptions<HardwareAgentOptions>>(_ => new HardwareAgentOptionsValidator(encoderOptions));

builder.Services.AddHttpClient<HardwareGatewayClient>((serviceProvider, client) =>
{
    HardwareAgentOptions options = serviceProvider.GetRequiredService<IOptions<HardwareAgentOptions>>().Value;
    client.BaseAddress = options.ServerBaseUrl;
    client.DefaultRequestHeaders.Add(HardwareGatewayHeaders.AgentId, options.AgentId);
    client.DefaultRequestHeaders.Add(HardwareGatewayHeaders.AgentKey, options.ApiKey);
});

builder.Services.AddSingleton<IReadOnlyList<IQrReaderDevice>>(serviceProvider =>
{
    HardwareAgentOptions options = serviceProvider.GetRequiredService<IOptions<HardwareAgentOptions>>().Value;
    ILogger<SerialQrReaderDevice> logger = serviceProvider.GetRequiredService<ILogger<SerialQrReaderDevice>>();
    return options.QrReaders.Select(qrReaderOptions => new SerialQrReaderDevice(qrReaderOptions, logger)).ToArray();
});
builder.Services.AddSingleton<IReadOnlyList<IDispenserDevice>>(serviceProvider =>
{
    HardwareAgentOptions options = serviceProvider.GetRequiredService<IOptions<HardwareAgentOptions>>().Value;
    ILogger<SerialDispenserDevice> logger = serviceProvider.GetRequiredService<ILogger<SerialDispenserDevice>>();
    ILogger<Fabric.Hardware.Dispenser.DispenserSerialPort> dispenserLogger = serviceProvider.GetRequiredService<ILogger<Fabric.Hardware.Dispenser.DispenserSerialPort>>();
    return options.Dispensers.Select(dispenserOptions => new SerialDispenserDevice(dispenserOptions, dispenserLogger, logger)).ToArray();
});
builder.Services.AddSingleton<IReadOnlyList<ICollectorDevice>>(serviceProvider =>
{
    HardwareAgentOptions options = serviceProvider.GetRequiredService<IOptions<HardwareAgentOptions>>().Value;
    ILogger<SerialCollectorDevice> logger = serviceProvider.GetRequiredService<ILogger<SerialCollectorDevice>>();
    ILogger<Fabric.Hardware.Collector.Collector> collectorLogger = serviceProvider.GetRequiredService<ILogger<Fabric.Hardware.Collector.Collector>>();
    ILogger<Fabric.Hardware.Collector.CollectorComPort> collectorComPortLogger = serviceProvider.GetRequiredService<ILogger<Fabric.Hardware.Collector.CollectorComPort>>();
    return options.Collectors.Select(collectorOptions => new SerialCollectorDevice(collectorOptions, collectorLogger, collectorComPortLogger, logger)).ToArray();
});
builder.Services.AddSingleton<IReadOnlyList<IRfidReaderDevice>>(serviceProvider =>
{
    HardwareAgentOptions options = serviceProvider.GetRequiredService<IOptions<HardwareAgentOptions>>().Value;
    if (options.RfidEas.Readers.Length == 0)
        return Array.Empty<IRfidReaderDevice>();

    ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    ILogger<EasRfidReaderDevice> logger = serviceProvider.GetRequiredService<ILogger<EasRfidReaderDevice>>();
    Lazy<RfidEasReader> reader = new(() => new RfidReaderFactory().Create(
        new RfidReaderSettings
        {
            CardFormat = options.RfidEas.CardFormat,
            Transformer = options.RfidEas.Transformer,
            DelayAfterRead = options.RfidEas.DelayAfterRead,
            PollingDelay = options.RfidEas.PollingDelay
        },
        loggerFactory));

    return options.RfidEas.Readers
        .Select<RfidEasReaderOptions, IRfidReaderDevice>(readerOptions => new EasRfidReaderDevice(readerOptions, () => reader.Value, logger))
        .ToArray();
});
builder.Services.AddSingleton<IReadOnlyList<IEncoderDevice>>(serviceProvider =>
{
    IReadOnlyList<EncoderOptions> options = serviceProvider.GetRequiredService<IReadOnlyList<EncoderOptions>>();
    ILogger<Fabric.Hardware.Dispenser.DispenserSerialPort> dispenserLogger = serviceProvider.GetRequiredService<ILogger<Fabric.Hardware.Dispenser.DispenserSerialPort>>();
    ILogger<HumanAssistedPcscEncoderDevice> humanAssistedLogger = serviceProvider.GetRequiredService<ILogger<HumanAssistedPcscEncoderDevice>>();
    ILogger<DispenserEncoderDevice> dispenserEncoderLogger = serviceProvider.GetRequiredService<ILogger<DispenserEncoderDevice>>();
    ILogger<EvolisEncoderDevice> evolisEncoderLogger = serviceProvider.GetRequiredService<ILogger<EvolisEncoderDevice>>();
    ILogger<FargoEncoderDevice> fargoEncoderLogger = serviceProvider.GetRequiredService<ILogger<FargoEncoderDevice>>();
    return options.Select(encoder => (IEncoderDevice)(encoder switch
    {
        HumanAssistedEncoderOptions humanAssistedEncoder => new HumanAssistedPcscEncoderDevice(humanAssistedEncoder, humanAssistedLogger),
        DispenserEncoderOptions dispenserEncoder => new DispenserEncoderDevice(dispenserEncoder, dispenserLogger, dispenserEncoderLogger),
        EvolisEncoderOptions evolisEncoder => new EvolisEncoderDevice(evolisEncoder, evolisEncoderLogger),
        FargoEncoderOptions fargoEncoder => new FargoEncoderDevice(fargoEncoder, fargoEncoderLogger),
        _ => throw new InvalidOperationException($"Unsupported encoder option type '{encoder.GetType().Name}'.")
    })).ToArray();
});
builder.Services.AddHostedService<HeartbeatWorker>();
builder.Services.AddHostedService<InventoryWorker>();
builder.Services.AddHostedService<QrEventWorker>();
builder.Services.AddHostedService<CommandWorker>();

await builder.Build().RunAsync();

static bool ShouldListEncoders(string[] args) =>
    args.Length switch
    {
        1 => string.Equals(args[0], "list-encoders", StringComparison.OrdinalIgnoreCase),
        >= 2 => string.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase)
            && string.Equals(args[1], "encoders", StringComparison.OrdinalIgnoreCase),
        _ => false
    };
