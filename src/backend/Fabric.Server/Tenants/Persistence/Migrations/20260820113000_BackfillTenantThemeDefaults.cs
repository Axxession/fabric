using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillTenantThemeDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE tenancy.tenants
                SET theme_background_color = COALESCE(theme_background_color, '#f8f8f8'),
                    theme_content_color = COALESCE(theme_content_color, '#ffffff'),
                    theme_primary_color = COALESCE(theme_primary_color, '#238cff'),
                    theme_text_color = COALESCE(theme_text_color, '#212529'),
                    theme_text_muted_color = COALESCE(theme_text_muted_color, '#6c757d'),
                    theme_border_color = COALESCE(theme_border_color, '#dddddd'),
                    theme_hover_blue_color = COALESCE(theme_hover_blue_color, '#eef6ff'),
                    theme_active_blue_color = COALESCE(theme_active_blue_color, '#deeeff'),
                    theme_hover_gray_color = COALESCE(theme_hover_gray_color, '#f3f3f3'),
                    theme_error_color = COALESCE(theme_error_color, '#ff6467'),
                    theme_error_background_color = COALESCE(theme_error_background_color, '#feeaea'),
                    theme_danger_color = COALESCE(theme_danger_color, '#ff6467'),
                    theme_success_color = COALESCE(theme_success_color, '#00c950'),
                    theme_success_background_color = COALESCE(theme_success_background_color, '#e6faeb')
                WHERE theme_background_color IS NULL
                   OR theme_content_color IS NULL
                   OR theme_primary_color IS NULL
                   OR theme_text_color IS NULL
                   OR theme_text_muted_color IS NULL
                   OR theme_border_color IS NULL
                   OR theme_hover_blue_color IS NULL
                   OR theme_active_blue_color IS NULL
                   OR theme_hover_gray_color IS NULL
                   OR theme_error_color IS NULL
                   OR theme_error_background_color IS NULL
                   OR theme_danger_color IS NULL
                   OR theme_success_color IS NULL
                   OR theme_success_background_color IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
