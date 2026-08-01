using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class ApprovalGroupConfiguration : IEntityTypeConfiguration<ApprovalGroup>
{
    public void Configure(EntityTypeBuilder<ApprovalGroup> builder)
    {
        builder.ToTable("approval_groups");
        builder.HasKey(item => item.Id).HasName("pk_approval_groups");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalGroup.Name))
            .IsUnique()
            .HasDatabaseName("ix_approval_groups_tenant_id_name");
    }
}
