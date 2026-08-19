using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Learning.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Learning.Persistence.Configuration;

public sealed class LaunchSessionConfiguration : IEntityTypeConfiguration<LaunchSession>
{
    public void Configure(EntityTypeBuilder<LaunchSession> builder)
    {
        builder.ToTable("launch_sessions");
        builder.HasKey(item => item.Id).HasName("pk_launch_sessions");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EnrollmentId).HasColumnName("enrollment_id").IsRequired();
        builder.Property(item => item.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(item => item.CourseVersionId).HasColumnName("course_version_id").IsRequired();
        builder.Property(item => item.AttemptId).HasColumnName("attempt_id");
        builder.Property(item => item.ScoId).HasColumnName("sco_id");
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.Token).HasColumnName("token").HasMaxLength(200).IsRequired();
        builder.Property(item => item.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(LaunchSession.Token)).IsUnique().HasDatabaseName("ix_launch_sessions_tenant_id_token");
    }
}
