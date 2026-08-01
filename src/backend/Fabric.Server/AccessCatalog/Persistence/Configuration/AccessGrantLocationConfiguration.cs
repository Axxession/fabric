using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class AccessGrantLocationConfiguration : IEntityTypeConfiguration<AccessGrantLocation>
{
    public void Configure(EntityTypeBuilder<AccessGrantLocation> builder)
    {
        builder.ToTable("access_grant_locations");
        builder.HasKey(item => new { item.AccessGrantId, item.LocationId }).HasName("pk_access_grant_locations");
        builder.Property(item => item.AccessGrantId).HasColumnName("access_grant_id").IsRequired();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();

        builder.HasOne<AccessGrant>()
            .WithMany()
            .HasForeignKey(item => item.AccessGrantId)
            .HasConstraintName("fk_access_grant_locations_access_grants_access_grant_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessGrantLocation.AccessGrantId), nameof(AccessGrantLocation.LocationId))
            .IsUnique()
            .HasDatabaseName("ix_access_grant_locations_tenant_id_access_grant_id_location_id");
    }
}
