namespace Fabric.Server.Learning.Domain;

public sealed class ScormProgress
{
    private ScormProgress() { }

    public Guid Id { get; private set; }
    public Guid AttemptId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid CourseVersionId { get; private set; }
    public Guid? ScoId { get; private set; }
    public Guid IdentityId { get; private set; }
    public ScormVersion ScormVersion { get; private set; }
    public string? CompletionStatus { get; private set; }
    public string? SuccessStatus { get; private set; }
    public decimal? Score { get; private set; }
    public decimal? ScoreScaled { get; private set; }
    public string? BookmarkLocation { get; private set; }
    public string? SessionTime { get; private set; }
    public string? SuspendData { get; private set; }
    public string RawCmiData { get; private set; } = null!;
    public DateTimeOffset LastCommittedAt { get; private set; }

    public static ScormProgress Create(Guid attemptId, Guid courseId, Guid courseVersionId, Guid? scoId, Guid identityId, ScormVersion scormVersion, string? completionStatus, string? successStatus, decimal? score, decimal? scoreScaled, string? bookmarkLocation, string? sessionTime, string? suspendData, string rawCmiData, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            AttemptId = attemptId,
            CourseId = courseId,
            CourseVersionId = courseVersionId,
            ScoId = scoId,
            IdentityId = identityId,
            ScormVersion = scormVersion,
            CompletionStatus = NormalizeOptional(completionStatus),
            SuccessStatus = NormalizeOptional(successStatus),
            Score = score,
            ScoreScaled = scoreScaled,
            BookmarkLocation = NormalizeOptional(bookmarkLocation),
            SessionTime = NormalizeOptional(sessionTime),
            SuspendData = NormalizeOptional(suspendData),
            RawCmiData = rawCmiData,
            LastCommittedAt = now,
        };

    public void Update(string? completionStatus, string? successStatus, decimal? score, decimal? scoreScaled, string? bookmarkLocation, string? sessionTime, string? suspendData, string rawCmiData, DateTimeOffset now)
    {
        CompletionStatus = NormalizeOptional(completionStatus);
        SuccessStatus = NormalizeOptional(successStatus);
        Score = score;
        ScoreScaled = scoreScaled;
        BookmarkLocation = NormalizeOptional(bookmarkLocation);
        SessionTime = NormalizeOptional(sessionTime);
        SuspendData = NormalizeOptional(suspendData);
        RawCmiData = rawCmiData;
        LastCommittedAt = now;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
