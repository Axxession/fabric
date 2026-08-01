using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class PackageAccessItemConfiguration : IEntityTypeConfiguration<PackageAccessItem>
{
    public void Configure(EntityTypeBuilder<PackageAccessItem> builder)
    {
        builder.ToTable("package_access_items");
        builder.HasKey(item => new { item.PackageId, item.AccessItemId }).HasName("pk_package_access_items");
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id").IsRequired();

        builder.HasOne<Package>()
            .WithMany()
            .HasForeignKey(item => item.PackageId)
            .HasConstraintName("fk_package_access_items_packages_package_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PackageAccessItem.PackageId), nameof(PackageAccessItem.AccessItemId))
            .IsUnique()
            .HasDatabaseName("ix_package_access_items_tenant_id_package_id_access_item_id");
    }
}
