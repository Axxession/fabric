namespace Fabric.Server.Sagas.ContractorJobs;

public sealed class ContractorJobPackageRule
{
    public Guid Id { get; set; }
    public Guid JobTypeId { get; set; }
    public Guid PackageId { get; set; }
    public Guid? LocationId { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class ContractorAssignmentAutomationMailbox
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public string Reason { get; set; } = null!;
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? LastRetryAt { get; set; }
    public string? LastKnownError { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }

    public void ReleaseLease()
    {
        LeaseOwner = null;
        LeaseUntil = null;
    }

    public void MarkFailed(string error, DateTimeOffset retryAt, DateTimeOffset now)
    {
        LastRetryAt = now;
        LastKnownError = error;
        AttemptCount++;
        ScheduledFor = retryAt;
        UpdatedAt = now;
        ReleaseLease();
    }
}

public sealed record ContractorAssignmentAutomationWorkItem(string TenantId, Guid AssignmentId, string Reason);
