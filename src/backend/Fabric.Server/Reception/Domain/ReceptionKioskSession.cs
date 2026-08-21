using Fabric.Server.Core;

namespace Fabric.Server.Reception.Domain;

public enum ReceptionKioskSessionStatus
{
    Active,
    Completed,
    Stopped,
    Failed
}

public enum ReceptionKioskSessionStep
{
    FacePicture,
    IdentityDocumentCheck,
    ComplianceCheck,
    Onboard
}

public enum ReceptionKioskSessionStepStatus
{
    Pending,
    Active,
    Completed,
    Skipped
}

public enum ReceptionKioskSessionStopReason
{
    HomeRedirect,
    NotCompliant,
    Timeout,
    Superseded,
    Failed
}

public enum ReceptionKioskSessionErrors
{
    SessionNotFound,
    SessionNotActive,
    InvalidCurrentStep,
    FacePictureMissing,
    IdentityDocumentMissing,
    ComplianceNotSatisfied,
    FinalizationNotReady,
    ArrivalNotFound,
    ArrivalAssignedToDifferentLocation,
    ArrivalOutsideKioskOnboardingWindow,
    SubjectAlreadyHasOnboardedArrival,
    InvalidArrivalStatus,
    MissingIdentity,
    CourseNotAvailable,
    LanguageNotAvailable,
    StorageItemMissing
}

public sealed class ReceptionKioskSession
{
    private ReceptionKioskSession() { }

    public Guid Id { get; private set; }
    public Guid KioskId { get; private set; }
    public Guid ArrivalId { get; private set; }
    public ReceptionKioskSessionStatus Status { get; private set; }
    public ReceptionKioskSessionStep? CurrentStep { get; private set; }
    public ReceptionKioskSessionStopReason? StopReason { get; private set; }
    public string? StopMessage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset LastInteractionAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset RetentionUntil { get; private set; }
    public bool RequiresFacePicture { get; private set; }
    public bool RequiresIdentityDocumentCheck { get; private set; }
    public bool RequiresComplianceCheck { get; private set; }
    public ReceptionKioskSessionStepStatus FacePictureStatus { get; private set; }
    public ReceptionKioskSessionStepStatus IdentityDocumentCheckStatus { get; private set; }
    public ReceptionKioskSessionStepStatus ComplianceCheckStatus { get; private set; }
    public ReceptionKioskSessionStepStatus OnboardStatus { get; private set; }
    public string? FacePictureStoragePath { get; private set; }
    public string? IdentityDocumentStoragePath { get; private set; }

    public static ReceptionKioskSession Start(
        Guid kioskId,
        Guid arrivalId,
        bool requiresFacePicture,
        bool requiresIdentityDocumentCheck,
        bool requiresComplianceCheck,
        DateTimeOffset now,
        DateTimeOffset retentionUntil)
    {
        ReceptionKioskSession session = new()
        {
            Id = Guid.NewGuid(),
            KioskId = kioskId,
            ArrivalId = arrivalId,
            Status = ReceptionKioskSessionStatus.Active,
            StartedAt = now,
            LastInteractionAt = now,
            RetentionUntil = retentionUntil,
            RequiresFacePicture = requiresFacePicture,
            RequiresIdentityDocumentCheck = requiresIdentityDocumentCheck,
            RequiresComplianceCheck = requiresComplianceCheck,
            FacePictureStatus = requiresFacePicture ? ReceptionKioskSessionStepStatus.Pending : ReceptionKioskSessionStepStatus.Skipped,
            IdentityDocumentCheckStatus = requiresIdentityDocumentCheck ? ReceptionKioskSessionStepStatus.Pending : ReceptionKioskSessionStepStatus.Skipped,
            ComplianceCheckStatus = requiresComplianceCheck ? ReceptionKioskSessionStepStatus.Pending : ReceptionKioskSessionStepStatus.Skipped,
            OnboardStatus = ReceptionKioskSessionStepStatus.Pending,
        };

        session.ActivateNextStep(now);
        return session;
    }

    public Result<ReceptionKioskSessionErrors> StoreFacePicture(string storagePath, DateTimeOffset now)
    {
        if (Status != ReceptionKioskSessionStatus.Active)
            return Result.Failure(ReceptionKioskSessionErrors.SessionNotActive);
        if (CurrentStep != ReceptionKioskSessionStep.FacePicture)
            return Result.Failure(ReceptionKioskSessionErrors.InvalidCurrentStep);

        FacePictureStoragePath = storagePath.Trim();
        LastInteractionAt = now;
        return Result.Success<ReceptionKioskSessionErrors>();
    }

    public Result<ReceptionKioskSessionErrors> StoreIdentityDocument(string storagePath, DateTimeOffset now)
    {
        if (Status != ReceptionKioskSessionStatus.Active)
            return Result.Failure(ReceptionKioskSessionErrors.SessionNotActive);
        if (CurrentStep != ReceptionKioskSessionStep.IdentityDocumentCheck)
            return Result.Failure(ReceptionKioskSessionErrors.InvalidCurrentStep);

        IdentityDocumentStoragePath = storagePath.Trim();
        LastInteractionAt = now;
        return Result.Success<ReceptionKioskSessionErrors>();
    }

    public Result<ReceptionKioskSessionErrors> Advance(DateTimeOffset now)
    {
        if (Status != ReceptionKioskSessionStatus.Active)
            return Result.Failure(ReceptionKioskSessionErrors.SessionNotActive);
        if (!CurrentStep.HasValue)
            return Result.Failure(ReceptionKioskSessionErrors.InvalidCurrentStep);

        switch (CurrentStep.Value)
        {
            case ReceptionKioskSessionStep.FacePicture:
                if (string.IsNullOrWhiteSpace(FacePictureStoragePath))
                    return Result.Failure(ReceptionKioskSessionErrors.FacePictureMissing);
                FacePictureStatus = ReceptionKioskSessionStepStatus.Completed;
                break;
            case ReceptionKioskSessionStep.IdentityDocumentCheck:
                if (string.IsNullOrWhiteSpace(IdentityDocumentStoragePath))
                    return Result.Failure(ReceptionKioskSessionErrors.IdentityDocumentMissing);
                IdentityDocumentCheckStatus = ReceptionKioskSessionStepStatus.Completed;
                break;
            case ReceptionKioskSessionStep.ComplianceCheck:
                ComplianceCheckStatus = ReceptionKioskSessionStepStatus.Completed;
                break;
            case ReceptionKioskSessionStep.Onboard:
                return Result.Failure(ReceptionKioskSessionErrors.FinalizationNotReady);
            default:
                return Result.Failure(ReceptionKioskSessionErrors.InvalidCurrentStep);
        }

        ActivateNextStep(now);
        return Result.Success<ReceptionKioskSessionErrors>();
    }

    public Result<ReceptionKioskSessionErrors> MarkCompleted(DateTimeOffset now)
    {
        if (Status != ReceptionKioskSessionStatus.Active)
            return Result.Failure(ReceptionKioskSessionErrors.SessionNotActive);
        if (CurrentStep != ReceptionKioskSessionStep.Onboard)
            return Result.Failure(ReceptionKioskSessionErrors.FinalizationNotReady);

        OnboardStatus = ReceptionKioskSessionStepStatus.Completed;
        Status = ReceptionKioskSessionStatus.Completed;
        CurrentStep = null;
        CompletedAt = now;
        LastInteractionAt = now;
        StopReason = null;
        StopMessage = null;
        return Result.Success<ReceptionKioskSessionErrors>();
    }

    public void Stop(ReceptionKioskSessionStopReason reason, string? message, DateTimeOffset now)
    {
        Status = ReceptionKioskSessionStatus.Stopped;
        CurrentStep = null;
        StopReason = reason;
        StopMessage = NormalizeMessage(message);
        LastInteractionAt = now;
        CompletedAt = now;
    }

    public void Fail(string? message, DateTimeOffset now)
    {
        Status = ReceptionKioskSessionStatus.Failed;
        CurrentStep = null;
        StopReason = ReceptionKioskSessionStopReason.Failed;
        StopMessage = NormalizeMessage(message);
        LastInteractionAt = now;
        CompletedAt = now;
    }

    public void ClearStoredArtifacts(DateTimeOffset now)
    {
        FacePictureStoragePath = null;
        IdentityDocumentStoragePath = null;
        LastInteractionAt = now;
    }

    private void ActivateNextStep(DateTimeOffset now)
    {
        if (FacePictureStatus == ReceptionKioskSessionStepStatus.Pending)
        {
            FacePictureStatus = ReceptionKioskSessionStepStatus.Active;
            CurrentStep = ReceptionKioskSessionStep.FacePicture;
        }
        else if (IdentityDocumentCheckStatus == ReceptionKioskSessionStepStatus.Pending)
        {
            IdentityDocumentCheckStatus = ReceptionKioskSessionStepStatus.Active;
            CurrentStep = ReceptionKioskSessionStep.IdentityDocumentCheck;
        }
        else if (ComplianceCheckStatus == ReceptionKioskSessionStepStatus.Pending)
        {
            ComplianceCheckStatus = ReceptionKioskSessionStepStatus.Active;
            CurrentStep = ReceptionKioskSessionStep.ComplianceCheck;
        }
        else
        {
            if (OnboardStatus == ReceptionKioskSessionStepStatus.Pending)
                OnboardStatus = ReceptionKioskSessionStepStatus.Active;

            CurrentStep = ReceptionKioskSessionStep.Onboard;
        }

        LastInteractionAt = now;
    }

    private static string? NormalizeMessage(string? message) => string.IsNullOrWhiteSpace(message) ? null : message.Trim();
}
