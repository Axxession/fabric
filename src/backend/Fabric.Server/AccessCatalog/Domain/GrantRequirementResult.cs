using Fabric.Server.Requirements.Domain;

namespace Fabric.Server.AccessCatalog.Domain;

public sealed class GrantRequirementResult
{
    private GrantRequirementResult() { }

    public Guid Id { get; private set; }
    public Guid AccessGrantId { get; private set; }
    public Guid RequirementDefinitionId { get; private set; }
    public RequirementResultStatus Status { get; private set; }
    public RequirementEvidenceKind? EvidenceKind { get; private set; }
    public string? EvidenceReference { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTimeOffset? ValidUntil { get; private set; }
    public DateTimeOffset LastEvaluatedAt { get; private set; }

    public static GrantRequirementResult Create(
        Guid accessGrantId,
        Guid requirementDefinitionId,
        RequirementResultStatus status,
        RequirementEvidenceKind? evidenceKind,
        string? evidenceReference,
        string reason,
        DateTimeOffset? validUntil,
        DateTimeOffset lastEvaluatedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccessGrantId = accessGrantId,
            RequirementDefinitionId = requirementDefinitionId,
            Status = status,
            EvidenceKind = evidenceKind,
            EvidenceReference = evidenceReference,
            Reason = reason,
            ValidUntil = validUntil,
            LastEvaluatedAt = lastEvaluatedAt
        };

    public void Update(
        RequirementResultStatus status,
        RequirementEvidenceKind? evidenceKind,
        string? evidenceReference,
        string reason,
        DateTimeOffset? validUntil,
        DateTimeOffset lastEvaluatedAt)
    {
        Status = status;
        EvidenceKind = evidenceKind;
        EvidenceReference = evidenceReference;
        Reason = reason;
        ValidUntil = validUntil;
        LastEvaluatedAt = lastEvaluatedAt;
    }
}
