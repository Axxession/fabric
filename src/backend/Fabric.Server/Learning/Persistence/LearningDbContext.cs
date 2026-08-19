using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Learning.Persistence;

public sealed class LearningDbContext : TenantDbContext
{
    public const string Schema = "learning";

    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<CourseLanguage> CourseLanguages { get; set; } = null!;
    public DbSet<CourseVersion> CourseVersions { get; set; } = null!;
    public DbSet<CourseSco> CourseScos { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Attempt> Attempts { get; set; } = null!;
    public DbSet<LaunchSession> LaunchSessions { get; set; } = null!;
    public DbSet<ScormProgress> ScormProgress { get; set; } = null!;

    public LearningDbContext(DbContextOptions<LearningDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public LearningDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
        modelBuilder.ApplyConfiguration(new CourseLanguageConfiguration());
        modelBuilder.ApplyConfiguration(new CourseVersionConfiguration());
        modelBuilder.ApplyConfiguration(new CourseScoConfiguration());
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
        modelBuilder.ApplyConfiguration(new AttemptConfiguration());
        modelBuilder.ApplyConfiguration(new LaunchSessionConfiguration());
        modelBuilder.ApplyConfiguration(new ScormProgressConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
