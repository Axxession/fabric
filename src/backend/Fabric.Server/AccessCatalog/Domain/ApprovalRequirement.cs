namespace Fabric.Server.AccessCatalog.Domain;

public sealed class ApprovalRequirement
{
    private ApprovalRequirement() { }

    public Guid Id { get; private set; }
    public Guid ApprovalFlowId { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid AccessItemId { get; private set; }
    public Guid LocationId { get; private set; }
    public ApprovalRequirementType Type { get; private set; }
    public ApprovalDecisionRole Role { get; private set; }
    public Guid? ApprovalGroupId { get; private set; }
    public Guid? RequiredApproverIdentityId { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public string? SystemApprovalReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static ApprovalRequirement CreateDestination(
        Guid approvalFlowId,
        Guid requestId,
        Guid accessItemId,
        Guid locationId,
        Guid approvalGroupId,
        ApprovalDecisionRole role,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApprovalFlowId = approvalFlowId,
            RequestId = requestId,
            AccessItemId = accessItemId,
            LocationId = locationId,
            Type = ApprovalRequirementType.Destination,
            Role = role,
            ApprovalGroupId = approvalGroupId,
            Status = ApprovalStatus.Pending,
            CreatedAt = createdAt
        };

    public static ApprovalRequirement CreateOrganizational(
        Guid approvalFlowId,
        Guid requestId,
        Guid accessItemId,
        Guid locationId,
        Guid requiredApproverIdentityId,
        ApprovalDecisionRole role,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApprovalFlowId = approvalFlowId,
            RequestId = requestId,
            AccessItemId = accessItemId,
            LocationId = locationId,
            Type = ApprovalRequirementType.Organizational,
            Role = role,
            RequiredApproverIdentityId = requiredApproverIdentityId,
            Status = ApprovalStatus.Pending,
            CreatedAt = createdAt
        };

    public static ApprovalRequirement CreateSystemApproved(
        Guid approvalFlowId,
        Guid requestId,
        Guid accessItemId,
        Guid locationId,
        ApprovalRequirementType type,
        ApprovalDecisionRole role,
        Guid? approvalGroupId,
        string reason,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApprovalFlowId = approvalFlowId,
            RequestId = requestId,
            AccessItemId = accessItemId,
            LocationId = locationId,
            Type = type,
            Role = role,
            ApprovalGroupId = approvalGroupId,
            Status = ApprovalStatus.SystemApproved,
            SystemApprovalReason = reason,
            CreatedAt = createdAt,
            CompletedAt = createdAt
        };

    public void MarkApproved(DateTimeOffset completedAt)
    {
        Status = ApprovalStatus.Approved;
        CompletedAt = completedAt;
        SystemApprovalReason = null;
    }

    public void MarkRejected(DateTimeOffset completedAt)
    {
        Status = ApprovalStatus.Rejected;
        CompletedAt = completedAt;
        SystemApprovalReason = null;
    }
}
