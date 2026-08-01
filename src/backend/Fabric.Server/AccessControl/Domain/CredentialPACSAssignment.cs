namespace Fabric.Server.AccessControl.Domain;

public sealed class CredentialPACSAssignment
{
    private CredentialPACSAssignment() { }

    public Guid Id { get; private set; }
    public Guid CredentialId { get; private set; }
    public Guid CredentialTypeTargetId { get; private set; }
    public Guid AccessControlSystemId { get; private set; }
    public CredentialPACSAssignmentStatus Status { get; private set; }
    public DateTimeOffset ScheduledFor { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public string? NativeAssignmentId { get; private set; }
    public DateTimeOffset? ProvisionedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? FailureReasonCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CredentialPACSAssignment Create(
        Guid credentialId,
        Guid credentialTypeTargetId,
        Guid accessControlSystemId,
        DateTimeOffset scheduledFor,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            CredentialId = credentialId,
            CredentialTypeTargetId = credentialTypeTargetId,
            AccessControlSystemId = accessControlSystemId,
            Status = CredentialPACSAssignmentStatus.Pending,
            ScheduledFor = scheduledFor,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void MarkProvisioned(string nativeAssignmentId, DateTimeOffset now)
    {
        Status = CredentialPACSAssignmentStatus.Provisioned;
        LastAttemptAt = now;
        NativeAssignmentId = nativeAssignmentId;
        ProvisionedAt = now;
        FailureReasonCode = null;
        ErrorMessage = null;
        UpdatedAt = now;
    }

    public void MarkRetryableFailure(string failureReasonCode, string errorMessage, DateTimeOffset nextRetryAt, DateTimeOffset now)
    {
        Status = CredentialPACSAssignmentStatus.FailedRetryable;
        LastAttemptAt = now;
        AttemptCount++;
        FailureReasonCode = failureReasonCode;
        ErrorMessage = errorMessage;
        ScheduledFor = nextRetryAt;
        NativeAssignmentId = null;
        UpdatedAt = now;
    }

    public void MarkTerminalFailure(string failureReasonCode, string errorMessage, DateTimeOffset now)
    {
        Status = CredentialPACSAssignmentStatus.FailedTerminal;
        LastAttemptAt = now;
        AttemptCount++;
        FailureReasonCode = failureReasonCode;
        ErrorMessage = errorMessage;
        NativeAssignmentId = null;
        UpdatedAt = now;
    }

    public void MarkRevoked(DateTimeOffset now)
    {
        Status = CredentialPACSAssignmentStatus.Revoked;
        NativeAssignmentId = null;
        RevokedAt = now;
        UpdatedAt = now;
    }
}
