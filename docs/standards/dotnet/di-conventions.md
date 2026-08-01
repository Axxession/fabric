# DI Conventions

## Service Lifetimes

| Lifetime | When to use | Example |
|---|---|---|
| **Scoped** | Default for most services — one instance per HTTP request | DbContext, repositories, domain services |
| **Singleton** | Stateless services, caches, configuration wrappers | ILogger<T>, HttpClient factory, memory cache |
| **Transient** | Lightweight stateless services with no shared state | Utility helpers, converters |

**Rule:** Default to Scoped. Only use Singleton when you can prove the service is truly stateless or the shared state is explicitly intended. Never register a Scoped service as Singleton — it will capture the first request's scope and leak it to all subsequent requests.

## Registration Patterns

Register services by interface in dedicated extension methods per module:

```csharp
public static class SiteServiceRegistration
{
    public static IServiceCollection AddSiteServices(this IServiceCollection services)
    {
        services.AddScoped<ISiteRepository, SiteRepository>();
        services.AddScoped<ISiteService, SiteService>();
        return services;
    }
}
```

Called in `Program.cs`:

```csharp
builder.Services
    .AddSiteServices()
    .AddIdentityServices()
    .AddPolicyServices();
```

**Rules:**
- One extension method per vertical slice or module.
- Extension methods live in the server project, co-located with the services they register.
- Do not use assembly scanning for DI registration — explicit registration is easier to debug and reason about.

## Open/Closed Registration

When a service has a single implementation, register it as `AddScoped<TInterface, TImplementation>`. When there are multiple implementations, register each variant explicitly or use a factory:

```csharp
services.AddScoped<IReaderProtocol>(sp =>
{
    var config = sp.GetRequiredService<IOptions<ReaderOptions>>();
    return config.Value.Protocol switch
    {
        "OSDP" => new OsdpReaderProtocol(),
        "Wiegand" => new WiegandReaderProtocol(),
        _ => throw new InvalidOperationException($"Unknown protocol: {config.Value.Protocol}")
    };
});
```
