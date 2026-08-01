using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class PACSProvisioningConfiguration : IEntityTypeConfiguration<PACSProvisioning>
{
    public void Configure(EntityTypeBuilder<PACSProvisioning> builder)
    {
        builder.ToTable("pacs_provisionings");
        builder.HasKey(item => item.Id).HasName("pk_pacs_provisionings");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AccessLevelTargetId).HasColumnName("access_level_target_id").IsRequired();
        builder.Property(item => item.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.DurationKind).HasColumnName("duration_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(item => item.ValidUntil).HasColumnName("valid_until");
        builder.Property(item => item.ProvisioningTiming).HasColumnName("provisioning_timing").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.NativeAssignmentId).HasColumnName("native_assignment_id").HasMaxLength(200);
        builder.Property(item => item.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);
        builder.Property(item => item.ProvisionedAt).HasColumnName("provisioned_at");
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");

        builder.HasOne<AccessLevelTarget>()
            .WithMany()
            .HasForeignKey(item => item.AccessLevelTargetId)
            .HasConstraintName("fk_pacs_provisionings_access_level_targets_access_level_target_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AccessControlSystem>()
            .WithMany()
            .HasForeignKey(item => item.AccessControlSystemId)
            .HasConstraintName("fk_pacs_provisionings_access_control_systems_access_control_system_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSProvisioning.IdentityId), nameof(PACSProvisioning.AccessControlSystemId), nameof(PACSProvisioning.AccessLevelTargetId), nameof(PACSProvisioning.Status))
            .HasDatabaseName("ix_pacs_provisionings_tenant_id_identity_system_target_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSProvisioning.Status), nameof(PACSProvisioning.ScheduledFor))
            .HasDatabaseName("ix_pacs_provisionings_tenant_id_status_scheduled_for");
    }
}

public sealed class PACSProvisioningSourceAssignmentConfiguration : IEntityTypeConfiguration<PACSProvisioningSourceAssignment>
{
    public void Configure(EntityTypeBuilder<PACSProvisioningSourceAssignment> builder)
    {
        builder.ToTable("pacs_provisioning_source_assignments");
        builder.HasKey(item => new { item.PACSProvisioningId, item.PACSAssignmentId }).HasName("pk_pacs_provisioning_source_assignments");
        builder.Property(item => item.PACSProvisioningId).HasColumnName("pacs_provisioning_id").IsRequired();
        builder.Property(item => item.PACSAssignmentId).HasColumnName("pacs_assignment_id").IsRequired();

        builder.HasOne<PACSProvisioning>()
            .WithMany()
            .HasForeignKey(item => item.PACSProvisioningId)
            .HasConstraintName("fk_pacs_provisioning_source_assignments_pacs_provisionings_pacs_provisioning_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PACSAssignment>()
            .WithMany()
            .HasForeignKey(item => item.PACSAssignmentId)
            .HasConstraintName("fk_pacs_provisioning_source_assignments_pacs_assignments_pacs_assignment_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSProvisioningSourceAssignment.PACSAssignmentId))
            .HasDatabaseName("ix_pacs_provisioning_source_assignments_tenant_id_pacs_assignment_id");
    }
}

public sealed class PACSProvisioningReconciliationConfiguration : IEntityTypeConfiguration<PACSProvisioningReconciliation>
{
    public void Configure(EntityTypeBuilder<PACSProvisioningReconciliation> builder)
    {
        builder.ToTable("pacs_provisioning_reconciliations");
        builder.HasKey(item => item.Id).HasName("pk_pacs_provisioning_reconciliations");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(item => item.LastKnownError).HasColumnName("last_known_error").HasMaxLength(2_000);
        builder.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<AccessControlSystem>()
            .WithMany()
            .HasForeignKey(item => item.AccessControlSystemId)
            .HasConstraintName("fk_pacs_provisioning_reconciliations_access_control_systems_access_control_system_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSProvisioningReconciliation.IdentityId), nameof(PACSProvisioningReconciliation.AccessControlSystemId))
            .IsUnique()
            .HasDatabaseName("ix_pacs_provisioning_reconciliations_tenant_id_identity_system");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSProvisioningReconciliation.ScheduledFor))
            .HasDatabaseName("ix_pacs_provisioning_reconciliations_tenant_id_scheduled_for");
    }
}
