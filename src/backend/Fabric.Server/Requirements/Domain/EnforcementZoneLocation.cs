namespace Fabric.Server.Requirements.Domain;

public sealed class EnforcementZoneLocation
{
    private EnforcementZoneLocation() { }

    public Guid Id { get; private set; }
    public Guid EnforcementZoneId { get; private set; }
    public Guid LocationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static EnforcementZoneLocation Create(Guid enforcementZoneId, Guid locationId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EnforcementZoneId = enforcementZoneId,
            LocationId = locationId,
            CreatedAt = now
        };
}
