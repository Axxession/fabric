using Fabric.Server.Core;

namespace Fabric.Server.Learning.Domain;

public sealed class Attempt
{
    private Attempt() { }

    public Guid Id { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid CourseVersionId { get; private set; }
    public Guid IdentityId { get; private set; }
    public AttemptStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? LastActivityAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CompletionStatus { get; private set; }
    public string? SuccessStatus { get; private set; }
    public decimal? Score { get; private set; }
    public decimal? ScoreScaled { get; private set; }
    public bool IsScored { get; private set; }

    public static Attempt Create(Guid enrollmentId, Guid courseId, Guid courseVersionId, Guid identityId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            CourseId = courseId,
            CourseVersionId = courseVersionId,
            IdentityId = identityId,
            Status = AttemptStatus.Active,
            StartedAt = now,
            LastActivityAt = now,
        };

    public void RecordProgress(string? completionStatus, string? successStatus, decimal? score, decimal? scoreScaled, bool isScored, DateTimeOffset now)
    {
        CompletionStatus = NormalizeOptional(completionStatus);
        SuccessStatus = NormalizeOptional(successStatus);
        Score = score;
        ScoreScaled = scoreScaled;
        IsScored = isScored;
        LastActivityAt = now;
    }

    public Result<EnrollmentErrors> Complete(string? completionStatus, string? successStatus, decimal? score, decimal? scoreScaled, bool isScored, DateTimeOffset now)
    {
        if (Status == AttemptStatus.Completed)
            return Result.Failure(EnrollmentErrors.LaunchSessionAlreadyCompleted);

        RecordProgress(completionStatus, successStatus, score, scoreScaled, isScored, now);
        Status = AttemptStatus.Completed;
        CompletedAt = now;
        return Result.Success<EnrollmentErrors>();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
