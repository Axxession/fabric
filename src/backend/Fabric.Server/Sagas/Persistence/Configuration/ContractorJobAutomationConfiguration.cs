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

public sealed class ContractorJobOnboardingReconciliationConfiguration : IEntityTypeConfiguration<ContractorJobOnboardingReconciliation>
{
    public void Configure(EntityTypeBuilder<ContractorJobOnboardingReconciliation> builder)
    {
        builder.ToTable("contractor_job_onboarding_reconciliations");
        builder.HasKey(item => item.Id).HasName("pk_contractor_job_onboarding_reconciliations");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AssignmentId).HasColumnName("assignment_id").IsRequired();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(item => item.LastKnownError).HasColumnName("last_known_error").HasMaxLength(2000);
        builder.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(ContractorJobOnboardingReconciliation.AssignmentId))
            .IsUnique()
            .HasDatabaseName("ix_contractor_job_onboarding_reconciliations_tenant_id_assignment_id");
        builder.HasIndex("TenantId", nameof(ContractorJobOnboardingReconciliation.ScheduledFor))
            .HasDatabaseName("ix_contractor_job_onboarding_reconciliations_tenant_id_scheduled_for");
    }
}

public sealed class ContractorJobAccessAutomationReconciliationConfiguration : IEntityTypeConfiguration<ContractorJobAccessAutomationReconciliation>
{
    public void Configure(EntityTypeBuilder<ContractorJobAccessAutomationReconciliation> builder)
    {
        builder.ToTable("contractor_job_access_automation_reconciliations");
        builder.HasKey(item => item.Id).HasName("pk_contractor_job_access_automation_reconciliations");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AssignmentId).HasColumnName("assignment_id").IsRequired();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(item => item.LastKnownError).HasColumnName("last_known_error").HasMaxLength(2000);
        builder.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(ContractorJobAccessAutomationReconciliation.AssignmentId))
            .IsUnique()
            .HasDatabaseName("ix_contractor_job_access_automation_reconciliations_tenant_id_assignment_id");
        builder.HasIndex("TenantId", nameof(ContractorJobAccessAutomationReconciliation.ScheduledFor))
            .HasDatabaseName("ix_contractor_job_access_automation_reconciliations_tenant_id_scheduled_for");
    }
}
