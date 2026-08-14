using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class EnforcementZoneAccessPolicyConfiguration : IEntityTypeConfiguration<EnforcementZoneAccessPolicy>
{
    public void Configure(EntityTypeBuilder<EnforcementZoneAccessPolicy> builder)
    {
        builder.ToTable("enforcement_zone_access_policies");
        builder.HasKey(item => item.Id).HasName("pk_enforcement_zone_access_policies");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.EnforcementZoneId).HasColumnName("enforcement_zone_id").IsRequired();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id").IsRequired();
        builder.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<EnforcementZone>()
            .WithMany()
            .HasForeignKey(item => item.EnforcementZoneId)
            .HasConstraintName("fk_enforcement_zone_access_policies_enforcement_zones_enforcement_zone_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EnforcementZoneAccessPolicy.EnforcementZoneId), nameof(EnforcementZoneAccessPolicy.AccessItemId)).IsUnique().HasDatabaseName("ix_enforcement_zone_access_policies_tenant_id_zone_access_item");
    }
}
