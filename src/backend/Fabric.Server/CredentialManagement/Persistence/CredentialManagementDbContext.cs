using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.CredentialManagement.Persistence.Configuration;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.CredentialManagement.Persistence;

public sealed class CredentialManagementDbContext : TenantDbContext
{
    public const string Schema = "credential_management";

    public DbSet<CredentialType> CredentialTypes { get; set; } = null!;
    public DbSet<CredentialRange> CredentialRanges { get; set; } = null!;
    public DbSet<CredentialSlot> CredentialSlots { get; set; } = null!;
    public DbSet<Credential> Credentials { get; set; } = null!;

    public CredentialManagementDbContext(DbContextOptions<CredentialManagementDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public CredentialManagementDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new CredentialTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CredentialRangeConfiguration());
        modelBuilder.ApplyConfiguration(new CredentialSlotConfiguration());
        modelBuilder.ApplyConfiguration(new CredentialConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
