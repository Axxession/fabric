using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class PackageRequestScopeConfiguration : IEntityTypeConfiguration<PackageRequestScope>
{
    public void Configure(EntityTypeBuilder<PackageRequestScope> builder)
    {
        builder.ToTable("package_request_scopes");
        builder.HasKey(item => item.Id).HasName("pk_package_request_scopes");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.RequestId).HasColumnName("request_id").IsRequired();
        builder.Property(item => item.ApprovalFlowId).HasColumnName("approval_flow_id").IsRequired();
        builder.Property(item => item.RequestedLocationId).HasColumnName("requested_location_id").IsRequired();

        builder.HasOne<PackageRequest>()
            .WithMany()
            .HasForeignKey(item => item.RequestId)
            .HasConstraintName("fk_package_request_scopes_package_requests_request_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApprovalFlow>()
            .WithMany()
            .HasForeignKey(item => item.ApprovalFlowId)
            .HasConstraintName("fk_package_request_scopes_approval_flows_approval_flow_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PackageRequestScope.RequestId))
            .HasDatabaseName("ix_package_request_scopes_tenant_id_request_id");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PackageRequestScope.ApprovalFlowId), nameof(PackageRequestScope.RequestedLocationId))
            .IsUnique()
            .HasDatabaseName("ix_package_request_scopes_tenant_id_flow_location");
    }
}
