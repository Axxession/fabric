using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class AccessGrantConfiguration : IEntityTypeConfiguration<AccessGrant>
{
    public void Configure(EntityTypeBuilder<AccessGrant> builder)
    {
        builder.ToTable("access_grants");
        builder.HasKey(item => item.Id).HasName("pk_access_grants");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.AccessItemId).HasColumnName("access_item_id");
        builder.Property(item => item.IdentityId).HasColumnName("identity_id").IsRequired();
        builder.Property(item => item.AssignmentChannel).HasColumnName("assignment_channel").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.SourceKind).HasColumnName("source_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.SourceId).HasColumnName("source_id").IsRequired();
        builder.Property(item => item.ApprovalFlowId).HasColumnName("approval_flow_id");
        builder.Property(item => item.RequestScopeId).HasColumnName("request_scope_id");
        builder.Property(item => item.DurationKind).HasColumnName("duration_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(item => item.ValidUntil).HasColumnName("valid_until");
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ReasonText).HasColumnName("reason_text").HasMaxLength(2_000).IsRequired();

        builder.HasOne<Package>()
            .WithMany()
            .HasForeignKey(item => item.PackageId)
            .HasConstraintName("fk_access_grants_packages_package_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApprovalFlow>()
            .WithMany()
            .HasForeignKey(item => item.ApprovalFlowId)
            .HasConstraintName("fk_access_grants_approval_flows_approval_flow_id")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<PackageRequestScope>()
            .WithMany()
            .HasForeignKey(item => item.RequestScopeId)
            .HasConstraintName("fk_access_grants_package_request_scopes_request_scope_id")
            .OnDelete(DeleteBehavior.SetNull);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(AccessGrant.IdentityId), nameof(AccessGrant.Status))
            .HasDatabaseName("ix_access_grants_tenant_id_identity_id_status");
    }
}
