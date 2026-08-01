using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class PACSSubjectProvisioningConfiguration : IEntityTypeConfiguration<PACSSubjectProvisioning>
{
    public void Configure(EntityTypeBuilder<PACSSubjectProvisioning> builder)
    {
        builder.ToTable("pacs_subject_provisionings");

        builder.HasKey(item => item.Id).HasName("pk_pacs_subject_provisionings");

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.PACSSubjectId).HasColumnName("pacs_subject_id").IsRequired();
        builder.Property(item => item.DesiredState).HasColumnName("desired_state").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.DesiredFirstName).HasColumnName("desired_first_name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.DesiredLastName).HasColumnName("desired_last_name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.DesiredEmail).HasColumnName("desired_email").HasMaxLength(320);
        builder.Property(item => item.Reason).HasColumnName("reason").HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(item => item.SourceKind).HasColumnName("source_kind").HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(item => item.SourceId).HasColumnName("source_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(item => item.LastKnownError).HasColumnName("last_known_error").HasMaxLength(2_000);
        builder.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<PACSSubject>()
            .WithMany()
            .HasForeignKey(item => item.PACSSubjectId)
            .HasConstraintName("fk_pacs_subject_provisionings_pacs_subjects_pacs_subject_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSSubjectProvisioning.PACSSubjectId))
            .IsUnique()
            .HasDatabaseName("ix_pacs_subject_provisionings_tenant_id_pacs_subject_id");

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSSubjectProvisioning.Status), nameof(PACSSubjectProvisioning.ScheduledFor))
            .HasDatabaseName("ix_pacs_subject_provisionings_tenant_id_status_scheduled_for");
    }
}
