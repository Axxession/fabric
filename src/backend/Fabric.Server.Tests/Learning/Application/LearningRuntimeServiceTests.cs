using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Fabric.Server.Requirements.Persistence;
using Fabric.Server.Sagas;
using Fabric.Server.Sagas.LearningRequirements;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.Learning.Application;

public sealed class LearningRuntimeServiceTests
{
    [Fact]
    public async Task CreateLaunchSessionAsync_WhenCalled_DoesNotCreateAttempt()
    {
        DateTimeOffset now = new(2026, 8, 19, 9, 0, 0, TimeSpan.Zero);

        await using LearningDbContext db = CreateLearningDbContext();
        SeedActiveEnrollment(db, now, out Enrollment enrollment, out CourseVersion version);
        await db.SaveChangesAsync();

        LearningRuntimeService service = CreateService(db, now);

        Result<LaunchSession, EnrollmentErrors> result = await service.CreateLaunchSessionAsync(enrollment.Id, null);

        Assert.True(result.IsSuccess(out LaunchSession session));
        Assert.Equal(version.Id, session.CourseVersionId);
        Assert.Empty(await db.Attempts.ToArrayAsync());
    }

    [Fact]
    public async Task RecordProgressAsync_WhenFirstProgressArrives_CreatesAttemptAndMovesEnrollmentToInProgress()
    {
        DateTimeOffset now = new(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);

        await using LearningDbContext db = CreateLearningDbContext();
        SeedActiveEnrollment(db, now, out Enrollment enrollment, out CourseVersion _, out CourseSco sco);
        await db.SaveChangesAsync();

        LearningRuntimeService service = CreateService(db, now);
        Result<LaunchSession, EnrollmentErrors> sessionResult = await service.CreateLaunchSessionAsync(enrollment.Id, sco.Id);
        sessionResult.IsSuccess(out LaunchSession session);

        Result<ScormProgress, EnrollmentErrors> progressResult = await service.RecordProgressAsync(new RecordScormProgressRequest(session.Token, sco.Id, "incomplete", null, null, null, "page-2", "PT5M", "bookmark", false, "{}"));

        Assert.True(progressResult.IsSuccess(out ScormProgress progress));
        Enrollment storedEnrollment = await db.Enrollments.SingleAsync(item => item.Id == enrollment.Id);
        Attempt storedAttempt = await db.Attempts.SingleAsync();
        Assert.Equal(EnrollmentStatus.InProgress, storedEnrollment.Status);
        Assert.Equal(storedAttempt.Id, storedEnrollment.LatestAttemptId);
        Assert.Equal(storedAttempt.Id, progress.AttemptId);
    }

    [Fact]
    public async Task RecordProgressAsync_WhenCompletionArrives_CompletesAttemptAndEnrollment()
    {
        DateTimeOffset now = new(2026, 8, 19, 11, 0, 0, TimeSpan.Zero);

        await using LearningDbContext db = CreateLearningDbContext();
        SeedActiveEnrollment(db, now, out Enrollment enrollment, out CourseVersion _, out CourseSco sco);
        await db.SaveChangesAsync();

        LearningRuntimeService service = CreateService(db, now);
        Result<LaunchSession, EnrollmentErrors> sessionResult = await service.CreateLaunchSessionAsync(enrollment.Id, sco.Id);
        sessionResult.IsSuccess(out LaunchSession session);

        Result<ScormProgress, EnrollmentErrors> progressResult = await service.RecordProgressAsync(new RecordScormProgressRequest(session.Token, sco.Id, "completed", "passed", 97m, 0.97m, "done", "PT10M", null, true, "{}"));

        Assert.True(progressResult.IsSuccess(out _));
        Enrollment storedEnrollment = await db.Enrollments.SingleAsync(item => item.Id == enrollment.Id);
        Attempt storedAttempt = await db.Attempts.SingleAsync();
        Assert.Equal(EnrollmentStatus.Completed, storedEnrollment.Status);
        Assert.Equal(storedAttempt.Id, storedEnrollment.CompletedAttemptId);
        Assert.Equal(AttemptStatus.Completed, storedAttempt.Status);
        Assert.Equal(97m, storedAttempt.Score);
    }

    private static LearningDbContext CreateLearningDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<LearningDbContext> options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseInMemoryDatabase($"learning-{Guid.NewGuid()}")
            .Options;
        return new LearningDbContext(options, tenantContext);
    }

    private static LearningRuntimeService CreateService(LearningDbContext db, DateTimeOffset now)
    {
        TenantContext tenantContext = new();
        DbContextOptions<SagasDbContext> sagasOptions = new DbContextOptionsBuilder<SagasDbContext>()
            .UseInMemoryDatabase($"learning-sagas-{Guid.NewGuid()}")
            .Options;
        DbContextOptions<RequirementsDbContext> requirementsOptions = new DbContextOptionsBuilder<RequirementsDbContext>()
            .UseInMemoryDatabase($"learning-requirements-{Guid.NewGuid()}")
            .Options;

        SagasDbContext sagasDb = new(sagasOptions, tenantContext);
        RequirementsDbContext requirementsDb = new(requirementsOptions, tenantContext);
        LearningRequirementAutomationService automationService = new(sagasDb, requirementsDb, db, null!, null!, null!);
        return new LearningRuntimeService(db, new FixedTimeProvider(now), automationService);
    }

    private static void SeedActiveEnrollment(LearningDbContext db, DateTimeOffset now, out Enrollment enrollment, out CourseVersion version) =>
        SeedActiveEnrollment(db, now, out enrollment, out version, out _);

    private static void SeedActiveEnrollment(LearningDbContext db, DateTimeOffset now, out Enrollment enrollment, out CourseVersion version, out CourseSco sco)
    {
        Result<Course, CourseErrors> courseCreate = Course.Create("safety-101", "Safety 101", null, now);
        courseCreate.IsSuccess(out Course course);
        version = CourseVersion.Create(course.Id, 1, course.Title, ScormVersion.Scorm2004, true, "learning/courses/safety-101", "ABC", now);
        course.SetCurrentVersion(version.Id, now);
        sco = CourseSco.Create(version.Id, "sco-1", "Intro", "index.html", "index.html", 0, 80m);
        enrollment = Enrollment.Create(course.Id, Guid.NewGuid(), Guid.NewGuid(), now);

        db.Courses.Add(course);
        db.CourseVersions.Add(version);
        db.CourseScos.Add(sco);
        db.Enrollments.Add(enrollment);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
