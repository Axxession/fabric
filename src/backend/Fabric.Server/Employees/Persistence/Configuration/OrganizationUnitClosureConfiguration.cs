using Fabric.Server.Employees.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Employees.Persistence.Configuration;

public sealed class OrganizationUnitClosureConfiguration : IEntityTypeConfiguration<OrganizationUnitClosure>
{
    public void Configure(EntityTypeBuilder<OrganizationUnitClosure> builder)
    {
        builder.ToTable("organization_unit_closures");

        builder.HasKey(closure => new { closure.AncestorId, closure.DescendantId })
            .HasName("pk_organization_unit_closures");

        builder.Property(closure => closure.AncestorId).HasColumnName("ancestor_id").IsRequired();
        builder.Property(closure => closure.DescendantId).HasColumnName("descendant_id").IsRequired();
        builder.Property(closure => closure.Depth).HasColumnName("depth").IsRequired();

        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(closure => closure.AncestorId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OrganizationUnit>()
            .WithMany()
            .HasForeignKey(closure => closure.DescendantId)
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(OrganizationUnitClosure.AncestorId), nameof(OrganizationUnitClosure.Depth))
            .HasDatabaseName("ix_org_unit_closures_tenant_id_ancestor_id_depth");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(OrganizationUnitClosure.DescendantId), nameof(OrganizationUnitClosure.Depth))
            .HasDatabaseName("ix_org_unit_closures_tenant_id_descendant_id_depth");
    }
}
