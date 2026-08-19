using Fabric.Server.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Tenants.Persistence.Configuration;

public sealed class TenantIntegrationConfiguration : IEntityTypeConfiguration<TenantIntegration>
{
    public void Configure(EntityTypeBuilder<TenantIntegration> builder)
    {
        builder.ToTable("tenant_integrations");

        builder.HasKey(item => new { item.TenantId, item.Name }).HasName("pk_tenant_integrations");

        builder.Property(item => item.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.DataJson)
            .HasColumnName("data_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(item => item.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(item => new { item.TenantId, item.Name })
            .IsUnique()
            .HasDatabaseName("ix_tenant_integrations_tenant_id_name");
    }
}
