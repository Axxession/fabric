using Fabric.Server.AccessControl.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessControl.Persistence.Configuration;

public sealed class AccessItemConfiguration : IEntityTypeConfiguration<AccessItem>
{
    public void Configure(EntityTypeBuilder<AccessItem> builder)
    {
        builder.ToTable("access_items");

        builder.HasKey(item => item.Id).HasName("pk_access_items");

        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Description).HasColumnName("description").HasMaxLength(2_000);
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);

        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessItem.Name))
            .IsUnique()
            .HasDatabaseName("ix_access_items_tenant_id_name");
    }
}
