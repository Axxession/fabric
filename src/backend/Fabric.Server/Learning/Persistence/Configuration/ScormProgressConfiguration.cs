using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class ScormProgressConfiguration : IEntityTypeConfiguration<ScormProgress>
{
    public void Configure(EntityTypeBuilder<ScormProgress> builder)
    {
        builder.ToTable("scorm_progress");
        builder.HasKey(item => item.Id).HasName("pk_scorm_progress");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AttemptId).HasColumnName("attempt_id").IsRequired();
        builder.Property(item => item.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(item => item.CourseVersionId).HasColumnName("course_version_id").IsRequired();
        builder.Property(item => item.ScoId).HasColumnName("sco_id");
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.ScormVersion).HasColumnName("scorm_version").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.CompletionStatus).HasColumnName("completion_status").HasMaxLength(64);
        builder.Property(item => item.SuccessStatus).HasColumnName("success_status").HasMaxLength(64);
        builder.Property(item => item.Score).HasColumnName("score").HasPrecision(10, 2);
        builder.Property(item => item.ScoreScaled).HasColumnName("score_scaled").HasPrecision(10, 4);
        builder.Property(item => item.BookmarkLocation).HasColumnName("bookmark_location").HasMaxLength(4_000);
        builder.Property(item => item.SessionTime).HasColumnName("session_time").HasMaxLength(200);
        builder.Property(item => item.SuspendData).HasColumnName("suspend_data").HasMaxLength(32_000);
        builder.Property(item => item.RawCmiData).HasColumnName("raw_cmi_data").HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.LastCommittedAt).HasColumnName("last_committed_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ScormProgress.AttemptId), nameof(ScormProgress.ScoId)).HasDatabaseName("ix_scorm_progress_tenant_id_attempt_id_sco_id");
    }
}
