namespace Fabric.Server.Requirements.Domain;

public sealed class ZoneComplianceRequirementResult
{
    private ZoneComplianceRequirementResult() { }

    public Guid Id { get; internal set; }
    public Guid ZoneComplianceId { get; internal set; }
    public Guid RequirementDefinitionId { get; internal set; }
    public RequirementResultStatus Status { get; internal set; }
    public RequirementEvidenceKind? EvidenceKind { get; internal set; }
    public string? EvidenceReference { get; internal set; }
    public string Reason { get; internal set; } = null!;
    public DateTimeOffset? ValidUntil { get; internal set; }

    public static ZoneComplianceRequirementResult Create(
        Guid requirementDefinitionId,
        RequirementResultStatus status,
        RequirementEvidenceKind? evidenceKind,
        string? evidenceReference,
        string reason,
        DateTimeOffset? validUntil) =>
        new()
        {
            Id = Guid.NewGuid(),
            RequirementDefinitionId = requirementDefinitionId,
            Status = status,
            EvidenceKind = evidenceKind,
            EvidenceReference = string.IsNullOrWhiteSpace(evidenceReference) ? null : evidenceReference.Trim(),
            Reason = reason,
            ValidUntil = validUntil
        };
}
