using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.ToTable("attempts");
        builder.HasKey(item => item.Id).HasName("pk_attempts");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EnrollmentId).HasColumnName("enrollment_id").IsRequired();
        builder.Property(item => item.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(item => item.CourseVersionId).HasColumnName("course_version_id").IsRequired();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(item => item.LastActivityAt).HasColumnName("last_activity_at");
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");
        builder.Property(item => item.CompletionStatus).HasColumnName("completion_status").HasMaxLength(64);
        builder.Property(item => item.SuccessStatus).HasColumnName("success_status").HasMaxLength(64);
        builder.Property(item => item.Score).HasColumnName("score").HasPrecision(10, 2);
        builder.Property(item => item.ScoreScaled).HasColumnName("score_scaled").HasPrecision(10, 4);
        builder.Property(item => item.IsScored).HasColumnName("is_scored").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Attempt.EnrollmentId), nameof(Attempt.StartedAt)).HasDatabaseName("ix_attempts_tenant_id_enrollment_id_started_at");
    }
}
