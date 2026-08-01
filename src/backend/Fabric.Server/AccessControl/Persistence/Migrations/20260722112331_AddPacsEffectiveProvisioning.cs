using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPacsEffectiveProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provisioning_timing",
                schema: "access_control",
                table: "access_level_targets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "pacs_provisioning_reconciliations",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_known_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_provisioning_reconciliations", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacs_provisioning_reconciliations_access_control_systems_access_control_system_id",
                        column: x => x.access_control_system_id,
                        principalSchema: "access_control",
                        principalTable: "access_control_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pacs_provisionings",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_level_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provisioning_timing = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    native_assignment_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    provisioned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_provisionings", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacs_provisionings_access_control_systems_access_control_system_id",
                        column: x => x.access_control_system_id,
                        principalSchema: "access_control",
                        principalTable: "access_control_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pacs_provisionings_access_level_targets_access_level_target_id",
                        column: x => x.access_level_target_id,
                        principalSchema: "access_control",
                        principalTable: "access_level_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pacs_provisioning_source_assignments",
                schema: "access_control",
                columns: table => new
                {
                    pacs_provisioning_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pacs_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_provisioning_source_assignments", x => new { x.pacs_provisioning_id, x.pacs_assignment_id });
                    table.ForeignKey(
                        name: "fk_pacs_provisioning_source_assignments_pacs_assignments_pacs_assignment_id",
                        column: x => x.pacs_assignment_id,
                        principalSchema: "access_control",
                        principalTable: "pacs_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pacs_provisioning_source_assignments_pacs_provisionings_pacs_provisioning_id",
                        column: x => x.pacs_provisioning_id,
                        principalSchema: "access_control",
                        principalTable: "pacs_provisionings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pacs_provisioning_reconciliations_access_control_system_id",
                schema: "access_control",
                table: "pacs_provisioning_reconciliations",
                column: "access_control_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisioning_reconciliations_tenant_id",
                schema: "access_control",
                table: "pacs_provisioning_reconciliations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisioning_reconciliations_tenant_id_identity_system",
                schema: "access_control",
                table: "pacs_provisioning_reconciliations",
                columns: new[] { "tenant_id", "identity_id", "access_control_system_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisioning_reconciliations_tenant_id_scheduled_for",
                schema: "access_control",
                table: "pacs_provisioning_reconciliations",
                columns: new[] { "tenant_id", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "IX_pacs_provisioning_source_assignments_pacs_assignment_id",
                schema: "access_control",
                table: "pacs_provisioning_source_assignments",
                column: "pacs_assignment_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisioning_source_assignments_tenant_id",
                schema: "access_control",
                table: "pacs_provisioning_source_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisioning_source_assignments_tenant_id_pacs_assignment_id",
                schema: "access_control",
                table: "pacs_provisioning_source_assignments",
                columns: new[] { "tenant_id", "pacs_assignment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_pacs_provisionings_access_control_system_id",
                schema: "access_control",
                table: "pacs_provisionings",
                column: "access_control_system_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacs_provisionings_access_level_target_id",
                schema: "access_control",
                table: "pacs_provisionings",
                column: "access_level_target_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisionings_tenant_id",
                schema: "access_control",
                table: "pacs_provisionings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisionings_tenant_id_identity_system_target_status",
                schema: "access_control",
                table: "pacs_provisionings",
                columns: new[] { "tenant_id", "identity_id", "access_control_system_id", "access_level_target_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pacs_provisionings_tenant_id_status_scheduled_for",
                schema: "access_control",
                table: "pacs_provisionings",
                columns: new[] { "tenant_id", "status", "scheduled_for" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pacs_provisioning_reconciliations",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "pacs_provisioning_source_assignments",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "pacs_provisionings",
                schema: "access_control");

            migrationBuilder.DropColumn(
                name: "provisioning_timing",
                schema: "access_control",
                table: "access_level_targets");
        }
    }
}
