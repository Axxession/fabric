using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence.Configuration;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Persistence;

public class AccessControlDbContext : TenantDbContext
{
    public const string Schema = "access_control";

    public DbSet<AccessControlSystem> AccessControlSystems { get; set; } = null!;
    public DbSet<AccessControlSystemLocation> AccessControlSystemLocations { get; set; } = null!;
    public DbSet<AccessItem> AccessItems { get; set; } = null!;
    public DbSet<AccessLevelTarget> AccessLevelTargets { get; set; } = null!;
    public DbSet<CredentialTypeTarget> CredentialTypeTargets { get; set; } = null!;
    public DbSet<CredentialPACSAssignment> CredentialPACSAssignments { get; set; } = null!;
    public DbSet<PACSSubject> PACSSubjects { get; set; } = null!;
    public DbSet<PACSAssignment> PACSAssignments { get; set; } = null!;
    public DbSet<PACSProvisioning> PACSProvisionings { get; set; } = null!;
    public DbSet<PACSProvisioningSourceAssignment> PACSProvisioningSourceAssignments { get; set; } = null!;
    public DbSet<PACSProvisioningReconciliation> PACSProvisioningReconciliations { get; set; } = null!;
    public DbSet<PACSSubjectProvisioning> PACSSubjectProvisionings { get; set; } = null!;

    public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public AccessControlDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new AccessControlSystemConfiguration());
        modelBuilder.ApplyConfiguration(new AccessControlSystemLocationConfiguration());
        modelBuilder.ApplyConfiguration(new AccessItemConfiguration());
        modelBuilder.ApplyConfiguration(new AccessLevelTargetConfiguration());
        modelBuilder.ApplyConfiguration(new UnipassAccessLevelTargetConfiguration());
        modelBuilder.ApplyConfiguration(new CredentialTypeTargetConfiguration());
        modelBuilder.ApplyConfiguration(new CredentialPACSAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new PACSSubjectConfiguration());
        modelBuilder.ApplyConfiguration(new PACSAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new PACSProvisioningConfiguration());
        modelBuilder.ApplyConfiguration(new PACSProvisioningSourceAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new PACSProvisioningReconciliationConfiguration());
        modelBuilder.ApplyConfiguration(new PACSSubjectProvisioningConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
