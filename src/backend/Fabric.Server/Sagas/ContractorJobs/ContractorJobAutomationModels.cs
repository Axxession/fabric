namespace Fabric.Server.Sagas.ContractorJobs;

public sealed class ContractorJobPackageRule
{
    public Guid Id { get; set; }
    public Guid JobTypeId { get; set; }
    public Guid PackageId { get; set; }
    public Guid? LocationId { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class ContractorJobOnboardingReconciliation
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

    public static ContractorJobOnboardingReconciliation Create(Guid assignmentId, string reason, DateTimeOffset scheduledFor, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId,
            Reason = reason,
            ScheduledFor = scheduledFor,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void RescheduleNow(string reason, DateTimeOffset now)
    {
        Reason = reason;
        ScheduledFor = now;
        LastRetryAt = null;
        LastKnownError = null;
        AttemptCount = 0;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset retryAt, DateTimeOffset now)
    {
        LastRetryAt = now;
        LastKnownError = error;
        AttemptCount++;
        ScheduledFor = retryAt;
        UpdatedAt = now;
    }
}

public sealed class ContractorJobAccessAutomationReconciliation
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

    public static ContractorJobAccessAutomationReconciliation Create(Guid assignmentId, string reason, DateTimeOffset scheduledFor, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            AssignmentId = assignmentId,
            Reason = reason,
            ScheduledFor = scheduledFor,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void RescheduleNow(string reason, DateTimeOffset now)
    {
        Reason = reason;
        ScheduledFor = now;
        LastRetryAt = null;
        LastKnownError = null;
        AttemptCount = 0;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset retryAt, DateTimeOffset now)
    {
        LastRetryAt = now;
        LastKnownError = error;
        AttemptCount++;
        ScheduledFor = retryAt;
        UpdatedAt = now;
    }
}

public sealed record ContractorJobOnboardingWorkItem(string TenantId, Guid AssignmentId, string Reason);

public sealed record ContractorJobAccessAutomationWorkItem(string TenantId, Guid AssignmentId, string Reason);
