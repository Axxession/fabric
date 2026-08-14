using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Requirements.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Requirements.Persistence.Configuration;

public sealed class EnforcementZoneConfiguration : IEntityTypeConfiguration<EnforcementZone>
{
    public void Configure(EntityTypeBuilder<EnforcementZone> builder)
    {
        builder.ToTable("enforcement_zones");
        builder.HasKey(item => item.Id).HasName("pk_enforcement_zones");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(2_000);
        builder.Property(item => item.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.UpdatedAt).HasColumnName("updated_at").IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(EnforcementZone.Code)).IsUnique().HasDatabaseName("ix_enforcement_zones_tenant_id_code");
    }
}
