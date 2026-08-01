using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class PackageRequestLocationConfiguration : IEntityTypeConfiguration<PackageRequestLocation>
{
    public void Configure(EntityTypeBuilder<PackageRequestLocation> builder)
    {
        builder.ToTable("package_request_locations");
        builder.HasKey(item => new { item.RequestId, item.LocationId }).HasName("pk_package_request_locations");
        builder.Property(item => item.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();

        builder.HasOne<PackageRequest>()
            .WithMany()
            .HasForeignKey(item => item.RequestId)
            .HasConstraintName("fk_package_request_locations_package_requests_request_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PackageRequestLocation.RequestId), nameof(PackageRequestLocation.LocationId))
            .IsUnique()
            .HasDatabaseName("ix_package_request_locations_tenant_id_request_id_location_id");
    }
}
