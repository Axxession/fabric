using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropTenantTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "theme_active_blue_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_background_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_border_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_content_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_danger_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_error_background_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_error_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_hover_blue_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_hover_gray_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_primary_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_success_background_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_success_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_text_color",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "theme_text_muted_color",
                schema: "tenancy",
                table: "tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "theme_active_blue_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#deeeff");

            migrationBuilder.AddColumn<string>(
                name: "theme_background_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#f8f8f8");

            migrationBuilder.AddColumn<string>(
                name: "theme_border_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#dddddd");

            migrationBuilder.AddColumn<string>(
                name: "theme_content_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#ffffff");

            migrationBuilder.AddColumn<string>(
                name: "theme_danger_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#ff6467");

            migrationBuilder.AddColumn<string>(
                name: "theme_error_background_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#feeaea");

            migrationBuilder.AddColumn<string>(
                name: "theme_error_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#ff6467");

            migrationBuilder.AddColumn<string>(
                name: "theme_hover_blue_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#eef6ff");

            migrationBuilder.AddColumn<string>(
                name: "theme_hover_gray_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#f3f3f3");

            migrationBuilder.AddColumn<string>(
                name: "theme_primary_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#238cff");

            migrationBuilder.AddColumn<string>(
                name: "theme_success_background_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#e6faeb");

            migrationBuilder.AddColumn<string>(
                name: "theme_success_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#00c950");

            migrationBuilder.AddColumn<string>(
                name: "theme_text_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#212529");

            migrationBuilder.AddColumn<string>(
                name: "theme_text_muted_color",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#6c757d");
        }
    }
}
