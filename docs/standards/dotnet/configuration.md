# Configuration

## Options Pattern

Use `IOptions<T>` (or `IOptionsSnapshot<T>` for reloadable config) for all strongly-typed configuration. Never access `IConfiguration` directly outside the registration boundary.

```csharp
public class ReaderOptions
{
    public const string SectionName = "Reader";

    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
    public string DefaultProtocol { get; init; } = "OSDP";
}
```

Register in `Program.cs`:

```csharp
builder.Services.Configure<ReaderOptions>(
    builder.Configuration.GetSection(ReaderOptions.SectionName));
```

Inject where needed:

```csharp
public class ReaderService(IOptions<ReaderOptions> options)
{
    private readonly ReaderOptions _options = options.Value;
}
```

**Rules:**
- Every settings section gets a dedicated options class.
- The class is immutable (`init`-only properties).
- Provide sensible defaults on all properties.
- The `SectionName` constant matches the `appsettings.json` path.

## appsettings.json Structure

```json
{
  "Reader": {
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "DefaultProtocol": "OSDP"
  },
  "ConnectionStrings": {
    "ServiceDb": "Host=localhost;Database=service"
  },
  "Cors": {
    "Origins": ["https://app.example.com"]
  }
}
```

## Environment-Specific Overrides

Use `appsettings.{Environment}.json` for environment-specific values:

```
appsettings.json              — shared defaults
appsettings.Development.json  — local overrides
appsettings.Production.json   — production overrides
```

Secrets and connection strings that differ per developer go in `appsettings.Development.json` or user secrets (`dotnet user-secrets`). Production secrets belong in environment variables or a vault — never committed to the repository.

## Validation on Startup

Validate configuration at startup to fail fast:

```csharp
builder.Services.AddOptions<ReaderOptions>()
    .Bind(builder.Configuration.GetSection(ReaderOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Use `System.ComponentModel.DataAnnotations` attributes on options classes for validation rules.
