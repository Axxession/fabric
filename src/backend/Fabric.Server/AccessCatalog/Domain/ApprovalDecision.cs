namespace Fabric.Server.AccessCatalog.Domain;

public sealed class ApprovalDecision
{
    private ApprovalDecision() { }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid ApprovalRequirementId { get; private set; }
    public Guid ApproverIdentityId { get; private set; }
    public ApprovalDecisionRole Role { get; private set; }
    public ApprovalDecisionKind DecisionKind { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset DecidedAt { get; private set; }

    public static ApprovalDecision Create(
        Guid requestId,
        Guid approvalRequirementId,
        Guid approverIdentityId,
        ApprovalDecisionRole role,
        ApprovalDecisionKind decisionKind,
        string? note,
        DateTimeOffset decidedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApprovalRequirementId = approvalRequirementId,
            ApproverIdentityId = approverIdentityId,
            Role = role,
            DecisionKind = decisionKind,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            DecidedAt = decidedAt
        };
}
