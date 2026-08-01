using Fabric.Server.Core;

namespace Fabric.Server.AccessControl.Domain;

public sealed class AccessControlSystem
{
    private AccessControlSystem() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public AccessControlProviderKind ProviderKind { get; private set; }
    public AccessControlSystemStatus Status { get; private set; }
    public UnipassSystemConfig? UnipassConfig { get; private set; }

    public static Result<AccessControlSystem, AccessControlErrors> CreateUnipass(string name, UnipassSystemConfig config)
    {
        if (!config.IsValid())
            return Result.Failure<AccessControlSystem, AccessControlErrors>(AccessControlErrors.ConfigInvalid);

        return Result.Success<AccessControlSystem, AccessControlErrors>(new AccessControlSystem
        {
            Id = Guid.NewGuid(),
            Name = name,
            ProviderKind = AccessControlProviderKind.Unipass,
            Status = AccessControlSystemStatus.Active,
            UnipassConfig = config
        });
    }

    public void Rename(string name) => Name = name;

    public Result<AccessControlErrors> UpdateUnipassConfig(UnipassSystemConfig config)
    {
        if (ProviderKind != AccessControlProviderKind.Unipass)
            return Result.Failure(AccessControlErrors.SystemProviderNotSupported);

        if (!config.IsValid())
            return Result.Failure(AccessControlErrors.ConfigInvalid);

        UnipassConfig = config;
        return Result.Success<AccessControlErrors>();
    }

    public void SetStatus(AccessControlSystemStatus status) => Status = status;
}

public sealed record UnipassSystemConfig
{
    private UnipassSystemConfig() { }

    private UnipassSystemConfig(string endpoint, bool sslValidation, string username, string password)
    {
        Endpoint = endpoint;
        SslValidation = sslValidation;
        Username = username;
        Password = password;
    }

    public string Endpoint { get; private init; } = null!;
    public bool SslValidation { get; private init; }
    public string Username { get; private init; } = null!;
    public string Password { get; private init; } = null!;

    public static Result<UnipassSystemConfig, AccessControlErrors> Create(
        string endpoint,
        bool sslValidation,
        string username,
        string password)
    {
        UnipassSystemConfig config = new(endpoint, sslValidation, username, password);

        return config.IsValid()
            ? Result.Success<UnipassSystemConfig, AccessControlErrors>(config)
            : Result.Failure<UnipassSystemConfig, AccessControlErrors>(AccessControlErrors.ConfigInvalid);
    }

    internal bool IsValid() =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password);
}
