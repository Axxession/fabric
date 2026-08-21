using Fabric.Server.Core;
using Fabric.Server.Reception.Contracts;
using Fabric.Server.Reception.Domain;
using Fabric.Server.Reception.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Reception.Application;

public sealed class ReceptionKioskSessionService(
    ReceptionDbContext db,
    ReceptionService receptionService,
    ReceptionKioskComplianceService complianceService,
    IReceptionKioskSessionStorage sessionStorage,
    TimeProvider timeProvider)
{
    public static readonly TimeSpan SessionTimeout = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan SessionRetention = TimeSpan.FromDays(30);

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> StartAsync(ReceptionKiosk kiosk, string code, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        ReceptionKioskSession? activeSession = await GetActiveSessionInternalAsync(kiosk.Id, cancellationToken);
        if (activeSession is not null)
            activeSession.Stop(ReceptionKioskSessionStopReason.Superseded, "Superseded by a new session.", now);

        Result<ExpectedArrival?, ReceptionErrors> lookup = await receptionService.ResolveArrivalForKiosk(code, kiosk, cancellationToken);
        if (!lookup.IsSuccess(out ExpectedArrival? arrival) || arrival is null)
            return MapLookupFailure(lookup);
        if (arrival.Status != OnboardingStatus.NotYetOnboarded)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.InvalidArrivalStatus);

        bool requiresComplianceCheck = await RequiresComplianceCheckAsync(arrival.Id, cancellationToken);
        ReceptionKioskSession session = ReceptionKioskSession.Start(
            kiosk.Id,
            arrival.Id,
            kiosk.RequireFacePicture,
            kiosk.IdentityVerificationMethod == IdentityVerificationMethod.Picture,
            requiresComplianceCheck,
            now,
            now.Add(SessionRetention));

        db.ReceptionKioskSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(await BuildResponseAsync(session, arrival, cancellationToken));
    }

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> GetCurrentAsync(ReceptionKiosk kiosk, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await db.ReceptionKioskSessions
            .OrderByDescending(item => item.StartedAt)
            .FirstOrDefaultAsync(item => item.KioskId == kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);

        await TimeoutIfNeededAsync(session, cancellationToken);

        ExpectedArrival? arrival = await db.Arrivals.AsNoTracking().SingleOrDefaultAsync(item => item.Id == session.ArrivalId, cancellationToken);
        if (arrival is null)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.ArrivalNotFound);

        return Result.Success<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(await BuildResponseAsync(session, arrival, cancellationToken));
    }

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> StoreFacePictureAsync(ReceptionKiosk kiosk, byte[] content, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await GetRequiredActiveSessionAsync(kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        string path = await sessionStorage.SaveAsync(session.Id, "face-picture.jpg", content, cancellationToken);
        Result<ReceptionKioskSessionErrors> result = session.StoreFacePicture(path, now);
        if (result.IsFailure(out ReceptionKioskSessionErrors error))
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return await GetCurrentAsync(kiosk, cancellationToken);
    }

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> StoreIdentityDocumentAsync(ReceptionKiosk kiosk, byte[] content, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await GetRequiredActiveSessionAsync(kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        string path = await sessionStorage.SaveAsync(session.Id, "identity-document.jpg", content, cancellationToken);
        Result<ReceptionKioskSessionErrors> result = session.StoreIdentityDocument(path, now);
        if (result.IsFailure(out ReceptionKioskSessionErrors error))
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return await GetCurrentAsync(kiosk, cancellationToken);
    }

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> AdvanceAsync(ReceptionKiosk kiosk, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await GetRequiredActiveSessionAsync(kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);

        if (session.CurrentStep == ReceptionKioskSessionStep.ComplianceCheck)
        {
            Result<ReceptionKioskComplianceResponse, ReceptionErrors> compliance = await complianceService.GetComplianceAsync(session.ArrivalId, cancellationToken);
            if (!compliance.IsSuccess(out ReceptionKioskComplianceResponse? currentCompliance) || currentCompliance is null)
                return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.ComplianceNotSatisfied);
            if (currentCompliance.Status == Requirements.Domain.ContextComplianceStatus.NonCompliant)
                return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.ComplianceNotSatisfied);
        }

        Result<ReceptionKioskSessionErrors> result = session.Advance(timeProvider.GetUtcNow());
        if (result.IsFailure(out ReceptionKioskSessionErrors error))
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return await GetCurrentAsync(kiosk, cancellationToken);
    }

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> StopAsync(ReceptionKiosk kiosk, ReceptionKioskSessionStopReason reason, string? message, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await db.ReceptionKioskSessions
            .OrderByDescending(item => item.StartedAt)
            .FirstOrDefaultAsync(item => item.KioskId == kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);

        session.Stop(reason, message, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return await GetCurrentAsync(kiosk, cancellationToken);
    }

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> MarkNonCompliantAsync(ReceptionKiosk kiosk, string? message, CancellationToken cancellationToken = default)
        => await StopAsync(kiosk, ReceptionKioskSessionStopReason.NotCompliant, message, cancellationToken);

    public async Task<Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>> FinalizeAsync(ReceptionKiosk kiosk, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await GetRequiredActiveSessionAsync(kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);
        if (session.CurrentStep != ReceptionKioskSessionStep.Onboard)
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.FinalizationNotReady);
        if ((session.RequiresFacePicture && session.FacePictureStatus != ReceptionKioskSessionStepStatus.Completed)
            || (session.RequiresIdentityDocumentCheck && session.IdentityDocumentCheckStatus != ReceptionKioskSessionStepStatus.Completed)
            || (session.RequiresComplianceCheck && session.ComplianceCheckStatus != ReceptionKioskSessionStepStatus.Completed))
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.FinalizationNotReady);

        List<CheckInDocumentRequirement> requiredDocuments = BuildRequiredDocuments(session);
        List<CheckInDocument> providedDocuments = await BuildProvidedDocumentsAsync(session, cancellationToken);

        Result<ReceptionErrors> onboard = await receptionService.OnboardFromKiosk(session.ArrivalId, requiredDocuments, providedDocuments, kiosk.Id, kiosk.Name, cancellationToken);
        if (onboard.IsFailure(out ReceptionErrors onboardError))
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(MapReceptionError(onboardError));

        await DeleteStoredArtifactsAsync(session, cancellationToken);
        Result<ReceptionKioskSessionErrors> complete = session.MarkCompleted(timeProvider.GetUtcNow());
        if (complete.IsFailure(out ReceptionKioskSessionErrors error))
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return await GetCurrentAsync(kiosk, cancellationToken);
    }

    public async Task<Result<ReceptionKioskComplianceResponse, ReceptionKioskSessionErrors>> GetComplianceAsync(ReceptionKiosk kiosk, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await GetRequiredActiveSessionAsync(kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskComplianceResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);

        Result<ReceptionKioskComplianceResponse, ReceptionErrors> result = await complianceService.GetComplianceAsync(session.ArrivalId, cancellationToken);
        return result.IsSuccess(out ReceptionKioskComplianceResponse? response) && response is not null
            ? Result.Success<ReceptionKioskComplianceResponse, ReceptionKioskSessionErrors>(response)
            : Result.Failure<ReceptionKioskComplianceResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.ArrivalNotFound);
    }

    public async Task<Result<ReceptionKioskComplianceCourseLaunchResponse, ReceptionKioskSessionErrors>> LaunchComplianceCourseAsync(ReceptionKiosk kiosk, Guid requirementDefinitionId, Guid? languageId, CancellationToken cancellationToken = default)
    {
        ReceptionKioskSession? session = await GetRequiredActiveSessionAsync(kiosk.Id, cancellationToken);
        if (session is null)
            return Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.SessionNotFound);

        Result<ReceptionKioskComplianceCourseLaunchResponse, ReceptionErrors> result = await complianceService.LaunchRequirementCourseAsync(session.ArrivalId, requirementDefinitionId, languageId, cancellationToken);
        return result.IsSuccess(out ReceptionKioskComplianceCourseLaunchResponse? response) && response is not null
            ? Result.Success<ReceptionKioskComplianceCourseLaunchResponse, ReceptionKioskSessionErrors>(response)
            : Result.Failure<ReceptionKioskComplianceCourseLaunchResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.CourseNotAvailable);
    }

    public async Task<int> DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        List<ReceptionKioskSession> expiredSessions = await db.ReceptionKioskSessions
            .Where(item => item.RetentionUntil <= now && item.Status != ReceptionKioskSessionStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (ReceptionKioskSession session in expiredSessions)
            await DeleteStoredArtifactsAsync(session, cancellationToken);

        db.ReceptionKioskSessions.RemoveRange(expiredSessions);
        await db.SaveChangesAsync(cancellationToken);
        return expiredSessions.Count;
    }

    private async Task<ReceptionKioskSession?> GetRequiredActiveSessionAsync(Guid kioskId, CancellationToken cancellationToken)
    {
        ReceptionKioskSession? session = await GetActiveSessionInternalAsync(kioskId, cancellationToken);
        if (session is null)
            return null;

        await TimeoutIfNeededAsync(session, cancellationToken);
        return session.Status == ReceptionKioskSessionStatus.Active ? session : null;
    }

    private async Task<ReceptionKioskSession?> GetActiveSessionInternalAsync(Guid kioskId, CancellationToken cancellationToken) =>
        await db.ReceptionKioskSessions
            .OrderByDescending(item => item.StartedAt)
            .FirstOrDefaultAsync(item => item.KioskId == kioskId && item.Status == ReceptionKioskSessionStatus.Active, cancellationToken);

    private async Task TimeoutIfNeededAsync(ReceptionKioskSession session, CancellationToken cancellationToken)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        if (session.Status == ReceptionKioskSessionStatus.Active && session.LastInteractionAt <= now.Subtract(SessionTimeout))
        {
            session.Stop(ReceptionKioskSessionStopReason.Timeout, "Session timed out.", now);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> RequiresComplianceCheckAsync(Guid arrivalId, CancellationToken cancellationToken)
    {
        Result<ReceptionKioskComplianceResponse, ReceptionErrors> result = await complianceService.GetComplianceAsync(arrivalId, cancellationToken);
        return result.IsSuccess(out ReceptionKioskComplianceResponse? compliance) && compliance is not null && compliance.Requirements.Length > 0;
    }

    private async Task<ReceptionKioskSessionResponse> BuildResponseAsync(ReceptionKioskSession session, ExpectedArrival arrival, CancellationToken cancellationToken)
    {
        var arrivalResponse = arrival.ToKioskResponseForSession(session);
        ReceptionKioskSessionStepResponse[] steps =
        [
            new(ReceptionKioskSessionStep.FacePicture, session.FacePictureStatus),
            new(ReceptionKioskSessionStep.IdentityDocumentCheck, session.IdentityDocumentCheckStatus),
            new(ReceptionKioskSessionStep.ComplianceCheck, session.ComplianceCheckStatus),
            new(ReceptionKioskSessionStep.Onboard, session.OnboardStatus),
        ];

        return new ReceptionKioskSessionResponse(
            session.Id,
            session.KioskId,
            session.ArrivalId,
            arrivalResponse,
            session.Status,
            session.CurrentStep,
            session.StopReason,
            session.StopMessage,
            session.StartedAt,
            session.LastInteractionAt,
            session.CompletedAt,
            steps);
    }

    private static Result<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors> MapLookupFailure(Result<ExpectedArrival?, ReceptionErrors> lookup)
    {
        if (lookup.IsFailure(out ReceptionErrors error))
            return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(MapReceptionError(error));

        return Result.Failure<ReceptionKioskSessionResponse, ReceptionKioskSessionErrors>(ReceptionKioskSessionErrors.ArrivalNotFound);
    }

    private static ReceptionKioskSessionErrors MapReceptionError(ReceptionErrors error) => error switch
    {
        ReceptionErrors.ArrivalNotFound => ReceptionKioskSessionErrors.ArrivalNotFound,
        ReceptionErrors.ArrivalAssignedToDifferentLocation => ReceptionKioskSessionErrors.ArrivalAssignedToDifferentLocation,
        ReceptionErrors.ArrivalOutsideKioskOnboardingWindow => ReceptionKioskSessionErrors.ArrivalOutsideKioskOnboardingWindow,
        ReceptionErrors.SubjectAlreadyHasOnboardedArrival => ReceptionKioskSessionErrors.SubjectAlreadyHasOnboardedArrival,
        _ => ReceptionKioskSessionErrors.InvalidArrivalStatus,
    };

    private static List<CheckInDocumentRequirement> BuildRequiredDocuments(ReceptionKioskSession session)
    {
        List<CheckInDocumentRequirement> requiredDocuments = [];

        if (session.RequiresFacePicture)
        {
            requiredDocuments.Add(new CheckInDocumentRequirement
            {
                Name = "Face picture",
                Required = true,
                DocumentType = CheckInDocumentType.FacePicture
            });
        }

        if (session.RequiresIdentityDocumentCheck)
        {
            requiredDocuments.Add(new CheckInDocumentRequirement
            {
                Name = "Identity document picture",
                Required = true,
                DocumentType = CheckInDocumentType.IdentityDocumentImage
            });
        }

        return requiredDocuments;
    }

    private async Task<List<CheckInDocument>> BuildProvidedDocumentsAsync(ReceptionKioskSession session, CancellationToken cancellationToken)
    {
        List<CheckInDocument> providedDocuments = [];

        if (session.RequiresFacePicture)
        {
            if (string.IsNullOrWhiteSpace(session.FacePictureStoragePath))
                throw new InvalidOperationException("Face picture storage path is missing.");

            byte[]? facePicture = await sessionStorage.ReadAsync(session.FacePictureStoragePath, cancellationToken);
            if (facePicture is null)
                throw new InvalidOperationException("Face picture storage item is missing.");

            providedDocuments.Add(CheckInDocument.Create("Face picture", CheckInDocumentType.FacePicture, facePicture));
        }

        if (session.RequiresIdentityDocumentCheck)
        {
            if (string.IsNullOrWhiteSpace(session.IdentityDocumentStoragePath))
                throw new InvalidOperationException("Identity document storage path is missing.");

            byte[]? identityDocument = await sessionStorage.ReadAsync(session.IdentityDocumentStoragePath, cancellationToken);
            if (identityDocument is null)
                throw new InvalidOperationException("Identity document storage item is missing.");

            providedDocuments.Add(CheckInDocument.Create("Identity document picture", CheckInDocumentType.IdentityDocumentImage, identityDocument));
        }

        return providedDocuments;
    }

    private async Task DeleteStoredArtifactsAsync(ReceptionKioskSession session, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(session.FacePictureStoragePath))
            await sessionStorage.DeleteAsync(session.FacePictureStoragePath, cancellationToken);
        if (!string.IsNullOrWhiteSpace(session.IdentityDocumentStoragePath))
            await sessionStorage.DeleteAsync(session.IdentityDocumentStoragePath, cancellationToken);

        session.ClearStoredArtifacts(timeProvider.GetUtcNow());
    }
}

internal static class ReceptionKioskSessionMapper
{
    public static ReceptionKioskExpectedArrivalResponse ToKioskResponseForSession(this ExpectedArrival arrival, ReceptionKioskSession session) =>
        new(
            arrival.Id,
            arrival.Type,
            arrival.ExpectedArrivalTime,
            arrival.ExpectedOffboardTime,
            arrival.FirstName,
            arrival.LastName,
            arrival.Company,
            arrival.Status,
            arrival.CheckedIn,
            arrival.LocationId,
            new ReceptionKioskOnboardingRequirementsResponse(session.RequiresFacePicture, session.RequiresIdentityDocumentCheck ? IdentityVerificationMethod.Picture : null),
            null,
            null);
}
