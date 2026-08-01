using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPacsAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pacs_assignments",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_level_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_pacs_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacs_assignments_access_control_systems_access_control_system_id",
                        column: x => x.access_control_system_id,
                        principalSchema: "access_control",
                        principalTable: "access_control_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pacs_assignments_access_level_targets_access_level_target_id",
                        column: x => x.access_level_target_id,
                        principalSchema: "access_control",
                        principalTable: "access_level_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pacs_subjects",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    native_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    current_state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_subjects", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacs_subjects_access_control_systems_access_control_system_id",
                        column: x => x.access_control_system_id,
                        principalSchema: "access_control",
                        principalTable: "access_control_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pacs_assignments_access_control_system_id",
                schema: "access_control",
                table: "pacs_assignments",
                column: "access_control_system_id");

            migrationBuilder.CreateIndex(
                name: "IX_pacs_assignments_access_level_target_id",
                schema: "access_control",
                table: "pacs_assignments",
                column: "access_level_target_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_assignments_tenant_id",
                schema: "access_control",
                table: "pacs_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_assignments_tenant_id_identity_id_status",
                schema: "access_control",
                table: "pacs_assignments",
                columns: new[] { "tenant_id", "identity_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pacs_assignments_tenant_id_source_target_identity",
                schema: "access_control",
                table: "pacs_assignments",
                columns: new[] { "tenant_id", "source_assignment_id", "access_level_target_id", "identity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pacs_assignments_tenant_id_system_id_status",
                schema: "access_control",
                table: "pacs_assignments",
                columns: new[] { "tenant_id", "access_control_system_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_pacs_subjects_access_control_system_id",
                schema: "access_control",
                table: "pacs_subjects",
                column: "access_control_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_subjects_tenant_id",
                schema: "access_control",
                table: "pacs_subjects",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_subjects_tenant_id_system_id_identity_id",
                schema: "access_control",
                table: "pacs_subjects",
                columns: new[] { "tenant_id", "access_control_system_id", "identity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pacs_subjects_tenant_id_system_id_native_subject_id",
                schema: "access_control",
                table: "pacs_subjects",
                columns: new[] { "tenant_id", "access_control_system_id", "native_subject_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pacs_assignments",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "pacs_subjects",
                schema: "access_control");
        }
    }
}
