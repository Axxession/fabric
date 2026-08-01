using System.Reflection;

namespace Fabric.Server.Infrastructure;

public interface IApplicationVersionProvider
{
    string GetVersion();
}

public sealed class ApplicationVersionProvider : IApplicationVersionProvider
{
    private readonly string _version;

    public ApplicationVersionProvider()
    {
        Assembly assembly = typeof(Program).Assembly;
        string? informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        string? fileVersion = assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()
            ?.Version;

        _version = informationalVersion
            ?? fileVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }

    public string GetVersion() => _version;
}
