namespace Fabric.Server.Reception.Application;

public sealed class ReceptionKioskSessionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReceptionKioskSessionCleanupService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(12);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(PollInterval, timeProvider);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                ReceptionKioskSessionService service = scope.ServiceProvider.GetRequiredService<ReceptionKioskSessionService>();
                int deleted = await service.DeleteExpiredSessionsAsync(stoppingToken);
                if (deleted > 0)
                    ReceptionKioskSessionCleanupLog.DeletedExpiredSessions(logger, deleted);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                ReceptionKioskSessionCleanupLog.DeleteExpiredSessionsFailed(logger, exception);
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }
}

internal static partial class ReceptionKioskSessionCleanupLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {DeletedCount} expired reception kiosk sessions")]
    public static partial void DeletedExpiredSessions(ILogger logger, int deletedCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed deleting expired reception kiosk sessions")]
    public static partial void DeleteExpiredSessionsFailed(ILogger logger, Exception exception);
}
