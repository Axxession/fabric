namespace Fabric.Server.AccessCatalog.Domain;

public sealed class ApprovalFlow
{
    private ApprovalFlow() { }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid AccessItemId { get; private set; }
    public Guid SiteId { get; private set; }
    public ApprovalFlowStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static ApprovalFlow Create(
        Guid requestId,
        Guid packageId,
        Guid accessItemId,
        Guid siteId,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            PackageId = packageId,
            AccessItemId = accessItemId,
            SiteId = siteId,
            Status = ApprovalFlowStatus.InProgress,
            CreatedAt = createdAt
        };

    public void MarkApproved(DateTimeOffset completedAt)
    {
        Status = ApprovalFlowStatus.Approved;
        CompletedAt = completedAt;
    }

    public void MarkRejected(DateTimeOffset completedAt)
    {
        Status = ApprovalFlowStatus.Rejected;
        CompletedAt = completedAt;
    }

    public void MarkSystemApproved(DateTimeOffset completedAt)
    {
        Status = ApprovalFlowStatus.SystemApproved;
        CompletedAt = completedAt;
    }

    public void MarkExpired(DateTimeOffset completedAt)
    {
        Status = ApprovalFlowStatus.Expired;
        CompletedAt = completedAt;
    }
}
