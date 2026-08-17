using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Requirements.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Requirements.Persistence;

public sealed class RequirementsDbContext : TenantDbContext
{
    public const string Schema = "requirements";

    public DbSet<RequirementDefinition> RequirementDefinitions { get; set; } = null!;
    public DbSet<LocationRequirementPolicy> LocationRequirementPolicies { get; set; } = null!;
    public DbSet<LocationJobRequirementPolicy> LocationJobRequirementPolicies { get; set; } = null!;
    public DbSet<RequirementEvidence> RequirementEvidence { get; set; } = null!;

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
        modelBuilder.ApplyConfiguration(new RequirementDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new LocationRequirementPolicyConfiguration());
        modelBuilder.ApplyConfiguration(new LocationJobRequirementPolicyConfiguration());
        modelBuilder.ApplyConfiguration(new RequirementEvidenceConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
