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

            configuration.OwnsOne(c => c.Theme, theme =>
            {
                ThemeSettings defaults = ThemeSettings.Default;

                theme.Property(t => t.BackgroundColor).HasColumnName("theme_background_color").HasMaxLength(7).HasDefaultValue(defaults.BackgroundColor).IsRequired();
                theme.Property(t => t.ContentColor).HasColumnName("theme_content_color").HasMaxLength(7).HasDefaultValue(defaults.ContentColor).IsRequired();
                theme.Property(t => t.PrimaryColor).HasColumnName("theme_primary_color").HasMaxLength(7).HasDefaultValue(defaults.PrimaryColor).IsRequired();
                theme.Property(t => t.TextColor).HasColumnName("theme_text_color").HasMaxLength(7).HasDefaultValue(defaults.TextColor).IsRequired();
                theme.Property(t => t.TextMutedColor).HasColumnName("theme_text_muted_color").HasMaxLength(7).HasDefaultValue(defaults.TextMutedColor).IsRequired();
                theme.Property(t => t.BorderColor).HasColumnName("theme_border_color").HasMaxLength(7).HasDefaultValue(defaults.BorderColor).IsRequired();
                theme.Property(t => t.HoverBlueColor).HasColumnName("theme_hover_blue_color").HasMaxLength(7).HasDefaultValue(defaults.HoverBlueColor).IsRequired();
                theme.Property(t => t.ActiveBlueColor).HasColumnName("theme_active_blue_color").HasMaxLength(7).HasDefaultValue(defaults.ActiveBlueColor).IsRequired();
                theme.Property(t => t.HoverGrayColor).HasColumnName("theme_hover_gray_color").HasMaxLength(7).HasDefaultValue(defaults.HoverGrayColor).IsRequired();
                theme.Property(t => t.ErrorColor).HasColumnName("theme_error_color").HasMaxLength(7).HasDefaultValue(defaults.ErrorColor).IsRequired();
                theme.Property(t => t.ErrorBackgroundColor).HasColumnName("theme_error_background_color").HasMaxLength(7).HasDefaultValue(defaults.ErrorBackgroundColor).IsRequired();
                theme.Property(t => t.DangerColor).HasColumnName("theme_danger_color").HasMaxLength(7).HasDefaultValue(defaults.DangerColor).IsRequired();
                theme.Property(t => t.SuccessColor).HasColumnName("theme_success_color").HasMaxLength(7).HasDefaultValue(defaults.SuccessColor).IsRequired();
                theme.Property(t => t.SuccessBackgroundColor).HasColumnName("theme_success_background_color").HasMaxLength(7).HasDefaultValue(defaults.SuccessBackgroundColor).IsRequired();
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
