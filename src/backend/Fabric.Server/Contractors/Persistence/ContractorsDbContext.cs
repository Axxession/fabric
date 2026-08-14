using Fabric.Server.Contractors.Domain;
using Fabric.Server.Contractors.Persistence.Configuration;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Contractors.Persistence;

public sealed class ContractorsDbContext : TenantDbContext
{
    public const string Schema = "contractors";

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Contractor> Contractors { get; set; } = null!;
    public DbSet<ContractorJob> ContractorJobs { get; set; } = null!;
    public DbSet<ContractorJobAssignment> ContractorJobAssignments { get; set; } = null!;
    public DbSet<JobType> JobTypes { get; set; } = null!;

    public ContractorsDbContext(DbContextOptions<ContractorsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public ContractorsDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorJobConfiguration());
        modelBuilder.ApplyConfiguration(new ContractorJobAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new JobTypeConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
