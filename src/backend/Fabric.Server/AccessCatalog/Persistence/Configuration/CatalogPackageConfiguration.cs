using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class CatalogPackageConfiguration : IEntityTypeConfiguration<CatalogPackage>
{
    public void Configure(EntityTypeBuilder<CatalogPackage> builder)
    {
        builder.ToTable("catalog_packages");
        builder.HasKey(item => new { item.CatalogId, item.PackageId }).HasName("pk_catalog_packages");
        builder.Property(item => item.CatalogId).HasColumnName("catalog_id").IsRequired();
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.IsRequestable).HasColumnName("is_requestable").IsRequired();

        builder.HasOne<Catalog>()
            .WithMany()
            .HasForeignKey(item => item.CatalogId)
            .HasConstraintName("fk_catalog_packages_catalogs_catalog_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Package>()
            .WithMany()
            .HasForeignKey(item => item.PackageId)
            .HasConstraintName("fk_catalog_packages_packages_package_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(CatalogPackage.CatalogId), nameof(CatalogPackage.PackageId))
            .IsUnique()
            .HasDatabaseName("ix_catalog_packages_tenant_id_catalog_id_package_id");
    }
}
