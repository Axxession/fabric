using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.AccessCatalog.Persistence.Configuration;

public sealed class PackageRequestConfiguration : IEntityTypeConfiguration<PackageRequest>
{
    public void Configure(EntityTypeBuilder<PackageRequest> builder)
    {
        builder.ToTable("package_requests");
        builder.HasKey(item => item.Id).HasName("pk_package_requests");
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(item => item.PackageId).HasColumnName("package_id").IsRequired();
        builder.Property(item => item.RequesterIdentityId).HasColumnName("requester_identity_id").IsRequired();
        builder.Property(item => item.BeneficiaryIdentityId).HasColumnName("beneficiary_identity_id").IsRequired();
        builder.Property(item => item.RequestReason).HasColumnName("request_reason").HasMaxLength(2_000).IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.SubStatus).HasColumnName("sub_status").HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.DurationKind).HasColumnName("duration_kind").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.ValidFrom).HasColumnName("valid_from").IsRequired();
        builder.Property(item => item.ValidUntil).HasColumnName("valid_until");
        builder.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(item => item.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(item => item.DecidedAt).HasColumnName("decided_at");

        builder.HasOne<Package>()
            .WithMany()
            .HasForeignKey(item => item.PackageId)
            .HasConstraintName("fk_package_requests_packages_package_id")
            .OnDelete(DeleteBehavior.Cascade);

        TenantDbContext.ConfigureTenantProperty(builder);
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PackageRequest.RequesterIdentityId), nameof(PackageRequest.Status))
            .HasDatabaseName("ix_package_requests_tenant_id_requester_identity_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PackageRequest.BeneficiaryIdentityId), nameof(PackageRequest.Status))
            .HasDatabaseName("ix_package_requests_tenant_id_beneficiary_identity_id_status");
        builder.HasIndex(TenantDbContext.TenantIdPropertyName, nameof(PackageRequest.PackageId), nameof(PackageRequest.Status))
            .HasDatabaseName("ix_package_requests_tenant_id_package_id_status");
    }
}
