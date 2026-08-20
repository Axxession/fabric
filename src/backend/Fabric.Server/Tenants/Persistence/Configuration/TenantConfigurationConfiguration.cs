using Fabric.Server.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fabric.Server.Tenants.Persistence.Configuration;

public sealed class TenantConfigurationConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", table =>
        {
            table.HasCheckConstraint(
                "ck_tenants_logo_data_max_length",
                $"logo_data IS NULL OR octet_length(logo_data) <= {LogoSettings.MaxDataLength}");
        });

        builder.HasKey(tenant => tenant.Id).HasName("pk_tenants");
        builder.Property(tenant => tenant.Id).HasColumnName("id").HasMaxLength(100).ValueGeneratedNever();
        builder.Property(tenant => tenant.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        builder.Property(tenant => tenant.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(tenant => tenant.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(tenant => tenant.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.OwnsOne(tenant => tenant.Configuration, configuration =>
        {
            configuration.OwnsOne(c => c.Oidc, oidc =>
            {
                oidc.Property(o => o.MetadataUrl)
                    .HasColumnName("oidc_metadata_url")
                    .HasMaxLength(2_000)
                    .IsRequired();

                oidc.Property(o => o.ClientId)
                    .HasColumnName("oidc_client_id")
                    .HasMaxLength(200)
                    .IsRequired();

                oidc.Property(o => o.RequireHttpsMetadata)
                    .HasColumnName("oidc_require_https_metadata")
                    .IsRequired();
            });

            configuration.OwnsOne(c => c.Logo, logo =>
            {
                logo.Property(l => l.ContentType)
                    .HasColumnName("logo_content_type")
                    .HasMaxLength(100)
                    .IsRequired();

                logo.Property(l => l.Data)
                    .HasColumnName("logo_data")
                    .HasColumnType("bytea")
                    .HasMaxLength(LogoSettings.MaxDataLength)
                    .IsRequired();
            });

            configuration.OwnsOne(c => c.Host, host =>
            {
                host.Property(h => h.AssignmentMode)
                    .HasColumnName("host_assignment_mode")
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .HasDefaultValue(HostAssignmentMode.AllEmployees)
                    .IsRequired();
            });
        });
    }
}
