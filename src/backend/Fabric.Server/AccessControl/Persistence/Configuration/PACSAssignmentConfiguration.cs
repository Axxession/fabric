using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class PACSAssignmentConfiguration : IEntityTypeConfiguration<PACSAssignment>
{
    public void Configure(EntityTypeBuilder<PACSAssignment> builder)
    {
        builder.ToTable("pacs_assignments");

        builder.HasKey(assignment => assignment.Id).HasName("pk_pacs_assignments");

        builder.Property(assignment => assignment.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(assignment => assignment.SourceAssignmentId).HasColumnName("source_assignment_id").IsRequired();
        builder.Property(assignment => assignment.AccessLevelTargetId).HasColumnName("access_level_target_id").IsRequired();
        builder.Property(assignment => assignment.AccessControlSystemId).HasColumnName("access_control_system_id").IsRequired();
        builder.Property(assignment => assignment.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(assignment => assignment.DurationKind).HasColumnName("duration_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(assignment => assignment.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(assignment => assignment.ValidUntil).HasColumnName("valid_until");
        builder.Property(assignment => assignment.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(assignment => assignment.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(assignment => assignment.NativeAssignmentId).HasColumnName("native_assignment_id").HasMaxLength(200);
        builder.Property(assignment => assignment.FailureReason).HasColumnName("failure_reason").HasMaxLength(2_000);
        builder.Property(assignment => assignment.ProvisionedAt).HasColumnName("provisioned_at");
        builder.Property(assignment => assignment.CompletedAt).HasColumnName("completed_at");

        builder.HasOne<AccessLevelTarget>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AccessLevelTargetId)
            .HasConstraintName("fk_pacs_assignments_access_level_targets_access_level_target_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AccessControlSystem>()
            .WithMany()
            .HasForeignKey(assignment => assignment.AccessControlSystemId)
            .HasConstraintName("fk_pacs_assignments_access_control_systems_access_control_system_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSAssignment.IdentityId), nameof(PACSAssignment.Status))
            .HasDatabaseName("ix_pacs_assignments_tenant_id_identity_id_status");

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSAssignment.AccessControlSystemId), nameof(PACSAssignment.Status))
            .HasDatabaseName("ix_pacs_assignments_tenant_id_system_id_status");

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PACSAssignment.SourceAssignmentId), nameof(PACSAssignment.AccessLevelTargetId), nameof(PACSAssignment.IdentityId))
            .IsUnique()
            .HasDatabaseName("ix_pacs_assignments_tenant_id_source_target_identity");
    }
}
