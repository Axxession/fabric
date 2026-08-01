using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class CredentialTypeTargetConfiguration : IEntityTypeConfiguration<CredentialTypeTarget>
{
    public void Configure(EntityTypeBuilder<CredentialTypeTarget> builder)
    {
        builder.ToTable("credential_type_targets");
        builder.HasKey(target => target.Id).HasName("pk_credential_type_targets");

        builder.Property(target => target.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(target => target.CredentialTypeId).HasColumnName("credential_type_id").IsRequired();
        builder.Property(target => target.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(target => target.ProvisioningTiming).HasColumnName("provisioning_timing").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(target => target.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(target => target.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(target => target.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasDiscriminator<string>("target_type")
            .HasValue<UnipassCredentialTypeTarget>("unipass");

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialTypeTarget.CredentialTypeId), nameof(CredentialTypeTarget.AccessControlSystemId))
            .IsUnique()
            .HasDatabaseName("ix_credential_type_targets_tenant_id_type_system");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialTypeTarget.AccessControlSystemId))
            .HasDatabaseName("ix_credential_type_targets_tenant_id_system");
    }
}

public sealed class CredentialPACSAssignmentConfiguration : IEntityTypeConfiguration<CredentialPACSAssignment>
{
    public void Configure(EntityTypeBuilder<CredentialPACSAssignment> builder)
    {
        builder.ToTable("credential_pacs_assignments");
        builder.HasKey(item => item.Id).HasName("pk_credential_pacs_assignments");

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.CredentialId).HasColumnName("credential_id").IsRequired();
        builder.Property(item => item.CredentialTypeTargetId).HasColumnName("credential_type_target_id").IsRequired();
        builder.Property(item => item.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(item => item.LastAttemptAt).HasColumnName("last_attempt_at");
        builder.Property(item => item.NativeAssignmentId).HasColumnName("native_assignment_id").HasMaxLength(200);
        builder.Property(item => item.ProvisionedAt).HasColumnName("provisioned_at");
        builder.Property(item => item.RevokedAt).HasColumnName("revoked_at");
        builder.Property(item => item.FailureReasonCode).HasColumnName("failure_reason_code").HasMaxLength(200);
        builder.Property(item => item.ErrorMessage).HasColumnName("error_message").HasMaxLength(2000);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<CredentialTypeTarget>()
            .WithMany()
            .HasForeignKey(item => item.CredentialTypeTargetId)
            .HasConstraintName("fk_credential_pacs_assignments_credential_type_targets_credential_type_target_id")
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialPACSAssignment.CredentialId))
            .HasDatabaseName("ix_credential_pacs_assignments_tenant_id_credential_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialPACSAssignment.AccessControlSystemId), nameof(CredentialPACSAssignment.Status))
            .HasDatabaseName("ix_credential_pacs_assignments_tenant_id_system_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CredentialPACSAssignment.ScheduledFor))
            .HasDatabaseName("ix_credential_pacs_assignments_tenant_id_scheduled_for");
    }
}
