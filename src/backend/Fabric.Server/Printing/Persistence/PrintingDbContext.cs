using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Printing.Domain;
using Fabric.Server.Printing.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Printing.Persistence;

public sealed class PrintingDbContext : TenantDbContext
{
    public const string Schema = "printing";

    public DbSet<PrintDesign> PrintDesigns { get; set; } = null!;

    public PrintingDbContext(DbContextOptions<PrintingDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public PrintingDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new PrintDesignConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
