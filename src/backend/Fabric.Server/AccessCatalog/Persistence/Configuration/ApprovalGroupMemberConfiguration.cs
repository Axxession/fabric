using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class ApprovalGroupMemberConfiguration : IEntityTypeConfiguration<ApprovalGroupMember>
{
    public void Configure(EntityTypeBuilder<ApprovalGroupMember> builder)
    {
        builder.ToTable("approval_group_members");
        builder.HasKey(item => item.Id).HasName("pk_approval_group_members");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.ApprovalGroupId).HasColumnName("approval_group_id").IsRequired();
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.ResponsibleLocationId).HasColumnName("responsible_location_id").IsRequired();

        builder.HasOne<ApprovalGroup>()
            .WithMany()
            .HasForeignKey(item => item.ApprovalGroupId)
            .HasConstraintName("fk_approval_group_members_approval_groups_approval_group_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalGroupMember.ApprovalGroupId), nameof(ApprovalGroupMember.IdentityId), nameof(ApprovalGroupMember.ResponsibleLocationId))
            .IsUnique()
            .HasDatabaseName("ix_approval_group_members_tenant_id_group_identity_location");
    }
}
