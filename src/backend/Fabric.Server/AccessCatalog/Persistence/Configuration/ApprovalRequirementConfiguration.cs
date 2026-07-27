using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class ApprovalRequirementConfiguration : IEntityTypeConfiguration<ApprovalRequirement>
{
    public void Configure(EntityTypeBuilder<ApprovalRequirement> builder)
    {
        builder.ToTable("approval_requirements");
        builder.HasKey(item => item.Id).HasName("pk_approval_requirements");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.ApprovalFlowId).HasColumnName("approval_flow_id").IsRequired();
        builder.Property(item => item.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id").IsRequired();
        builder.Property(item => item.LocationId).HasColumnName("location_id").IsRequired();
        builder.Property(item => item.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ApprovalGroupId).HasColumnName("approval_group_id");
        builder.Property(item => item.RequiredApproverIdentityId).HasColumnName("required_approver_identity_id");
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.SystemApprovalReason).HasColumnName("system_approval_reason").HasMaxLength(2_000);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");

        builder.HasOne<PackageRequest>()
            .WithMany()
            .HasForeignKey(item => item.RequestId)
            .HasConstraintName("fk_approval_requirements_package_requests_request_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApprovalFlow>()
            .WithMany()
            .HasForeignKey(item => item.ApprovalFlowId)
            .HasConstraintName("fk_approval_requirements_approval_flows_approval_flow_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApprovalGroup>()
            .WithMany()
            .HasForeignKey(item => item.ApprovalGroupId)
            .HasConstraintName("fk_approval_requirements_approval_groups_approval_group_id")
            .OnDelete(DeleteBehavior.Restrict);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalRequirement.ApprovalFlowId), nameof(ApprovalRequirement.Status))
            .HasDatabaseName("ix_approval_requirements_tenant_id_approval_flow_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalRequirement.RequestId), nameof(ApprovalRequirement.Status))
            .HasDatabaseName("ix_approval_requirements_tenant_id_request_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalRequirement.RequiredApproverIdentityId), nameof(ApprovalRequirement.Status))
            .HasDatabaseName("ix_approval_requirements_tenant_id_required_approver_identity_id_status");
    }
}
