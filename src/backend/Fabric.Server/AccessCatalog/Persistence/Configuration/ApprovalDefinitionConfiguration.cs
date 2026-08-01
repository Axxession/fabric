using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class ApprovalDefinitionConfiguration : IEntityTypeConfiguration<ApprovalDefinition>
{
    public void Configure(EntityTypeBuilder<ApprovalDefinition> builder)
    {
        builder.ToTable("approval_definitions");
        builder.HasKey(item => item.Id).HasName("pk_approval_definitions");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id").IsRequired();
        builder.Property(item => item.DestinationApprovalGroupId).HasColumnName("destination_approval_group_id");
        builder.Property(item => item.OrganizationalApprovalMode).HasColumnName("organizational_approval_mode").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.OrganizationalApprovalLevels).HasColumnName("organizational_approval_levels").IsRequired();

        builder.HasOne<ApprovalGroup>()
            .WithMany()
            .HasForeignKey(item => item.DestinationApprovalGroupId)
            .HasConstraintName("fk_approval_definitions_approval_groups_destination_approval_group_id")
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalDefinition.AccessItemId))
            .IsUnique()
            .HasDatabaseName("ix_approval_definitions_tenant_id_access_item_id");
    }
}
