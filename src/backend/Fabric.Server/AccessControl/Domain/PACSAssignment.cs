using Fabric.Server.Core;

namespace Fabric.Server.AccessControl.Domain;

public sealed class PACSAssignment
{
    private PACSAssignment() { }

    public Guid Id { get; private set; }
    public Guid SourceAssignmentId { get; private set; }
    public Guid AccessLevelTargetId { get; private set; }
    public Guid AccessControlSystemId { get; private set; }
    public Guid IdentityId { get; private set; }
    public PACSAssignmentDurationKind DurationKind { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public PACSAssignmentStatus Status { get; private set; }
    public DateTimeOffset ScheduledFor { get; private set; }
    public string? NativeAssignmentId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? ProvisionedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static Result<PACSAssignment, AccessControlErrors> Create(
        Guid sourceAssignmentId,
        Guid accessLevelTargetId,
        Guid accessControlSystemId,
        Guid identityId,
        PACSAssignmentDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        DateTimeOffset scheduledFor)
    {
        if (durationKind == PACSAssignmentDurationKind.Temporary && !validUntil.HasValue)
            return Result.Failure<PACSAssignment, AccessControlErrors>(AccessControlErrors.ConfigInvalid);

        if (durationKind == PACSAssignmentDurationKind.Permanent && validUntil.HasValue)
            return Result.Failure<PACSAssignment, AccessControlErrors>(AccessControlErrors.ConfigInvalid);

        if (validUntil.HasValue && validUntil.Value <= validFrom)
            return Result.Failure<PACSAssignment, AccessControlErrors>(AccessControlErrors.ConfigInvalid);

        return Result.Success<PACSAssignment, AccessControlErrors>(new PACSAssignment
        {
            Id = Guid.NewGuid(),
            SourceAssignmentId = sourceAssignmentId,
            AccessLevelTargetId = accessLevelTargetId,
            AccessControlSystemId = accessControlSystemId,
            IdentityId = identityId,
            DurationKind = durationKind,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = PACSAssignmentStatus.Pending,
            ScheduledFor = scheduledFor
        });
    }

    public void MarkProvisioned(string nativeAssignmentId, DateTimeOffset now)
    {
        Status = PACSAssignmentStatus.Provisioned;
        NativeAssignmentId = nativeAssignmentId;
        FailureReason = null;
        ProvisionedAt = now;
        CompletedAt = now;
    }

    public void MarkPending()
    {
        Status = PACSAssignmentStatus.Pending;
        FailureReason = null;
    }

    public void MarkContributingProvisioned()
    {
        Status = PACSAssignmentStatus.Provisioned;
        FailureReason = null;
        NativeAssignmentId = null;
        ProvisionedAt = null;
        CompletedAt = null;
    }

    public void MarkFailed(string reason, DateTimeOffset now)
    {
        Status = PACSAssignmentStatus.Failed;
        FailureReason = reason;
        NativeAssignmentId = null;
        ProvisionedAt = null;
        CompletedAt = now;
    }

    public void MarkRevoked(DateTimeOffset now)
    {
        Status = PACSAssignmentStatus.Revoked;
        FailureReason = null;
        NativeAssignmentId = null;
        ProvisionedAt = null;
        CompletedAt = now;
    }
}
