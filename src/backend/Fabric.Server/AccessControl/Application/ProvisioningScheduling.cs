using Fabric.Server.AccessControl.Domain;

namespace Fabric.Server.AccessControl.Application;

internal static class ProvisioningScheduling
{
    public static DateTimeOffset GetScheduledFor(ProvisioningTiming provisioningTiming, DateTimeOffset validFrom, DateTimeOffset now) =>
        provisioningTiming switch
        {
            ProvisioningTiming.AtValidFrom =>
                StartOfUtcDay(validFrom) <= now ? now : StartOfUtcDay(validFrom),
            _ => now,
        };

    private static DateTimeOffset StartOfUtcDay(DateTimeOffset value) =>
        new(DateOnly.FromDateTime(value.UtcDateTime).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
}
