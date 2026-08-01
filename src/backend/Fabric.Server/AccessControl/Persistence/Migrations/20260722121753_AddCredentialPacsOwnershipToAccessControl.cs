using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialPacsOwnershipToAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credential_type_targets",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_credential_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provisioning_timing = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_type_targets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credential_pacs_assignments",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provisioned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_pacs_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_credential_pacs_assignments_credential_type_targets_credential_type_target_id",
                        column: x => x.credential_type_target_id,
                        principalSchema: "access_control",
                        principalTable: "credential_type_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credential_pacs_assignments_credential_type_target_id",
                schema: "access_control",
                table: "credential_pacs_assignments",
                column: "credential_type_target_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_pacs_assignments_tenant_id",
                schema: "access_control",
                table: "credential_pacs_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_pacs_assignments_tenant_id_credential_id",
                schema: "access_control",
                table: "credential_pacs_assignments",
                columns: new[] { "tenant_id", "credential_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_pacs_assignments_tenant_id_scheduled_for",
                schema: "access_control",
                table: "credential_pacs_assignments",
                columns: new[] { "tenant_id", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_pacs_assignments_tenant_id_system_status",
                schema: "access_control",
                table: "credential_pacs_assignments",
                columns: new[] { "tenant_id", "access_control_system_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_type_targets_tenant_id",
                schema: "access_control",
                table: "credential_type_targets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_type_targets_tenant_id_system",
                schema: "access_control",
                table: "credential_type_targets",
                columns: new[] { "tenant_id", "access_control_system_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_type_targets_tenant_id_type_system",
                schema: "access_control",
                table: "credential_type_targets",
                columns: new[] { "tenant_id", "credential_type_id", "access_control_system_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credential_pacs_assignments",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "credential_type_targets",
                schema: "access_control");
        }
    }
}
