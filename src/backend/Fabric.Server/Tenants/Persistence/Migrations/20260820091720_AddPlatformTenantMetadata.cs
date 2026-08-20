using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformTenantMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at_utc",
                schema: "tenancy",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "tenancy",
                table: "tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at_utc",
                schema: "tenancy",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.Sql("""
                UPDATE tenancy.tenants
                SET display_name = id
                WHERE display_name = '';
                """);

            migrationBuilder.Sql("""
                UPDATE tenancy.tenants
                SET created_at_utc = CURRENT_TIMESTAMP,
                    updated_at_utc = CURRENT_TIMESTAMP
                WHERE created_at_utc = TIMESTAMPTZ '0001-01-01 00:00:00+00'
                   OR updated_at_utc = TIMESTAMPTZ '0001-01-01 00:00:00+00';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at_utc",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "display_name",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "updated_at_utc",
                schema: "tenancy",
                table: "tenants");
        }
    }
}
