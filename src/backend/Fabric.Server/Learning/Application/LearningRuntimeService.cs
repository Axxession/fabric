using System.Security.Cryptography;
using Fabric.Server.Core;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Fabric.Server.Sagas.LearningRequirements;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Learning.Application;

public sealed class LearningRuntimeService(LearningDbContext db, TimeProvider timeProvider, LearningRequirementAutomationService learningRequirementAutomationService)
{
    private static readonly TimeSpan DefaultSessionLifetime = TimeSpan.FromHours(4);

    public async Task<Result<LaunchSession, EnrollmentErrors>> CreateLaunchSessionAsync(Guid enrollmentId, Guid? scoId, CancellationToken cancellationToken = default)
    {
        Enrollment? enrollment = await db.Enrollments.SingleOrDefaultAsync(item => item.Id == enrollmentId, cancellationToken);
        if (enrollment is null)
            return Result.Failure<LaunchSession, EnrollmentErrors>(EnrollmentErrors.EnrollmentNotFound);

        if (enrollment.Status is EnrollmentStatus.Completed or EnrollmentStatus.Cancelled)
            return Result.Failure<LaunchSession, EnrollmentErrors>(EnrollmentErrors.EnrollmentNotActive);

        Attempt? activeAttempt = await ResolveActiveAttemptAsync(enrollment, cancellationToken);
        Guid courseVersionId = activeAttempt?.CourseVersionId ?? await ResolveCourseVersionIdAsync(enrollment, cancellationToken);
        if (courseVersionId == Guid.Empty)
            return Result.Failure<LaunchSession, EnrollmentErrors>(EnrollmentErrors.CourseVersionNotFound);

        LaunchSession session = LaunchSession.Create(enrollment.Id, enrollment.CourseId, courseVersionId, scoId, enrollment.IdentityId, GenerateToken(), timeProvider.GetUtcNow().Add(DefaultSessionLifetime), timeProvider.GetUtcNow());
        if (activeAttempt is not null)
            session.LinkAttempt(activeAttempt.Id);

        db.LaunchSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<LaunchSession, EnrollmentErrors>(session);
    }

    public async Task<Result<ScormProgress, EnrollmentErrors>> RecordProgressAsync(RecordScormProgressRequest request, CancellationToken cancellationToken = default)
    {
        LaunchSession? session = await db.LaunchSessions.SingleOrDefaultAsync(item => item.Token == request.Token, cancellationToken);
        if (session is null)
            return Result.Failure<ScormProgress, EnrollmentErrors>(EnrollmentErrors.LaunchSessionTokenInvalid);

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (session.IsExpired(now))
            return Result.Failure<ScormProgress, EnrollmentErrors>(EnrollmentErrors.LaunchSessionExpired);

        Enrollment? enrollment = await db.Enrollments.SingleOrDefaultAsync(item => item.Id == session.EnrollmentId, cancellationToken);
        if (enrollment is null)
            return Result.Failure<ScormProgress, EnrollmentErrors>(EnrollmentErrors.EnrollmentNotFound);

        Attempt attempt;
        if (session.AttemptId.HasValue)
        {
            attempt = await db.Attempts.SingleAsync(item => item.Id == session.AttemptId.Value, cancellationToken);
        }
        else
        {
            attempt = Attempt.Create(enrollment.Id, enrollment.CourseId, session.CourseVersionId, enrollment.IdentityId, now);
            db.Attempts.Add(attempt);
            session.LinkAttempt(attempt.Id);
        }

        bool isScored = request.Score.HasValue || request.ScoreScaled.HasValue;
        bool completed = request.IsCompleted;
        if (completed)
        {
            Result<EnrollmentErrors> complete = attempt.Complete(request.CompletionStatus, request.SuccessStatus, request.Score, request.ScoreScaled, isScored, now);
            if (complete.IsFailure(out EnrollmentErrors error))
                return Result.Failure<ScormProgress, EnrollmentErrors>(error);

            Result<EnrollmentErrors> completeEnrollment = enrollment.Complete(attempt.Id, now);
            if (completeEnrollment.IsFailure(out error))
                return Result.Failure<ScormProgress, EnrollmentErrors>(error);
        }
        else
        {
            attempt.RecordProgress(request.CompletionStatus, request.SuccessStatus, request.Score, request.ScoreScaled, isScored, now);
            Result<EnrollmentErrors> markInProgress = enrollment.MarkInProgress(attempt.Id, now);
            if (markInProgress.IsFailure(out EnrollmentErrors error))
                return Result.Failure<ScormProgress, EnrollmentErrors>(error);
        }

        CourseVersion version = await db.CourseVersions.SingleAsync(item => item.Id == attempt.CourseVersionId, cancellationToken);
        ScormProgress? progress = await db.ScormProgress.SingleOrDefaultAsync(item => item.AttemptId == attempt.Id && item.ScoId == request.ScoId, cancellationToken);
        if (progress is null)
        {
            progress = ScormProgress.Create(attempt.Id, attempt.CourseId, attempt.CourseVersionId, request.ScoId, attempt.IdentityId, version.ScormVersion, request.CompletionStatus, request.SuccessStatus, request.Score, request.ScoreScaled, request.BookmarkLocation, request.SessionTime, request.SuspendData, request.RawCmiData, now);
            db.ScormProgress.Add(progress);
        }
        else
        {
            progress.Update(request.CompletionStatus, request.SuccessStatus, request.Score, request.ScoreScaled, request.BookmarkLocation, request.SessionTime, request.SuspendData, request.RawCmiData, now);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (completed)
            await learningRequirementAutomationService.HandleCourseCompletionAsync(attempt.IdentityId, attempt.CourseId, attempt.Id, attempt.Score, attempt.CompletedAt ?? now, cancellationToken);

        return Result.Success<ScormProgress, EnrollmentErrors>(progress);
    }

    public async Task<Result<ScormProgress?, EnrollmentErrors>> LoadProgressAsync(string token, Guid? scoId, CancellationToken cancellationToken = default)
    {
        LaunchSession? session = await db.LaunchSessions.SingleOrDefaultAsync(item => item.Token == token, cancellationToken);
        if (session is null)
            return Result.Failure<ScormProgress?, EnrollmentErrors>(EnrollmentErrors.LaunchSessionTokenInvalid);

        if (session.IsExpired(timeProvider.GetUtcNow()))
            return Result.Failure<ScormProgress?, EnrollmentErrors>(EnrollmentErrors.LaunchSessionExpired);

        if (!session.AttemptId.HasValue)
            return Result.Success<ScormProgress?, EnrollmentErrors>(null);

        ScormProgress? progress = await db.ScormProgress
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.AttemptId == session.AttemptId.Value && item.ScoId == scoId, cancellationToken);
        return Result.Success<ScormProgress?, EnrollmentErrors>(progress);
    }

    private async Task<Guid> ResolveCourseVersionIdAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        return await db.Courses.AsNoTracking().Where(item => item.Id == enrollment.CourseId).Select(item => item.CurrentVersionId ?? Guid.Empty).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<Attempt?> ResolveActiveAttemptAsync(Enrollment enrollment, CancellationToken cancellationToken)
    {
        if (!enrollment.LatestAttemptId.HasValue)
            return null;

        Attempt? latestAttempt = await db.Attempts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == enrollment.LatestAttemptId.Value, cancellationToken);
        return latestAttempt is not null && latestAttempt.Status == AttemptStatus.Active ? latestAttempt : null;
    }

    private static string GenerateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
