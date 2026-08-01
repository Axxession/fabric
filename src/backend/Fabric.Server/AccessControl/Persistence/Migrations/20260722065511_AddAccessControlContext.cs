using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessControlContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "access_control");

            migrationBuilder.CreateTable(
                name: "access_control_systems",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    unipass_endpoint = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    unipass_ssl_validation = table.Column<bool>(type: "boolean", nullable: true),
                    unipass_username = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    unipass_password = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_control_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "access_items",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "access_control_system_locations",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_control_system_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_access_control_system_locations_access_control_systems_access_control_system_id",
                        column: x => x.access_control_system_id,
                        principalSchema: "access_control",
                        principalTable: "access_control_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "access_level_targets",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_type = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    access_rule_id = table.Column<int>(type: "integer", nullable: true),
                    site_id = table.Column<int>(type: "integer", nullable: true),
                    access_rule_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    site_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_level_targets", x => x.id);
                    table.ForeignKey(
                        name: "fk_access_level_targets_access_control_systems_access_control_system_id",
                        column: x => x.access_control_system_id,
                        principalSchema: "access_control",
                        principalTable: "access_control_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_access_level_targets_access_items_access_item_id",
                        column: x => x.access_item_id,
                        principalSchema: "access_control",
                        principalTable: "access_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_control_system_locations_access_control_system_id",
                schema: "access_control",
                table: "access_control_system_locations",
                column: "access_control_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_control_system_locations_tenant_id",
                schema: "access_control",
                table: "access_control_system_locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_control_system_locations_tenant_id_access_control_system_id",
                schema: "access_control",
                table: "access_control_system_locations",
                columns: new[] { "tenant_id", "access_control_system_id" });

            migrationBuilder.CreateIndex(
                name: "ix_access_control_system_locations_tenant_id_location_id",
                schema: "access_control",
                table: "access_control_system_locations",
                columns: new[] { "tenant_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_access_control_systems_tenant_id",
                schema: "access_control",
                table: "access_control_systems",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_control_systems_tenant_id_name",
                schema: "access_control",
                table: "access_control_systems",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_access_items_tenant_id",
                schema: "access_control",
                table: "access_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_items_tenant_id_name",
                schema: "access_control",
                table: "access_items",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_access_level_targets_access_control_system_id",
                schema: "access_control",
                table: "access_level_targets",
                column: "access_control_system_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_level_targets_access_item_id",
                schema: "access_control",
                table: "access_level_targets",
                column: "access_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id",
                schema: "access_control",
                table: "access_level_targets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id_access_control_system_id",
                schema: "access_control",
                table: "access_level_targets",
                columns: new[] { "tenant_id", "access_control_system_id" });

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id_access_item_id",
                schema: "access_control",
                table: "access_level_targets",
                columns: new[] { "tenant_id", "access_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_access_level_targets_tenant_id_item_system_site_rule",
                schema: "access_control",
                table: "access_level_targets",
                columns: new[] { "tenant_id", "access_item_id", "access_control_system_id", "site_id", "access_rule_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_control_system_locations",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "access_level_targets",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "access_control_systems",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "access_items",
                schema: "access_control");
        }
    }
}
