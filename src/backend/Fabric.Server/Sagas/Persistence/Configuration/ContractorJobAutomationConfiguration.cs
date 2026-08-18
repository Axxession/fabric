using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Sagas.ContractorJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class ContractorJobPackageRuleConfiguration : IEntityTypeConfiguration<ContractorJobPackageRule>
{
    public void Configure(EntityTypeBuilder<ContractorJobPackageRule> builder)
    {
        builder.ToTable("contractor_job_package_rules");
        builder.HasKey(item => item.Id).HasName("pk_contractor_job_package_rules");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.JobTypeId).HasColumnName("job_type_id").IsRequired();
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.LocationId).HasColumnName("location_id");
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(ContractorJobPackageRule.JobTypeId), nameof(ContractorJobPackageRule.PackageId), nameof(ContractorJobPackageRule.LocationId))
            .IsUnique()
            .HasDatabaseName("ix_contractor_job_package_rules_tenant_id_job_type_package_location");
    }
}

public sealed class ContractorAssignmentAutomationMailboxConfiguration : IEntityTypeConfiguration<ContractorAssignmentAutomationMailbox>
{
    public void Configure(EntityTypeBuilder<ContractorAssignmentAutomationMailbox> builder)
    {
        builder.ToTable("contractor_assignment_automation_mailboxes");
        builder.HasKey(item => item.Id).HasName("pk_contractor_assignment_automation_mailboxes");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AssignmentId).HasColumnName("assignment_id").IsRequired();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(item => item.LastKnownError).HasColumnName("last_known_error").HasMaxLength(2000);
        builder.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(item => item.LeaseOwner).HasColumnName("lease_owner").HasMaxLength(200);
        builder.Property(item => item.LeaseUntil).HasColumnName("lease_until");
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(ContractorAssignmentAutomationMailbox.AssignmentId))
            .IsUnique()
            .HasDatabaseName("ix_contractor_assignment_automation_mailboxes_tenant_id_assignment_id");
        builder.HasIndex("TenantId", nameof(ContractorAssignmentAutomationMailbox.ScheduledFor))
            .HasDatabaseName("ix_contractor_assignment_automation_mailboxes_tenant_id_scheduled_for");
        builder.HasIndex("TenantId", nameof(ContractorAssignmentAutomationMailbox.LeaseUntil))
            .HasDatabaseName("ix_contractor_assignment_automation_mailboxes_tenant_id_lease_until");
    }
}
