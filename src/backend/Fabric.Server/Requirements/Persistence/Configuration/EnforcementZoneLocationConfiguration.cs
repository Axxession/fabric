using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class EnforcementZoneLocationConfiguration : IEntityTypeConfiguration<EnforcementZoneLocation>
{
    public void Configure(EntityTypeBuilder<EnforcementZoneLocation> builder)
    {
        builder.ToTable("enforcement_zone_locations");
        builder.HasKey(item => item.Id).HasName("pk_enforcement_zone_locations");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EnforcementZoneId).HasColumnName("enforcement_zone_id").IsRequired();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<EnforcementZone>()
            .WithMany()
            .HasForeignKey(item => item.EnforcementZoneId)
            .HasConstraintName("fk_enforcement_zone_locations_enforcement_zones_enforcement_zone_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EnforcementZoneLocation.LocationId)).IsUnique().HasDatabaseName("ix_enforcement_zone_locations_tenant_id_location_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EnforcementZoneLocation.EnforcementZoneId), nameof(EnforcementZoneLocation.LocationId)).IsUnique().HasDatabaseName("ix_enforcement_zone_locations_tenant_id_zone_location");
    }
}
