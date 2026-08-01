# Background Jobs

## Hosted Services

Use `BackgroundService` for long-running background work. Prefer it over raw `IHostedService` — `BackgroundService` provides a structured `ExecuteAsync` pattern with cancellation support.

```csharp
public sealed class KeyGroupCleanupJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeyGroupCleanupJob> _logger;

    public KeyGroupCleanupJob(IServiceScopeFactory scopeFactory, ILogger<KeyGroupCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.JobStarted(nameof(KeyGroupCleanupJob));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();

                var expired = await db.KeyGroups
                    .Where(k => k.ExpiresAt < DateTime.UtcNow)
                    .ExecuteDeleteAsync(stoppingToken);

                if (expired > 0)
                    _logger.CleanupCompleted(expired);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.JobFailed(nameof(KeyGroupCleanupJob), ex);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.JobStopped(nameof(KeyGroupCleanupJob));
    }
}
```

**Key patterns:**
- Inject `IServiceScopeFactory` (not scoped services directly) — `BackgroundService` is a singleton.
- Wrap the loop body in try/catch so a single failure does not kill the job.
- Respect `stoppingToken` — check it and break on `OperationCanceledException`.
- Log start, stop, and errors using `[LoggerMessage]` source generation.

## When to Use Hosted Services

| Scenario | Use |
|---|---|
| Periodic cleanup | `BackgroundService` with `Task.Delay` |
| Message queue consumer | `BackgroundService` with channel |
| Startup initialization | `IHostedService` (single-run) |
| Webhook delivery | `BackgroundService` with retry queue |

Do **not** use hosted services for:
- Request-scoped work that should be synchronous.
- One-off tasks that can run as part of the request pipeline.
- Work that belongs in a separate worker process (use a dedicated worker project instead).

## Graceful Shutdown

The runtime calls `StopAsync` on all hosted services when the application is shutting down. Ensure your `ExecuteAsync` exits promptly when `stoppingToken` is cancelled. Do not block shutdown with long-running cleanup — defer non-critical cleanup to the next startup.
