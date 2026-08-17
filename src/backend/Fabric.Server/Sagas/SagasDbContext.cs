using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Sagas.AccessGrantProvisioning;
using Fabric.Server.Sagas.ContractorJobs;
using Fabric.Server.Sagas.EmployeeLifecycle;
using Fabric.Server.Sagas.Kiosk;
using Fabric.Server.Sagas.Persistence.Configuration;
using Fabric.Server.Sagas.VisitorPreOnboarding;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas;

public class SagasDbContext : TenantDbContext
{
    public const string Schema = "sagas";
    public DbSet<VisitorPreOnboardingSaga> VisitorPreOnboardingSagas { get; set; } = null!;
    public DbSet<VisitorPreOnboardingSagaConfig> VisitorPreOnboardingSagaConfigs { get; set; } = null!;
    public DbSet<VisitorPreOnboardingSagaEvent> VisitorPreOnboardingSagaEvents { get; set; } = null!;
    public DbSet<VisitorPreOnboardingSagaAuditEntry> VisitorPreOnboardingSagaAuditEntries { get; set; } = null!;
    public DbSet<AccessGrantProvisioningSaga> AccessGrantProvisioningSagas { get; set; } = null!;
    public DbSet<AccessGrantMaterializationOutcome> AccessGrantMaterializationOutcomes { get; set; } = null!;
    public DbSet<AccessGrantProvisioningSagaEvent> AccessGrantProvisioningSagaEvents { get; set; } = null!;
    public DbSet<OrganizationalUnitPackageRule> OrganizationalUnitPackageRules { get; set; } = null!;
    public DbSet<PersonaPackageRule> PersonaPackageRules { get; set; } = null!;
    public DbSet<ContractorJobPackageRule> ContractorJobPackageRules { get; set; } = null!;
    public DbSet<ContractorJobOnboardingReconciliation> ContractorJobOnboardingReconciliations { get; set; } = null!;
    public DbSet<ContractorJobAccessAutomationReconciliation> ContractorJobAccessAutomationReconciliations { get; set; } = null!;
    public DbSet<EmployeeLifecycleAutomationSettings> EmployeeLifecycleAutomationSettings { get; set; } = null!;
    public DbSet<EmployeeAccessAutomationReconciliation> EmployeeAccessAutomationReconciliations { get; set; } = null!;
    public DbSet<KioskSaga> KioskSagas { get; set; } = null!;
    public DbSet<KioskSagaEvent> KioskSagaEvents { get; set; } = null!;

    public SagasDbContext(DbContextOptions<SagasDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public SagasDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new VisitorPreOnboardingSagaConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorPreOnboardingSagaConfigConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorPreOnboardingSagaEventConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorPreOnboardingSagaAuditEntryConfiguration());
        modelBuilder.ApplyConfiguration(new AccessGrantProvisioningSagaConfiguration());
        modelBuilder.ApplyConfiguration(new AccessGrantMaterializationOutcomeConfiguration());
        modelBuilder.ApplyConfiguration(new AccessGrantProvisioningSagaEventConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationalUnitPackageRuleConfiguration());
        modelBuilder.ApplyConfiguration(new PersonaPackageRuleConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorJobPackageRuleConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorJobOnboardingReconciliationConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorJobAccessAutomationReconciliationConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeLifecycleAutomationSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeAccessAutomationReconciliationConfiguration());
        modelBuilder.ApplyConfiguration(new KioskSagaConfiguration());
        modelBuilder.ApplyConfiguration(new KioskSagaEventConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
