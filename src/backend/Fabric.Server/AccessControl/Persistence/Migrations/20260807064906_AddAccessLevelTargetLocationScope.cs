using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessLevelTargetLocationScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_access_level_targets_tenant_id_item_system_site_rule",
                schema: "access_control",
                table: "access_level_targets");

            migrationBuilder.AddColumn<Guid>(
                name: "location_id",
                schema: "access_control",
                table: "access_level_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id_item_system_location_site_rule",
                schema: "access_control",
                table: "access_level_targets",
                columns: new[] { "tenant_id", "access_item_id", "access_control_system_id", "location_id", "site_id", "access_rule_id" },
                unique: true,
                filter: "location_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id_item_system_site_rule_global",
                schema: "access_control",
                table: "access_level_targets",
                columns: new[] { "tenant_id", "access_item_id", "access_control_system_id", "site_id", "access_rule_id" },
                unique: true,
                filter: "location_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id_location_id",
                schema: "access_control",
                table: "access_level_targets",
                columns: new[] { "tenant_id", "location_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_access_level_targets_tenant_id_item_system_location_site_rule",
                schema: "access_control",
                table: "access_level_targets");

            migrationBuilder.DropIndex(
                name: "ix_access_level_targets_tenant_id_item_system_site_rule_global",
                schema: "access_control",
                table: "access_level_targets");

            migrationBuilder.DropIndex(
                name: "ix_access_level_targets_tenant_id_location_id",
                schema: "access_control",
                table: "access_level_targets");

            migrationBuilder.DropColumn(
                name: "location_id",
                schema: "access_control",
                table: "access_level_targets");

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id_item_system_site_rule",
                schema: "access_control",
                table: "access_level_targets",
                columns: new[] { "tenant_id", "access_item_id", "access_control_system_id", "site_id", "access_rule_id" },
                unique: true);
        }
    }
}
