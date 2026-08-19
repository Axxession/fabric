using Fabric.Server.Core;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Learning.Application;

public sealed class EnrollmentService(
    LearningDbContext db,
    IdentitiesDbContext identitiesDb,
    TimeProvider timeProvider)
{
    public async Task<Result<Enrollment, EnrollmentErrors>> UpsertEnrollmentAsync(CreateEnrollmentRequest request, Guid assignedByIdentityId, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(item => item.Id == request.CourseId && item.IsActive, cancellationToken))
            return Result.Failure<Enrollment, EnrollmentErrors>(EnrollmentErrors.CourseNotFound);

        if (!await identitiesDb.Identities.AnyAsync(item => item.Id == request.IdentityId, cancellationToken))
            return Result.Failure<Enrollment, EnrollmentErrors>(EnrollmentErrors.IdentityNotFound);

        Enrollment? existing = await db.Enrollments
            .SingleOrDefaultAsync(item => item.CourseId == request.CourseId && item.IdentityId == request.IdentityId && (item.Status == EnrollmentStatus.Assigned || item.Status == EnrollmentStatus.InProgress), cancellationToken);
        if (existing is not null)
            return Result.Success<Enrollment, EnrollmentErrors>(existing);

        Enrollment enrollment = Enrollment.Create(request.CourseId, request.IdentityId, assignedByIdentityId, timeProvider.GetUtcNow());
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Enrollment, EnrollmentErrors>(enrollment);
    }

    public async Task<Result<Enrollment, EnrollmentErrors>> CreateEnrollmentAsync(CreateEnrollmentRequest request, Guid assignedByIdentityId, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(item => item.Id == request.CourseId && item.IsActive, cancellationToken))
            return Result.Failure<Enrollment, EnrollmentErrors>(EnrollmentErrors.CourseNotFound);

        if (!await identitiesDb.Identities.AnyAsync(item => item.Id == request.IdentityId, cancellationToken))
            return Result.Failure<Enrollment, EnrollmentErrors>(EnrollmentErrors.IdentityNotFound);

        bool hasActiveEnrollment = await db.Enrollments.AnyAsync(item => item.CourseId == request.CourseId && item.IdentityId == request.IdentityId && (item.Status == EnrollmentStatus.Assigned || item.Status == EnrollmentStatus.InProgress), cancellationToken);
        if (hasActiveEnrollment)
            return Result.Failure<Enrollment, EnrollmentErrors>(EnrollmentErrors.ActiveEnrollmentAlreadyExists);

        Enrollment enrollment = Enrollment.Create(request.CourseId, request.IdentityId, assignedByIdentityId, timeProvider.GetUtcNow());
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Enrollment, EnrollmentErrors>(enrollment);
    }

    public async Task<Result<Enrollment, EnrollmentErrors>> CancelEnrollmentAsync(Guid enrollmentId, Guid cancelledByIdentityId, string? reason, CancellationToken cancellationToken = default)
    {
        Enrollment? enrollment = await db.Enrollments.SingleOrDefaultAsync(item => item.Id == enrollmentId, cancellationToken);
        if (enrollment is null)
            return Result.Failure<Enrollment, EnrollmentErrors>(EnrollmentErrors.EnrollmentNotFound);

        Result<EnrollmentErrors> cancel = enrollment.Cancel(cancelledByIdentityId, reason, timeProvider.GetUtcNow());
        if (cancel.IsFailure(out EnrollmentErrors error))
            return Result.Failure<Enrollment, EnrollmentErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Enrollment, EnrollmentErrors>(enrollment);
    }
}
