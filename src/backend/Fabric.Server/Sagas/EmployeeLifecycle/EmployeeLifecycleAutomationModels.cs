using Fabric.Server.Employees.Domain;

namespace Fabric.Server.Sagas.EmployeeLifecycle;

public sealed class OrganizationalUnitPackageRule
{
    public Guid Id { get; set; }
    public Guid OrganizationUnitId { get; set; }
    public Guid PackageId { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class PersonaPackageRule
{
    public Guid Id { get; set; }
    public Guid PersonaId { get; set; }
    public Guid PackageId { get; set; }
    public bool IsEnabled { get; set; }
}

public sealed class EmployeeLifecycleAutomationSettings
{
    public Guid Id { get; set; }
    public bool IsEnabled { get; set; }
    public bool DisableEmployeeOnLeave { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
    public DateTimeOffset? ReenabledAt { get; set; }
    public DateTimeOffset? LastFullReconciledAt { get; set; }

    public static EmployeeLifecycleAutomationSettings Default => new()
    {
        Id = Guid.Empty,
        IsEnabled = true,
        DisableEmployeeOnLeave = false
    };
}

public sealed class EmployeeAccessAutomationReconciliation
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string Reason { get; set; } = null!;
    public DateTimeOffset ScheduledFor { get; set; }
    public DateTimeOffset? LastRetryAt { get; set; }
    public string? LastKnownError { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static EmployeeAccessAutomationReconciliation Create(Guid employeeId, string reason, DateTimeOffset scheduledFor, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
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

public sealed record EmployeeAccessAutomationWorkItem(string TenantId, Guid EmployeeId, string Reason);
