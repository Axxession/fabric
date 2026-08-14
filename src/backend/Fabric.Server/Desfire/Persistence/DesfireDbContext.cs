using Fabric.Server.Desfire.Domain;
using Fabric.Server.Desfire.Persistence.Configuration;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Desfire.Persistence;

public sealed class DesfireDbContext : TenantDbContext
{
    public const string Schema = "desfire";

    public DbSet<ChipDesign> ChipDesigns { get; set; } = null!;
    public DbSet<Transformation> Transformations { get; set; } = null!;
    public DbSet<KeyDiversificationStrategyEntity> KeyDiversificationStrategies { get; set; } = null!;
    public DbSet<KeyGroup> KeyGroups { get; set; } = null!;
    public DbSet<BadgeBatch> BadgeBatches { get; set; } = null!;
    public DbSet<BadgeJob> BadgeJobs { get; set; } = null!;
    public DbSet<DesfireDeviceLease> DeviceLeases { get; set; } = null!;
    public DbSet<DesfireVariableSequence> VariableSequences { get; set; } = null!;
    public DbSet<DesfireSystemProvider> SystemProviders { get; set; } = null!;
    public DbSet<DesfireEncoder> Encoders { get; set; } = null!;

    public DesfireDbContext(DbContextOptions<DesfireDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public DesfireDbContext()
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new ChipDesignConfiguration());
        modelBuilder.ApplyConfiguration(new TransformationConfiguration());
        modelBuilder.ApplyConfiguration(new KeyDiversificationStrategyConfiguration());
        modelBuilder.ApplyConfiguration(new KeyGroupConfiguration());
        modelBuilder.ApplyConfiguration(new BadgeBatchConfiguration());
        modelBuilder.ApplyConfiguration(new BadgeJobConfiguration());
        modelBuilder.ApplyConfiguration(new DesfireDeviceLeaseConfiguration());
        modelBuilder.ApplyConfiguration(new DesfireVariableSequenceConfiguration());
        modelBuilder.ApplyConfiguration(new DesfireSystemProviderConfiguration());
        modelBuilder.ApplyConfiguration(new DesfireEncoderConfiguration());
        ApplyTenantFilters(modelBuilder);
    }
}
