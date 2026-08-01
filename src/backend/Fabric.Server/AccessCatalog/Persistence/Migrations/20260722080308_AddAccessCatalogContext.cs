using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessCatalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessCatalogContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "access_catalog");

            migrationBuilder.CreateTable(
                name: "catalogs",
                schema: "access_catalog",
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
                    table.PrimaryKey("pk_catalogs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "packages",
                schema: "access_catalog",
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
                    table.PrimaryKey("pk_packages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "access_grants",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_grants", x => x.id);
                    table.ForeignKey(
                        name: "fk_access_grants_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "access_catalog",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_packages",
                schema: "access_catalog",
                columns: table => new
                {
                    catalog_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_requestable = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_packages", x => new { x.catalog_id, x.package_id });
                    table.ForeignKey(
                        name: "fk_catalog_packages_catalogs_catalog_id",
                        column: x => x.catalog_id,
                        principalSchema: "access_catalog",
                        principalTable: "catalogs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_packages_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "access_catalog",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package_access_items",
                schema: "access_catalog",
                columns: table => new
                {
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_access_items", x => new { x.package_id, x.access_item_id });
                    table.ForeignKey(
                        name: "fk_package_access_items_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "access_catalog",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "access_grant_locations",
                schema: "access_catalog",
                columns: table => new
                {
                    access_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_grant_locations", x => new { x.access_grant_id, x.location_id });
                    table.ForeignKey(
                        name: "fk_access_grant_locations_access_grants_access_grant_id",
                        column: x => x.access_grant_id,
                        principalSchema: "access_catalog",
                        principalTable: "access_grants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_locations_tenant_id",
                schema: "access_catalog",
                table: "access_grant_locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_locations_tenant_id_access_grant_id_location_id",
                schema: "access_catalog",
                table: "access_grant_locations",
                columns: new[] { "tenant_id", "access_grant_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_access_grants_package_id",
                schema: "access_catalog",
                table: "access_grants",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_grants_tenant_id",
                schema: "access_catalog",
                table: "access_grants",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_grants_tenant_id_identity_id_status",
                schema: "access_catalog",
                table: "access_grants",
                columns: new[] { "tenant_id", "identity_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_catalog_packages_package_id",
                schema: "access_catalog",
                table: "catalog_packages",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_packages_tenant_id",
                schema: "access_catalog",
                table: "catalog_packages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_packages_tenant_id_catalog_id_package_id",
                schema: "access_catalog",
                table: "catalog_packages",
                columns: new[] { "tenant_id", "catalog_id", "package_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_tenant_id",
                schema: "access_catalog",
                table: "catalogs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalogs_tenant_id_name",
                schema: "access_catalog",
                table: "catalogs",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_package_access_items_tenant_id",
                schema: "access_catalog",
                table: "package_access_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_access_items_tenant_id_package_id_access_item_id",
                schema: "access_catalog",
                table: "package_access_items",
                columns: new[] { "tenant_id", "package_id", "access_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_packages_tenant_id",
                schema: "access_catalog",
                table: "packages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_packages_tenant_id_name",
                schema: "access_catalog",
                table: "packages",
                columns: new[] { "tenant_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_grant_locations",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "catalog_packages",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "package_access_items",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "access_grants",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "catalogs",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "packages",
                schema: "access_catalog");
        }
    }
}
