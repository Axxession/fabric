using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.CredentialManagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCredentialManagementModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_credentials_credential_reservations_reservation_id",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropTable(
                name: "credential_provisioning_transactions",
                schema: "credential_management");

            migrationBuilder.DropTable(
                name: "credential_reservations",
                schema: "credential_management");

            migrationBuilder.DropTable(
                name: "credential_type_targets",
                schema: "credential_management");

            migrationBuilder.DropIndex(
                name: "IX_credentials_reservation_id",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropIndex(
                name: "ix_credentials_tenant_id_type_number",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropIndex(
                name: "ix_credential_types_tenant_id_technology",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "credential_number",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "reservation_id",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "range_start",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "range_stop",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.AddColumn<string>(
                name: "identifier",
                schema: "credential_management",
                table: "credentials",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "allocation_mode",
                schema: "credential_management",
                table: "credential_types",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "credential_ranges",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    range_start = table.Column<long>(type: "bigint", nullable: false),
                    range_stop = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_ranges", x => x.id);
                    table.ForeignKey(
                        name: "FK_credential_ranges_credential_types_credential_type_id",
                        column: x => x.credential_type_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_identifier",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credential_ranges_credential_type_id",
                schema: "credential_management",
                table: "credential_ranges",
                column: "credential_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_ranges_tenant_id",
                schema: "credential_management",
                table: "credential_ranges",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_ranges_tenant_id_credential_type_id",
                schema: "credential_management",
                table: "credential_ranges",
                columns: new[] { "tenant_id", "credential_type_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credential_ranges",
                schema: "credential_management");

            migrationBuilder.DropIndex(
                name: "ix_credentials_tenant_id_identifier",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "identifier",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "allocation_mode",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.AddColumn<int>(
                name: "credential_number",
                schema: "credential_management",
                table: "credentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "reservation_id",
                schema: "credential_management",
                table: "credentials",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "range_start",
                schema: "credential_management",
                table: "credential_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "range_stop",
                schema: "credential_management",
                table: "credential_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "credential_reservations",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    credential_number = table.Column<int>(type: "integer", nullable: false),
                    credential_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reason_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    requested_by_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_reservations", x => x.id);
                    table.ForeignKey(
                        name: "FK_credential_reservations_credential_types_credential_type_id",
                        column: x => x.credential_type_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credential_type_targets",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    credential_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    provider_credential_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provisioning_timing = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_type_targets", x => x.id);
                    table.ForeignKey(
                        name: "FK_credential_type_targets_credential_types_credential_type_id",
                        column: x => x.credential_type_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credential_provisioning_transactions",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_control_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type_target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provisioned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_provisioning_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_credential_provisioning_transactions_credential_type_target~",
                        column: x => x.credential_type_target_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_type_targets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credential_provisioning_transactions_credentials_credential~",
                        column: x => x.credential_id,
                        principalSchema: "credential_management",
                        principalTable: "credentials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_credentials_reservation_id",
                schema: "credential_management",
                table: "credentials",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_type_number",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "credential_type_id", "credential_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credential_types_tenant_id_technology",
                schema: "credential_management",
                table: "credential_types",
                columns: new[] { "tenant_id", "technology" });

            migrationBuilder.CreateIndex(
                name: "IX_credential_provisioning_transactions_credential_id",
                schema: "credential_management",
                table: "credential_provisioning_transactions",
                column: "credential_id");

            migrationBuilder.CreateIndex(
                name: "IX_credential_provisioning_transactions_credential_type_target~",
                schema: "credential_management",
                table: "credential_provisioning_transactions",
                column: "credential_type_target_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_provisioning_transactions_tenant_id",
                schema: "credential_management",
                table: "credential_provisioning_transactions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_provisioning_transactions_tenant_id_credential_id",
                schema: "credential_management",
                table: "credential_provisioning_transactions",
                columns: new[] { "tenant_id", "credential_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_provisioning_transactions_tenant_id_scheduled_for",
                schema: "credential_management",
                table: "credential_provisioning_transactions",
                columns: new[] { "tenant_id", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_provisioning_transactions_tenant_id_system_status",
                schema: "credential_management",
                table: "credential_provisioning_transactions",
                columns: new[] { "tenant_id", "access_control_system_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_credential_reservations_credential_type_id",
                schema: "credential_management",
                table: "credential_reservations",
                column: "credential_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_reservations_tenant_id",
                schema: "credential_management",
                table: "credential_reservations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_reservations_tenant_id_active_number",
                schema: "credential_management",
                table: "credential_reservations",
                columns: new[] { "tenant_id", "credential_type_id", "credential_number" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_credential_reservations_tenant_id_identity_id",
                schema: "credential_management",
                table: "credential_reservations",
                columns: new[] { "tenant_id", "identity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_reservations_tenant_id_source",
                schema: "credential_management",
                table: "credential_reservations",
                columns: new[] { "tenant_id", "source_kind", "source_id" });

            migrationBuilder.CreateIndex(
                name: "IX_credential_type_targets_credential_type_id",
                schema: "credential_management",
                table: "credential_type_targets",
                column: "credential_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_type_targets_tenant_id",
                schema: "credential_management",
                table: "credential_type_targets",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_type_targets_tenant_id_system",
                schema: "credential_management",
                table: "credential_type_targets",
                columns: new[] { "tenant_id", "access_control_system_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_type_targets_tenant_id_type_system",
                schema: "credential_management",
                table: "credential_type_targets",
                columns: new[] { "tenant_id", "credential_type_id", "access_control_system_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_credentials_credential_reservations_reservation_id",
                schema: "credential_management",
                table: "credentials",
                column: "reservation_id",
                principalSchema: "credential_management",
                principalTable: "credential_reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
