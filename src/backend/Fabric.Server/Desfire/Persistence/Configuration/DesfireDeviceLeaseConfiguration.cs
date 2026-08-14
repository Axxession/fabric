using Fabric.Server.Desfire.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Desfire.Persistence.Configuration;

public sealed class DesfireDeviceLeaseConfiguration : IEntityTypeConfiguration<DesfireDeviceLease>
{
    public void Configure(EntityTypeBuilder<DesfireDeviceLease> builder)
    {
        builder.ToTable("device_leases");
        builder.HasKey(lease => lease.Id);
        builder.Property(lease => lease.Id).ValueGeneratedNever();
        builder.Property(lease => lease.AgentId).IsRequired().HasMaxLength(100);
        builder.Property(lease => lease.DeviceId).IsRequired().HasMaxLength(100);
        builder.Property(lease => lease.BadgeJobId).IsRequired();
        builder.Property(lease => lease.AcquiredAt).IsRequired();
        builder.Property(lease => lease.ExpiresAt).IsRequired();
        builder.Property(lease => lease.ReleasedAt);
        builder.HasIndex(lease => new { lease.AgentId, lease.DeviceId });
    }
}
