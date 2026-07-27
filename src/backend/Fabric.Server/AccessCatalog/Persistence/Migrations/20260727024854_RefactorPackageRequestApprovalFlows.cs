using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessCatalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPackageRequestApprovalFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sub_status",
                schema: "access_catalog",
                table: "package_requests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approval_flow_id",
                schema: "access_catalog",
                table: "approval_requirements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "access_item_id",
                schema: "access_catalog",
                table: "access_grants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approval_flow_id",
                schema: "access_catalog",
                table: "access_grants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "request_scope_id",
                schema: "access_catalog",
                table: "access_grants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_flows",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_flows", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_flows_package_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "access_catalog",
                        principalTable: "package_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_approval_flows_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "access_catalog",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package_request_scopes",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_flow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_request_scopes", x => x.id);
                    table.ForeignKey(
                        name: "fk_package_request_scopes_approval_flows_approval_flow_id",
                        column: x => x.approval_flow_id,
                        principalSchema: "access_catalog",
                        principalTable: "approval_flows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_package_request_scopes_package_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "access_catalog",
                        principalTable: "package_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE access_catalog.package_requests
                SET status = CASE status
                        WHEN 'Requested' THEN 'InProgress'
                        WHEN 'PendingApproval' THEN 'InProgress'
                        ELSE 'Completed'
                    END,
                    sub_status = CASE status
                        WHEN 'Approved' THEN 'Approved'
                        WHEN 'Rejected' THEN 'Rejected'
                        WHEN 'Expired' THEN 'Expired'
                        ELSE NULL
                    END;
                """);

            migrationBuilder.Sql("""
                INSERT INTO access_catalog.approval_flows (id, request_id, package_id, access_item_id, site_id, status, created_at, completed_at, tenant_id)
                SELECT
                    gen_random_uuid(),
                    pr.id,
                    pr.package_id,
                    pai.access_item_id,
                    ll.site_id,
                    CASE
                        WHEN pr.status = 'InProgress' THEN 'InProgress'
                        WHEN pr.sub_status = 'Approved' AND EXISTS (
                            SELECT 1
                            FROM access_catalog.approval_requirements ar
                            WHERE ar.request_id = pr.id
                              AND ar.access_item_id = pai.access_item_id
                              AND ar.tenant_id = pr.tenant_id) THEN 'Approved'
                        WHEN pr.sub_status = 'Approved' THEN 'SystemApproved'
                        WHEN pr.sub_status = 'Rejected' THEN 'Rejected'
                        WHEN pr.sub_status = 'Expired' THEN 'Expired'
                        ELSE 'InProgress'
                    END,
                    pr.created_at,
                    CASE
                        WHEN pr.status = 'Completed' THEN COALESCE(pr.decided_at, pr.created_at)
                        ELSE NULL
                    END,
                    pr.tenant_id
                FROM access_catalog.package_requests pr
                JOIN access_catalog.package_request_locations prl
                    ON prl.request_id = pr.id
                   AND prl.tenant_id = pr.tenant_id
                JOIN locations.location_lookup ll
                    ON ll.id = prl.location_id
                JOIN (
                    SELECT package_id, access_item_id, tenant_id
                    FROM access_catalog.package_access_items
                    UNION
                    SELECT prh.package_id, ar.access_item_id, ar.tenant_id
                    FROM access_catalog.approval_requirements ar
                    JOIN access_catalog.package_requests prh
                        ON prh.id = ar.request_id
                       AND prh.tenant_id = ar.tenant_id
                ) pai
                    ON pai.package_id = pr.package_id
                   AND pai.tenant_id = pr.tenant_id
                GROUP BY pr.id, pr.package_id, pai.access_item_id, ll.site_id, pr.status, pr.sub_status, pr.created_at, pr.decided_at, pr.tenant_id;
                """);

            migrationBuilder.Sql("""
                UPDATE access_catalog.approval_requirements ar
                SET location_id = ll.site_id
                FROM locations.location_lookup ll
                WHERE ll.id = ar.location_id;
                """);

            migrationBuilder.Sql("""
                UPDATE access_catalog.approval_requirements ar
                SET approval_flow_id = af.id
                FROM access_catalog.approval_flows af
                WHERE af.request_id = ar.request_id
                  AND af.access_item_id = ar.access_item_id
                  AND af.site_id = ar.location_id
                  AND af.tenant_id = ar.tenant_id;
                """);

            migrationBuilder.Sql("""
                INSERT INTO access_catalog.package_request_scopes (id, request_id, approval_flow_id, requested_location_id, tenant_id)
                SELECT
                    gen_random_uuid(),
                    pr.id,
                    af.id,
                    prl.location_id,
                    pr.tenant_id
                FROM access_catalog.package_requests pr
                JOIN access_catalog.package_request_locations prl
                    ON prl.request_id = pr.id
                   AND prl.tenant_id = pr.tenant_id
                JOIN locations.location_lookup ll
                    ON ll.id = prl.location_id
                JOIN (
                    SELECT package_id, access_item_id, tenant_id
                    FROM access_catalog.package_access_items
                    UNION
                    SELECT prh.package_id, ar.access_item_id, ar.tenant_id
                    FROM access_catalog.approval_requirements ar
                    JOIN access_catalog.package_requests prh
                        ON prh.id = ar.request_id
                       AND prh.tenant_id = ar.tenant_id
                ) pai
                    ON pai.package_id = pr.package_id
                   AND pai.tenant_id = pr.tenant_id
                JOIN access_catalog.approval_flows af
                    ON af.request_id = pr.id
                   AND af.access_item_id = pai.access_item_id
                   AND af.site_id = ll.site_id
                   AND af.tenant_id = pr.tenant_id;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "approval_flow_id",
                schema: "access_catalog",
                table: "approval_requirements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_approval_requirements_approval_flow_id",
                schema: "access_catalog",
                table: "approval_requirements",
                column: "approval_flow_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requirements_tenant_id_approval_flow_id_status",
                schema: "access_catalog",
                table: "approval_requirements",
                columns: new[] { "tenant_id", "approval_flow_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_access_grants_approval_flow_id",
                schema: "access_catalog",
                table: "access_grants",
                column: "approval_flow_id");

            migrationBuilder.CreateIndex(
                name: "IX_access_grants_request_scope_id",
                schema: "access_catalog",
                table: "access_grants",
                column: "request_scope_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_flows_package_id",
                schema: "access_catalog",
                table: "approval_flows",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_flows_request_id",
                schema: "access_catalog",
                table: "approval_flows",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_flows_tenant_id",
                schema: "access_catalog",
                table: "approval_flows",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_flows_tenant_id_access_item_id_site_id",
                schema: "access_catalog",
                table: "approval_flows",
                columns: new[] { "tenant_id", "access_item_id", "site_id" });

            migrationBuilder.CreateIndex(
                name: "ix_approval_flows_tenant_id_request_id_status",
                schema: "access_catalog",
                table: "approval_flows",
                columns: new[] { "tenant_id", "request_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_package_request_scopes_approval_flow_id",
                schema: "access_catalog",
                table: "package_request_scopes",
                column: "approval_flow_id");

            migrationBuilder.CreateIndex(
                name: "IX_package_request_scopes_request_id",
                schema: "access_catalog",
                table: "package_request_scopes",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_request_scopes_tenant_id",
                schema: "access_catalog",
                table: "package_request_scopes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_request_scopes_tenant_id_flow_location",
                schema: "access_catalog",
                table: "package_request_scopes",
                columns: new[] { "tenant_id", "approval_flow_id", "requested_location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_package_request_scopes_tenant_id_request_id",
                schema: "access_catalog",
                table: "package_request_scopes",
                columns: new[] { "tenant_id", "request_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_access_grants_approval_flows_approval_flow_id",
                schema: "access_catalog",
                table: "access_grants",
                column: "approval_flow_id",
                principalSchema: "access_catalog",
                principalTable: "approval_flows",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_access_grants_package_request_scopes_request_scope_id",
                schema: "access_catalog",
                table: "access_grants",
                column: "request_scope_id",
                principalSchema: "access_catalog",
                principalTable: "package_request_scopes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_requirements_approval_flows_approval_flow_id",
                schema: "access_catalog",
                table: "approval_requirements",
                column: "approval_flow_id",
                principalSchema: "access_catalog",
                principalTable: "approval_flows",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_access_grants_approval_flows_approval_flow_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropForeignKey(
                name: "fk_access_grants_package_request_scopes_request_scope_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropForeignKey(
                name: "fk_approval_requirements_approval_flows_approval_flow_id",
                schema: "access_catalog",
                table: "approval_requirements");

            migrationBuilder.DropTable(
                name: "package_request_scopes",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "approval_flows",
                schema: "access_catalog");

            migrationBuilder.DropIndex(
                name: "IX_approval_requirements_approval_flow_id",
                schema: "access_catalog",
                table: "approval_requirements");

            migrationBuilder.DropIndex(
                name: "ix_approval_requirements_tenant_id_approval_flow_id_status",
                schema: "access_catalog",
                table: "approval_requirements");

            migrationBuilder.DropIndex(
                name: "IX_access_grants_approval_flow_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropIndex(
                name: "IX_access_grants_request_scope_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.Sql("""
                UPDATE access_catalog.package_requests
                SET status = CASE
                        WHEN status = 'InProgress' THEN 'PendingApproval'
                        WHEN sub_status = 'Approved' THEN 'Approved'
                        WHEN sub_status = 'Rejected' THEN 'Rejected'
                        WHEN sub_status = 'Expired' THEN 'Expired'
                        WHEN sub_status = 'PartiallyApproved' THEN 'Approved'
                        ELSE status
                    END;
                """);

            migrationBuilder.DropColumn(
                name: "sub_status",
                schema: "access_catalog",
                table: "package_requests");

            migrationBuilder.DropColumn(
                name: "approval_flow_id",
                schema: "access_catalog",
                table: "approval_requirements");

            migrationBuilder.DropColumn(
                name: "access_item_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "approval_flow_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "request_scope_id",
                schema: "access_catalog",
                table: "access_grants");
        }
    }
}
