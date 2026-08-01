using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class ApprovalFlowConfiguration : IEntityTypeConfiguration<ApprovalFlow>
{
    public void Configure(EntityTypeBuilder<ApprovalFlow> builder)
    {
        builder.ToTable("approval_flows");
        builder.HasKey(item => item.Id).HasName("pk_approval_flows");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id").IsRequired();
        builder.Property(item => item.SiteId).HasColumnName("site_id").IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.CompletedAt).HasColumnName("completed_at");

        builder.HasOne<PackageRequest>()
            .WithMany()
            .HasForeignKey(item => item.RequestId)
            .HasConstraintName("fk_approval_flows_package_requests_request_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Package>()
            .WithMany()
            .HasForeignKey(item => item.PackageId)
            .HasConstraintName("fk_approval_flows_packages_package_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalFlow.RequestId), nameof(ApprovalFlow.Status))
            .HasDatabaseName("ix_approval_flows_tenant_id_request_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(ApprovalFlow.AccessItemId), nameof(ApprovalFlow.SiteId))
            .HasDatabaseName("ix_approval_flows_tenant_id_access_item_id_site_id");
    }
}
