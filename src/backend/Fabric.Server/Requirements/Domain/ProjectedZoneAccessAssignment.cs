namespace Fabric.Server.Requirements.Domain;

public sealed class ProjectedZoneAccessAssignment
{
    private ProjectedZoneAccessAssignment() { }

    public Guid Id { get; private set; }
    public Guid ZoneComplianceId { get; private set; }
    public Guid EnforcementZoneAccessPolicyId { get; private set; }
    public Guid AccessItemId { get; private set; }
    public Guid LocationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ProjectedZoneAccessAssignment Create(
        Guid zoneComplianceId,
        Guid enforcementZoneAccessPolicyId,
        Guid accessItemId,
        Guid locationId,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            ZoneComplianceId = zoneComplianceId,
            EnforcementZoneAccessPolicyId = enforcementZoneAccessPolicyId,
            AccessItemId = accessItemId,
            LocationId = locationId,
            CreatedAt = now
        };
}
