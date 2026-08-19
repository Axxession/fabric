namespace Fabric.Server.Learning.Domain;

public sealed class LaunchSession
{
    private LaunchSession() { }

    public Guid Id { get; private set; }
    public Guid EnrollmentId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid CourseVersionId { get; private set; }
    public Guid? AttemptId { get; private set; }
    public Guid? ScoId { get; private set; }
    public Guid IdentityId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static LaunchSession Create(Guid enrollmentId, Guid courseId, Guid courseVersionId, Guid? scoId, Guid identityId, string token, DateTimeOffset expiresAt, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            EnrollmentId = enrollmentId,
            CourseId = courseId,
            CourseVersionId = courseVersionId,
            ScoId = scoId,
            IdentityId = identityId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = now,
        };

    public void LinkAttempt(Guid attemptId) => AttemptId = attemptId;

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;
}
