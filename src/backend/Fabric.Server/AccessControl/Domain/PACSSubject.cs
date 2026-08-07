using Fabric.Server.Core;

namespace Fabric.Server.AccessControl.Domain;

public sealed class PACSSubject
{
    private PACSSubject() { }

    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public Guid AccessControlSystemId { get; private set; }
    public string NativeSubjectId { get; private set; } = null!;
    public PACSSubjectState State { get; private set; }
    public PACSSubjectConformityStatus ConformityStatus { get; private set; }
    public string? ConformityDetails { get; private set; }
    public DateTimeOffset? LastConformityCheckedAt { get; private set; }
    public string? LastConformityError { get; private set; }
    public bool IsManualProvisioningBlocked { get; private set; }
    public string? ManualProvisioningBlockedReason { get; private set; }
    public DateTimeOffset? ManualProvisioningBlockedAt { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Email { get; private set; }
    public DateTimeOffset LastSynchronizedAt { get; private set; }

    public static PACSSubject Create(
        Guid identityId,
        Guid accessControlSystemId,
        string nativeSubjectId,
        PACSSubjectState state,
        string firstName,
        string lastName,
        string? email,
        DateTimeOffset lastSynchronizedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            AccessControlSystemId = accessControlSystemId,
            NativeSubjectId = nativeSubjectId,
            State = state,
            ConformityStatus = PACSSubjectConformityStatus.Unknown,
            FirstName = firstName,
            LastName = lastName,
            Email = NormalizeOptional(email),
            LastSynchronizedAt = lastSynchronizedAt
        };

    public void ApplySynchronizedRepresentation(
        PACSSubjectState state,
        string firstName,
        string lastName,
        string? email,
        DateTimeOffset synchronizedAt)
    {
        State = state;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = NormalizeOptional(email);
        LastSynchronizedAt = synchronizedAt;
    }

    public Result<AccessControlErrors> BlockProvisioningManually(string reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(AccessControlErrors.ProvisioningBlockReasonRequired);

        IsManualProvisioningBlocked = true;
        ManualProvisioningBlockedReason = reason.Trim();
        ManualProvisioningBlockedAt = now;
        return Result.Success<AccessControlErrors>();
    }

    public void AllowProvisioningManually()
    {
        IsManualProvisioningBlocked = false;
        ManualProvisioningBlockedReason = null;
        ManualProvisioningBlockedAt = null;
    }

    public void ApplyConformityCheck(PACSSubjectConformityStatus conformityStatus, string? details, DateTimeOffset checkedAt)
    {
        ConformityStatus = conformityStatus;
        ConformityDetails = NormalizeOptional(details);
        LastConformityCheckedAt = checkedAt;
        LastConformityError = null;
    }

    public void MarkConformityCheckFailed(string error, DateTimeOffset checkedAt)
    {
        LastConformityCheckedAt = checkedAt;
        LastConformityError = error.Trim();
    }

    public PACSSubjectProvisioningBlockStatus GetProvisioningBlockStatus(AnomalyBlockMode anomalyBlockMode)
    {
        if (IsManualProvisioningBlocked)
            return PACSSubjectProvisioningBlockStatus.BlockedManual;

        if (anomalyBlockMode == AnomalyBlockMode.BlockProvisioning && ConformityStatus == PACSSubjectConformityStatus.Anomaly)
            return PACSSubjectProvisioningBlockStatus.BlockedByAnomaly;

        return PACSSubjectProvisioningBlockStatus.ProvisioningAllowed;
    }

    public string? GetProvisioningBlockedReason(AnomalyBlockMode anomalyBlockMode) =>
        GetProvisioningBlockStatus(anomalyBlockMode) switch
        {
            PACSSubjectProvisioningBlockStatus.BlockedManual => ManualProvisioningBlockedReason,
            PACSSubjectProvisioningBlockStatus.BlockedByAnomaly => ConformityDetails,
            _ => null
        };

    public DateTimeOffset? GetProvisioningBlockedAt(AnomalyBlockMode anomalyBlockMode) =>
        GetProvisioningBlockStatus(anomalyBlockMode) switch
        {
            PACSSubjectProvisioningBlockStatus.BlockedManual => ManualProvisioningBlockedAt,
            PACSSubjectProvisioningBlockStatus.BlockedByAnomaly => LastConformityCheckedAt,
            _ => null
        };

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
