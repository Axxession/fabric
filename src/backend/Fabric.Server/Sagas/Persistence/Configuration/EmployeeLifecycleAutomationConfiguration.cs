using Fabric.Server.Sagas.EmployeeLifecycle;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Sagas.Persistence.Configuration;

public sealed class OrganizationalUnitPackageRuleConfiguration : IEntityTypeConfiguration<OrganizationalUnitPackageRule>
{
    public void Configure(EntityTypeBuilder<OrganizationalUnitPackageRule> builder)
    {
        builder.ToTable("employee_lifecycle_ou_package_rules");
        builder.HasKey(item => item.Id).HasName("pk_employee_lifecycle_ou_package_rules");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.OrganizationUnitId).HasColumnName("organization_unit_id").IsRequired();
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(OrganizationalUnitPackageRule.OrganizationUnitId), nameof(OrganizationalUnitPackageRule.PackageId)).IsUnique().HasDatabaseName("ix_employee_lifecycle_ou_package_rules_tenant_id_ou_package_id");
    }
}

public sealed class PersonaPackageRuleConfiguration : IEntityTypeConfiguration<PersonaPackageRule>
{
    public void Configure(EntityTypeBuilder<PersonaPackageRule> builder)
    {
        builder.ToTable("employee_lifecycle_persona_package_rules");
        builder.HasKey(item => item.Id).HasName("pk_employee_lifecycle_persona_package_rules");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.PersonaId).HasColumnName("persona_id").IsRequired();
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(PersonaPackageRule.PersonaId), nameof(PersonaPackageRule.PackageId)).IsUnique().HasDatabaseName("ix_employee_lifecycle_persona_package_rules_tenant_id_persona_package_id");
    }
}

public sealed class EmployeeLifecycleAutomationSettingsConfiguration : IEntityTypeConfiguration<EmployeeLifecycleAutomationSettings>
{
    public void Configure(EntityTypeBuilder<EmployeeLifecycleAutomationSettings> builder)
    {
        builder.ToTable("employee_lifecycle_automation_settings");
        builder.HasKey(item => item.Id).HasName("pk_employee_lifecycle_automation_settings");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(item => item.DisableEmployeeOnLeave).HasColumnName("disable_employee_on_leave").IsRequired();
        builder.Property(item => item.DisabledAt).HasColumnName("disabled_at");
        builder.Property(item => item.ReenabledAt).HasColumnName("reenabled_at");
        builder.Property(item => item.LastFullReconciledAt).HasColumnName("last_full_reconciled_at");
        TenantDbContext.ConfigureTenantProperty(builder);
    }
}

public sealed class EmployeeAccessAutomationReconciliationConfiguration : IEntityTypeConfiguration<EmployeeAccessAutomationReconciliation>
{
    public void Configure(EntityTypeBuilder<EmployeeAccessAutomationReconciliation> builder)
    {
        builder.ToTable("employee_access_automation_reconciliations");
        builder.HasKey(item => item.Id).HasName("pk_employee_access_automation_reconciliations");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EmployeeId).HasColumnName("employee_id").IsRequired();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
        builder.Property(item => item.ScheduledFor).HasColumnName("scheduled_for").IsRequired();
        builder.Property(item => item.LastRetryAt).HasColumnName("last_retry_at");
        builder.Property(item => item.LastKnownError).HasColumnName("last_known_error").HasMaxLength(2000);
        builder.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();
        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex("TenantId", nameof(EmployeeAccessAutomationReconciliation.EmployeeId)).IsUnique().HasDatabaseName("ix_employee_access_automation_reconciliations_tenant_id_employee_id");
        builder.HasIndex("TenantId", nameof(EmployeeAccessAutomationReconciliation.ScheduledFor)).HasDatabaseName("ix_employee_access_automation_reconciliations_tenant_id_scheduled_for");
    }
}
