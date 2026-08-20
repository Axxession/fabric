using Fabric.Server.Core;

namespace Fabric.Server.Learning.Domain;

public sealed class Enrollment
{
    private Enrollment() { }

    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid IdentityId { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid AssignedByIdentityId { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedAttemptId { get; private set; }
    public Guid? LatestAttemptId { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledByIdentityId { get; private set; }
    public string? CancellationReason { get; private set; }

    public static Enrollment Create(Guid courseId, Guid identityId, Guid assignedByIdentityId, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            IdentityId = identityId,
            Status = EnrollmentStatus.Assigned,
            AssignedAt = now,
            AssignedByIdentityId = assignedByIdentityId,
        };

    public Result<EnrollmentErrors> MarkInProgress(Guid attemptId, DateTimeOffset now)
    {
        if (Status == EnrollmentStatus.Completed)
            return Result.Failure(EnrollmentErrors.EnrollmentAlreadyCompleted);

        if (Status == EnrollmentStatus.Cancelled)
            return Result.Failure(EnrollmentErrors.EnrollmentAlreadyCancelled);

        LatestAttemptId = attemptId;

        if (Status == EnrollmentStatus.Assigned)
        {
            Status = EnrollmentStatus.InProgress;
            StartedAt = now;
        }

        return Result.Success<EnrollmentErrors>();
    }

    public Result<EnrollmentErrors> Complete(Guid attemptId, DateTimeOffset now)
    {
        if (Status == EnrollmentStatus.Completed)
            return Result.Failure(EnrollmentErrors.EnrollmentAlreadyCompleted);

        if (Status == EnrollmentStatus.Cancelled)
            return Result.Failure(EnrollmentErrors.EnrollmentAlreadyCancelled);

        Status = EnrollmentStatus.Completed;
        CompletedAttemptId = attemptId;
        LatestAttemptId = attemptId;
        CompletedAt = now;
        StartedAt ??= now;
        return Result.Success<EnrollmentErrors>();
    }

    public Result<EnrollmentErrors> Cancel(Guid cancelledByIdentityId, string? reason, DateTimeOffset now)
    {
        if (Status == EnrollmentStatus.Completed)
            return Result.Failure(EnrollmentErrors.EnrollmentAlreadyCompleted);

        if (Status == EnrollmentStatus.Cancelled)
            return Result.Failure(EnrollmentErrors.EnrollmentAlreadyCancelled);

        Status = EnrollmentStatus.Cancelled;
        CancelledAt = now;
        CancelledByIdentityId = cancelledByIdentityId;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return Result.Success<EnrollmentErrors>();
    }
}
