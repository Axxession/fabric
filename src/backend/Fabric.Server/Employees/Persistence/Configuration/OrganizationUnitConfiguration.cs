using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ToTable("organization_units");

        builder.HasKey(unit => unit.Id).HasName("pk_organization_units");

        builder.Property(unit => unit.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(unit => unit.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(unit => unit.Code).HasColumnName("code").HasMaxLength(100);
        builder.Property(unit => unit.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        builder.Property(unit => unit.ParentId).HasColumnName("parent_id");
        builder.Property(unit => unit.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(unit => unit.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(unit => unit.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(unit => unit.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(OrganizationUnit.ParentId))
            .HasDatabaseName("ix_organization_units_tenant_id_parent_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(OrganizationUnit.Name))
            .HasDatabaseName("ix_organization_units_tenant_id_name");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(OrganizationUnit.Code))
            .IsUnique()
            .HasFilter("code IS NOT NULL")
            .HasDatabaseName("ix_organization_units_tenant_id_code");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(OrganizationUnit.IsActive))
            .HasDatabaseName("ix_organization_units_tenant_id_is_active");
    }
}
