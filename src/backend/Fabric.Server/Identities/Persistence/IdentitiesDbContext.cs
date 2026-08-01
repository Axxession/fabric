using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence.Configuration;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Identities.Persistence;

public class IdentitiesDbContext : TenantDbContext
{
    public const string Schema = "identities";

    public DbSet<Identity> Identities { get; set; } = null!;
    public DbSet<EmployeeAffiliation> EmployeeAffiliations { get; set; } = null!;
    public DbSet<ContractorAffiliation> ContractorAffiliations { get; set; } = null!;
    public DbSet<VisitorAffiliation> VisitorAffiliations { get; set; } = null!;

    public IdentitiesDbContext(DbContextOptions<IdentitiesDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public IdentitiesDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new IdentityConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeAffiliationConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorAffiliationConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorAffiliationConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
