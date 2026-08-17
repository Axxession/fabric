using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence.Configuration;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessCatalog.Persistence;

public class AccessCatalogDbContext : TenantDbContext
{
    public const string Schema = "access_catalog";

    public DbSet<Catalog> Catalogs { get; set; } = null!;
    public DbSet<CatalogPackage> CatalogPackages { get; set; } = null!;
    public DbSet<Package> Packages { get; set; } = null!;
    public DbSet<PackageAccessItem> PackageAccessItems { get; set; } = null!;
    public DbSet<AccessGrant> AccessGrants { get; set; } = null!;
    public DbSet<GrantRequirement> GrantRequirements { get; set; } = null!;
    public DbSet<GrantRequirementResult> GrantRequirementResults { get; set; } = null!;
    public DbSet<PackageRequest> PackageRequests { get; set; } = null!;
    public DbSet<PackageRequestLocation> PackageRequestLocations { get; set; } = null!;
    public DbSet<PackageRequestScope> PackageRequestScopes { get; set; } = null!;
    public DbSet<ApprovalGroup> ApprovalGroups { get; set; } = null!;
    public DbSet<ApprovalGroupMember> ApprovalGroupMembers { get; set; } = null!;
    public DbSet<ApprovalDefinition> ApprovalDefinitions { get; set; } = null!;
    public DbSet<ApprovalFlow> ApprovalFlows { get; set; } = null!;
    public DbSet<ApprovalRequirement> ApprovalRequirements { get; set; } = null!;
    public DbSet<ApprovalDecision> ApprovalDecisions { get; set; } = null!;

    public AccessCatalogDbContext(DbContextOptions<AccessCatalogDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public AccessCatalogDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new CatalogConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogPackageConfiguration());
        modelBuilder.ApplyConfiguration(new PackageConfiguration());
        modelBuilder.ApplyConfiguration(new PackageAccessItemConfiguration());
        modelBuilder.ApplyConfiguration(new AccessGrantConfiguration());
        modelBuilder.ApplyConfiguration(new GrantRequirementConfiguration());
        modelBuilder.ApplyConfiguration(new GrantRequirementResultConfiguration());
        modelBuilder.ApplyConfiguration(new PackageRequestConfiguration());
        modelBuilder.ApplyConfiguration(new PackageRequestLocationConfiguration());
        modelBuilder.ApplyConfiguration(new PackageRequestScopeConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalGroupConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalGroupMemberConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalFlowConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalRequirementConfiguration());
        modelBuilder.ApplyConfiguration(new ApprovalDecisionConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
