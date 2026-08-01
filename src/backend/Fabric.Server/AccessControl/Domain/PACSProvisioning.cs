namespace Fabric.Server.AccessControl.Domain;

public sealed class PACSProvisioning
{
    private PACSProvisioning() { }

    public Guid Id { get; private set; }
    public Guid AccessLevelTargetId { get; private set; }
    public Guid AccessControlSystemId { get; private set; }
    public Guid IdentityId { get; private set; }
    public PACSAssignmentDurationKind DurationKind { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public ProvisioningTiming ProvisioningTiming { get; private set; }
    public PACSProvisioningStatus Status { get; private set; }
    public DateTimeOffset ScheduledFor { get; private set; }
    public string? NativeAssignmentId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? ProvisionedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static PACSProvisioning Create(
        Guid accessLevelTargetId,
        Guid accessControlSystemId,
        Guid identityId,
        PACSAssignmentDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        ProvisioningTiming provisioningTiming,
        DateTimeOffset scheduledFor) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccessLevelTargetId = accessLevelTargetId,
            AccessControlSystemId = accessControlSystemId,
            IdentityId = identityId,
            DurationKind = durationKind,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            ProvisioningTiming = provisioningTiming,
            Status = PACSProvisioningStatus.Pending,
            ScheduledFor = scheduledFor
        };

    public bool Matches(
        Guid accessLevelTargetId,
        Guid identityId,
        PACSAssignmentDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        ProvisioningTiming provisioningTiming) =>
        AccessLevelTargetId == accessLevelTargetId &&
        IdentityId == identityId &&
        DurationKind == durationKind &&
        ValidFrom == validFrom &&
        ValidUntil == validUntil &&
        ProvisioningTiming == provisioningTiming &&
        Status != PACSProvisioningStatus.Revoked;

    public void MarkPending(DateTimeOffset scheduledFor)
    {
        Status = PACSProvisioningStatus.Pending;
        ScheduledFor = scheduledFor;
        FailureReason = null;
    }

    public void MarkProvisioned(string nativeAssignmentId, DateTimeOffset now)
    {
        Status = PACSProvisioningStatus.Provisioned;
        NativeAssignmentId = nativeAssignmentId;
        FailureReason = null;
        ProvisionedAt = now;
        CompletedAt = now;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        Status = PACSProvisioningStatus.Failed;
        FailureReason = reason;
        CompletedAt = now;
    }

    public void MarkRevoked(DateTimeOffset now)
    {
        Status = PACSProvisioningStatus.Revoked;
        FailureReason = null;
        CompletedAt = now;
    }
}
