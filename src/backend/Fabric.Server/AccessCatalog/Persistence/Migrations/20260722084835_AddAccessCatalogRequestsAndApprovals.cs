using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessCatalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessCatalogRequestsAndApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "valid_until",
                schema: "access_catalog",
                table: "access_grants",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "duration_kind",
                schema: "access_catalog",
                table: "access_grants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "approval_groups",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_groups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "package_requests",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beneficiary_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    duration_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_package_requests_packages_package_id",
                        column: x => x.package_id,
                        principalSchema: "access_catalog",
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_definitions",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_approval_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organizational_approval_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    organizational_approval_levels = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_definitions", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_definitions_approval_groups_destination_approval_group_id",
                        column: x => x.destination_approval_group_id,
                        principalSchema: "access_catalog",
                        principalTable: "approval_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "approval_group_members",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responsible_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_group_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_group_members_approval_groups_approval_group_id",
                        column: x => x.approval_group_id,
                        principalSchema: "access_catalog",
                        principalTable: "approval_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_requirements",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    approval_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_approver_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    system_approval_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_requirements", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_requirements_approval_groups_approval_group_id",
                        column: x => x.approval_group_id,
                        principalSchema: "access_catalog",
                        principalTable: "approval_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_approval_requirements_package_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "access_catalog",
                        principalTable: "package_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package_request_locations",
                schema: "access_catalog",
                columns: table => new
                {
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_request_locations", x => new { x.request_id, x.location_id });
                    table.ForeignKey(
                        name: "fk_package_request_locations_package_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "access_catalog",
                        principalTable: "package_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "approval_decisions",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approver_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    decision_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_approval_decisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_approval_decisions_approval_requirements_approval_requirement_id",
                        column: x => x.approval_requirement_id,
                        principalSchema: "access_catalog",
                        principalTable: "approval_requirements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_approval_decisions_package_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "access_catalog",
                        principalTable: "package_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_decisions_approval_requirement_id",
                schema: "access_catalog",
                table: "approval_decisions",
                column: "approval_requirement_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_decisions_request_id",
                schema: "access_catalog",
                table: "approval_decisions",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_decisions_tenant_id",
                schema: "access_catalog",
                table: "approval_decisions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_decisions_tenant_id_approval_requirement_id",
                schema: "access_catalog",
                table: "approval_decisions",
                columns: new[] { "tenant_id", "approval_requirement_id" });

            migrationBuilder.CreateIndex(
                name: "ix_approval_decisions_tenant_id_request_id",
                schema: "access_catalog",
                table: "approval_decisions",
                columns: new[] { "tenant_id", "request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_approval_definitions_destination_approval_group_id",
                schema: "access_catalog",
                table: "approval_definitions",
                column: "destination_approval_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_definitions_tenant_id",
                schema: "access_catalog",
                table: "approval_definitions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_definitions_tenant_id_access_item_id",
                schema: "access_catalog",
                table: "approval_definitions",
                columns: new[] { "tenant_id", "access_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_approval_group_members_approval_group_id",
                schema: "access_catalog",
                table: "approval_group_members",
                column: "approval_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_group_members_tenant_id",
                schema: "access_catalog",
                table: "approval_group_members",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_group_members_tenant_id_group_identity_location",
                schema: "access_catalog",
                table: "approval_group_members",
                columns: new[] { "tenant_id", "approval_group_id", "identity_id", "responsible_location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_approval_groups_tenant_id",
                schema: "access_catalog",
                table: "approval_groups",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_groups_tenant_id_name",
                schema: "access_catalog",
                table: "approval_groups",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_approval_requirements_approval_group_id",
                schema: "access_catalog",
                table: "approval_requirements",
                column: "approval_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_approval_requirements_request_id",
                schema: "access_catalog",
                table: "approval_requirements",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requirements_tenant_id",
                schema: "access_catalog",
                table: "approval_requirements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_requirements_tenant_id_request_id_status",
                schema: "access_catalog",
                table: "approval_requirements",
                columns: new[] { "tenant_id", "request_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_approval_requirements_tenant_id_required_approver_identity_id_status",
                schema: "access_catalog",
                table: "approval_requirements",
                columns: new[] { "tenant_id", "required_approver_identity_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_package_request_locations_tenant_id",
                schema: "access_catalog",
                table: "package_request_locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_request_locations_tenant_id_request_id_location_id",
                schema: "access_catalog",
                table: "package_request_locations",
                columns: new[] { "tenant_id", "request_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_package_requests_package_id",
                schema: "access_catalog",
                table: "package_requests",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_requests_tenant_id",
                schema: "access_catalog",
                table: "package_requests",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_package_requests_tenant_id_beneficiary_identity_id_status",
                schema: "access_catalog",
                table: "package_requests",
                columns: new[] { "tenant_id", "beneficiary_identity_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_package_requests_tenant_id_package_id_status",
                schema: "access_catalog",
                table: "package_requests",
                columns: new[] { "tenant_id", "package_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_package_requests_tenant_id_requester_identity_id_status",
                schema: "access_catalog",
                table: "package_requests",
                columns: new[] { "tenant_id", "requester_identity_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_decisions",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "approval_definitions",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "approval_group_members",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "package_request_locations",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "approval_requirements",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "approval_groups",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "package_requests",
                schema: "access_catalog");

            migrationBuilder.DropColumn(
                name: "duration_kind",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "valid_until",
                schema: "access_catalog",
                table: "access_grants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
