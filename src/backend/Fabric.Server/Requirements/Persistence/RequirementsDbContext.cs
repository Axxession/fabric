using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Persistence;

public sealed class RequirementsDbContext : TenantDbContext
{
    public const string Schema = "requirements";

    public DbSet<EnforcementZone> EnforcementZones { get; set; } = null!;
    public DbSet<EnforcementZoneLocation> EnforcementZoneLocations { get; set; } = null!;
    public DbSet<RequirementDefinition> RequirementDefinitions { get; set; } = null!;
    public DbSet<ZoneRequirementPolicy> ZoneRequirementPolicies { get; set; } = null!;
    public DbSet<ContractorJobRequirementPolicy> ContractorJobRequirementPolicies { get; set; } = null!;
    public DbSet<EnforcementZoneAccessPolicy> EnforcementZoneAccessPolicies { get; set; } = null!;
    public DbSet<RequirementEvidence> RequirementEvidence { get; set; } = null!;
    public DbSet<ZoneCompliance> ZoneCompliances { get; set; } = null!;
    public DbSet<ProjectedZoneAccessAssignment> ProjectedZoneAccessAssignments { get; set; } = null!;

    public RequirementsDbContext(DbContextOptions<RequirementsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public RequirementsDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new EnforcementZoneConfiguration());
        modelBuilder.ApplyConfiguration(new EnforcementZoneLocationConfiguration());
        modelBuilder.ApplyConfiguration(new RequirementDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ZoneRequirementPolicyConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorJobRequirementPolicyConfiguration());
        modelBuilder.ApplyConfiguration(new EnforcementZoneAccessPolicyConfiguration());
        modelBuilder.ApplyConfiguration(new RequirementEvidenceConfiguration());
        modelBuilder.ApplyConfiguration(new ZoneComplianceConfiguration());
        modelBuilder.ApplyConfiguration(new ZoneComplianceRequirementResultConfiguration());
        modelBuilder.ApplyConfiguration(new ProjectedZoneAccessAssignmentConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
