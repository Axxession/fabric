using Fabric.Server.Learning.Domain;

namespace Fabric.Server.Learning.Contracts;

public sealed record CourseResponse(Guid Id, string Code, string Title, string? Description, Guid? CurrentVersionId, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record CourseLanguageResponse(Guid Id, Guid CourseId, string LanguageCode, string DisplayLabel, Guid? CurrentVersionId, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record CourseVersionResponse(Guid Id, Guid CourseId, Guid CourseLanguageId, int VersionNumber, string Title, ScormVersion ScormVersion, bool EmitsScore, string StoragePath, string? ManifestChecksum, DateTimeOffset PublishedAt, DateTimeOffset CreatedAt, IReadOnlyList<CourseScoResponse> Scos);
public sealed record CourseScoResponse(Guid Id, Guid CourseVersionId, string ScoIdentifier, string Title, string LaunchUrl, string ResourcePath, int ManifestOrder, decimal? MasteryScore);
public sealed record EnrollmentResponse(Guid Id, Guid CourseId, Guid IdentityId, EnrollmentStatus Status, DateTimeOffset AssignedAt, Guid AssignedByIdentityId, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt, Guid? CompletedAttemptId, Guid? LatestAttemptId, DateTimeOffset? CancelledAt, Guid? CancelledByIdentityId, string? CancellationReason);
public sealed record AttemptResponse(Guid Id, Guid EnrollmentId, Guid CourseId, Guid CourseVersionId, Guid IdentityId, AttemptStatus Status, DateTimeOffset StartedAt, DateTimeOffset? LastActivityAt, DateTimeOffset? CompletedAt, string? CompletionStatus, string? SuccessStatus, decimal? Score, decimal? ScoreScaled, bool IsScored);
public sealed record CourseCompletionReportRowResponse(Guid IdentityId, Guid EnrollmentId, Guid AttemptId, Guid CourseVersionId, int VersionNumber, DateTimeOffset CompletedAt, string? CompletionStatus, string? SuccessStatus, decimal? Score, decimal? ScoreScaled);
public sealed record StartLaunchSessionResponse(string Token);
public sealed record LaunchSessionResponse(Guid Id, Guid EnrollmentId, Guid CourseId, Guid CourseVersionId, Guid? AttemptId, Guid? ScoId, Guid IdentityId, string Token, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt);
public sealed record LaunchSessionBootstrapResponse(Guid EnrollmentId, Guid CourseId, Guid CourseVersionId, Guid CourseLanguageId, ScormVersion ScormVersion, Guid? AttemptId, Guid? ActiveScoId, string ContentBaseUrl, string LaunchPath, DateTimeOffset ExpiresAt, IReadOnlyList<CourseScoResponse> Scos);
public sealed record ScormProgressResponse(Guid Id, Guid AttemptId, Guid CourseId, Guid CourseVersionId, Guid? ScoId, Guid IdentityId, ScormVersion ScormVersion, string? CompletionStatus, string? SuccessStatus, decimal? Score, decimal? ScoreScaled, string? BookmarkLocation, string? SessionTime, string? SuspendData, string RawCmiData, DateTimeOffset LastCommittedAt);

public static class LearningMapper
{
    public static CourseResponse ToResponse(this Course course) =>
        new(course.Id, course.Code, course.Title, course.Description, course.CurrentVersionId, course.IsActive, course.CreatedAt, course.UpdatedAt);

    public static CourseLanguageResponse ToResponse(this CourseLanguage language) =>
        new(language.Id, language.CourseId, language.LanguageCode, language.DisplayLabel, language.CurrentVersionId, language.IsActive, language.CreatedAt, language.UpdatedAt);

    public static CourseVersionResponse ToResponse(this CourseVersion version, IReadOnlyList<CourseSco> scos) =>
        new(version.Id, version.CourseId, version.CourseLanguageId, version.VersionNumber, version.Title, version.ScormVersion, version.EmitsScore, version.StoragePath, version.ManifestChecksum, version.PublishedAt, version.CreatedAt, scos.OrderBy(item => item.ManifestOrder).Select(item => item.ToResponse()).ToArray());

    public static CourseScoResponse ToResponse(this CourseSco sco) =>
        new(sco.Id, sco.CourseVersionId, sco.ScoIdentifier, sco.Title, sco.LaunchUrl, sco.ResourcePath, sco.ManifestOrder, sco.MasteryScore);

    public static EnrollmentResponse ToResponse(this Enrollment enrollment) =>
        new(enrollment.Id, enrollment.CourseId, enrollment.IdentityId, enrollment.Status, enrollment.AssignedAt, enrollment.AssignedByIdentityId, enrollment.StartedAt, enrollment.CompletedAt, enrollment.CompletedAttemptId, enrollment.LatestAttemptId, enrollment.CancelledAt, enrollment.CancelledByIdentityId, enrollment.CancellationReason);

    public static AttemptResponse ToResponse(this Attempt attempt) =>
        new(attempt.Id, attempt.EnrollmentId, attempt.CourseId, attempt.CourseVersionId, attempt.IdentityId, attempt.Status, attempt.StartedAt, attempt.LastActivityAt, attempt.CompletedAt, attempt.CompletionStatus, attempt.SuccessStatus, attempt.Score, attempt.ScoreScaled, attempt.IsScored);

    public static LaunchSessionResponse ToResponse(this LaunchSession session) =>
        new(session.Id, session.EnrollmentId, session.CourseId, session.CourseVersionId, session.AttemptId, session.ScoId, session.IdentityId, session.Token, session.ExpiresAt, session.CreatedAt);

    public static ScormProgressResponse ToResponse(this ScormProgress progress) =>
        new(progress.Id, progress.AttemptId, progress.CourseId, progress.CourseVersionId, progress.ScoId, progress.IdentityId, progress.ScormVersion, progress.CompletionStatus, progress.SuccessStatus, progress.Score, progress.ScoreScaled, progress.BookmarkLocation, progress.SessionTime, progress.SuspendData, progress.RawCmiData, progress.LastCommittedAt);
}
