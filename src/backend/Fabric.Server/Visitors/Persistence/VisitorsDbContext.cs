using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Visitors.Domain;
using Fabric.Server.Visitors.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Visitors.Persistence;

public class VisitorsDbContext : TenantDbContext
{
    public const string Schema = "visitors";
    public DbSet<Visit> Visits { get; set; } = null!;
    public DbSet<VisitInvitation> Invitations { get; set; } = null!;
    public DbSet<Visitor> Visitors { get; set; } = null!;
    public DbSet<HostAssignment> HostAssignments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new VisitConfiguration());
        modelBuilder.ApplyConfiguration(new VisitInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new VisitorConfiguration());
        modelBuilder.ApplyConfiguration(new HostAssignmentConfiguration());
        ApplyTenantFilters(modelBuilder);
    }

    public VisitorsDbContext(DbContextOptions<VisitorsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public VisitorsDbContext()
    {
    }

}
