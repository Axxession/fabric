using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tests.Learning.Application;

public sealed class EnrollmentServiceTests
{
    [Fact]
    public async Task CreateEnrollmentAsync_WhenActiveEnrollmentExists_ReturnsFailure()
    {
        DateTimeOffset now = new(2026, 8, 19, 8, 0, 0, TimeSpan.Zero);

        await using LearningDbContext learningDb = CreateLearningDbContext();
        await using IdentitiesDbContext identitiesDb = CreateIdentitiesDbContext();

        Result<Identity, IdentityErrors> identityCreate = Identity.Create("Ada", null, "Lovelace", null, "ada@example.com", null, now);
        identityCreate.IsSuccess(out Identity identity);
        identitiesDb.Identities.Add(identity);

        Result<Course, CourseErrors> courseCreate = Course.Create("safety-101", "Safety 101", null, now);
        courseCreate.IsSuccess(out Course course);
        learningDb.Courses.Add(course);
        learningDb.Enrollments.Add(Enrollment.Create(course.Id, identity.Id, identity.Id, now));

        await identitiesDb.SaveChangesAsync();
        await learningDb.SaveChangesAsync();

        EnrollmentService service = new(learningDb, identitiesDb, new FixedTimeProvider(now));

        Result<Enrollment, EnrollmentErrors> result = await service.CreateEnrollmentAsync(new CreateEnrollmentRequest(course.Id, identity.Id), identity.Id);

        Assert.True(result.IsFailure(out EnrollmentErrors error));
        Assert.Equal(EnrollmentErrors.ActiveEnrollmentAlreadyExists, error);
    }

    private static LearningDbContext CreateLearningDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<LearningDbContext> options = new DbContextOptionsBuilder<LearningDbContext>()
            .UseInMemoryDatabase($"learning-{Guid.NewGuid()}")
            .Options;
        return new LearningDbContext(options, tenantContext);
    }

    private static IdentitiesDbContext CreateIdentitiesDbContext()
    {
        TenantContext tenantContext = new();
        DbContextOptions<IdentitiesDbContext> options = new DbContextOptionsBuilder<IdentitiesDbContext>()
            .UseInMemoryDatabase($"identities-{Guid.NewGuid()}")
            .Options;
        return new IdentitiesDbContext(options, tenantContext);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
