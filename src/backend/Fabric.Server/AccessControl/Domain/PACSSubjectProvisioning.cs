using Fabric.Server.Core;

namespace Fabric.Server.AccessControl.Domain;

public sealed class PACSSubjectProvisioning
{
    private PACSSubjectProvisioning() { }

    public Guid Id { get; private set; }
    public Guid PACSSubjectId { get; private set; }
    public PACSSubjectState DesiredState { get; private set; }
    public string DesiredFirstName { get; private set; } = null!;
    public string DesiredLastName { get; private set; } = null!;
    public string? DesiredEmail { get; private set; }
    public PACSSubjectProvisioningReason Reason { get; private set; }
    public PACSSubjectProvisioningSourceKind SourceKind { get; private set; }
    public Guid SourceId { get; private set; }
    public PACSSubjectProvisioningStatus Status { get; private set; }
    public DateTimeOffset ScheduledFor { get; private set; }
    public DateTimeOffset? LastRetryAt { get; private set; }
    public string? LastKnownError { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<PACSSubjectProvisioning, AccessControlErrors> Create(
        Guid pacsSubjectId,
        PACSSubjectState desiredState,
        string desiredFirstName,
        string desiredLastName,
        string? desiredEmail,
        PACSSubjectProvisioningReason reason,
        PACSSubjectProvisioningSourceKind sourceKind,
        Guid sourceId,
        DateTimeOffset scheduledFor,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(desiredFirstName) || string.IsNullOrWhiteSpace(desiredLastName))
            return Result.Failure<PACSSubjectProvisioning, AccessControlErrors>(AccessControlErrors.ConfigInvalid);

        return Result.Success<PACSSubjectProvisioning, AccessControlErrors>(new PACSSubjectProvisioning
        {
            Id = Guid.NewGuid(),
            PACSSubjectId = pacsSubjectId,
            DesiredState = desiredState,
            DesiredFirstName = desiredFirstName.Trim(),
            DesiredLastName = desiredLastName.Trim(),
            DesiredEmail = NormalizeOptional(desiredEmail),
            Reason = reason,
            SourceKind = sourceKind,
            SourceId = sourceId,
            Status = PACSSubjectProvisioningStatus.Pending,
            ScheduledFor = scheduledFor,
            AttemptCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public Result<AccessControlErrors> Overwrite(
        PACSSubjectState desiredState,
        string desiredFirstName,
        string desiredLastName,
        string? desiredEmail,
        PACSSubjectProvisioningReason reason,
        PACSSubjectProvisioningSourceKind sourceKind,
        Guid sourceId,
        DateTimeOffset scheduledFor,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(desiredFirstName) || string.IsNullOrWhiteSpace(desiredLastName))
            return Result.Failure(AccessControlErrors.ConfigInvalid);

        DesiredState = desiredState;
        DesiredFirstName = desiredFirstName.Trim();
        DesiredLastName = desiredLastName.Trim();
        DesiredEmail = NormalizeOptional(desiredEmail);
        Reason = reason;
        SourceKind = sourceKind;
        SourceId = sourceId;
        Status = PACSSubjectProvisioningStatus.Pending;
        ScheduledFor = scheduledFor;
        LastRetryAt = null;
        LastKnownError = null;
        AttemptCount = 0;
        UpdatedAt = now;
        return Result.Success<AccessControlErrors>();
    }

    public void MarkInProgress(DateTimeOffset now)
    {
        Status = PACSSubjectProvisioningStatus.InProgress;
        UpdatedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset scheduledFor, DateTimeOffset now)
    {
        Status = PACSSubjectProvisioningStatus.Failed;
        LastRetryAt = now;
        LastKnownError = error;
        AttemptCount++;
        ScheduledFor = scheduledFor;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
