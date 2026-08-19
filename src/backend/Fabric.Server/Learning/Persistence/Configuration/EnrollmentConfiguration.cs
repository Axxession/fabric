using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");
        builder.HasKey(item => item.Id).HasName("pk_enrollments");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.AssignedAt).HasColumnName("assigned_at").IsRequired();
        builder.Property(item => item.AssignedByIdentityId).HasColumnName("assigned_by_identity_id").IsRequired();
        builder.Property(item => item.StartedAt).HasColumnName("started_at");
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");
        builder.Property(item => item.CompletedAttemptId).HasColumnName("completed_attempt_id");
        builder.Property(item => item.LatestAttemptId).HasColumnName("latest_attempt_id");
        builder.Property(item => item.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(item => item.CancelledByIdentityId).HasColumnName("cancelled_by_identity_id");
        builder.Property(item => item.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(2_000);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(Enrollment.CourseId), nameof(Enrollment.IdentityId), nameof(Enrollment.Status)).HasDatabaseName("ix_enrollments_tenant_id_course_id_identity_id_status");
    }
}
