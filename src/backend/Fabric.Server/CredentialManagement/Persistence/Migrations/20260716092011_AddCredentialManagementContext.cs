using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.CredentialManagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialManagementContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "credential_management");

            migrationBuilder.CreateTable(
                name: "credential_types",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    technology = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    range_start = table.Column<int>(type: "integer", nullable: false),
                    range_stop = table.Column<int>(type: "integer", nullable: false),
                    near_limit_threshold = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "credential_reservations",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_number = table.Column<int>(type: "integer", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_credential_type_targets_credential_types_credential_type_id",
                        column: x => x.credential_type_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credentials",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_number = table.Column<int>(type: "integer", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    duration_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason_text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_credentials_credential_reservations_reservation_id",
                        column: x => x.reservation_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credentials_credential_types_credential_type_id",
                        column: x => x.credential_type_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credential_provisioning_transactions",
                schema: "credential_management",
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

            migrationBuilder.CreateIndex(
                name: "ix_credential_types_tenant_id",
                schema: "credential_management",
                table: "credential_types",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_types_tenant_id_name",
                schema: "credential_management",
                table: "credential_types",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credential_types_tenant_id_status",
                schema: "credential_management",
                table: "credential_types",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_types_tenant_id_technology",
                schema: "credential_management",
                table: "credential_types",
                columns: new[] { "tenant_id", "technology" });

            migrationBuilder.CreateIndex(
                name: "IX_credentials_credential_type_id",
                schema: "credential_management",
                table: "credentials",
                column: "credential_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_credentials_reservation_id",
                schema: "credential_management",
                table: "credentials",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id",
                schema: "credential_management",
                table: "credentials",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_identity_id",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "identity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_source",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "source_kind", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_status",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_type_number",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "credential_type_id", "credential_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credential_provisioning_transactions",
                schema: "credential_management");

            migrationBuilder.DropTable(
                name: "credential_type_targets",
                schema: "credential_management");

            migrationBuilder.DropTable(
                name: "credentials",
                schema: "credential_management");

            migrationBuilder.DropTable(
                name: "credential_reservations",
                schema: "credential_management");

            migrationBuilder.DropTable(
                name: "credential_types",
                schema: "credential_management");
        }
    }
}
