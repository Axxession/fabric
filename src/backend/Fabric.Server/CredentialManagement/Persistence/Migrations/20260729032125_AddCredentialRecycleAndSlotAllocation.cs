using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.CredentialManagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialRecycleAndSlotAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_credentials_tenant_id_identifier",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "recycle_grace_period",
                schema: "credential_management",
                table: "credential_types",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "recycle_policy",
                schema: "credential_management",
                table: "credential_types",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NeverReuse");

            migrationBuilder.AddColumn<bool>(
                name: "requires_confirmed_pacs_revocation",
                schema: "credential_management",
                table: "credential_types",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "next_candidate_number",
                schema: "credential_management",
                table: "credential_ranges",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql(
                """
                UPDATE credential_management.credential_ranges
                SET next_candidate_number = range_start;
                """);

            migrationBuilder.CreateTable(
                name: "credential_slots",
                schema: "credential_management",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_range_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    credential_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reusable_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_state_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credential_slots", x => x.id);
                    table.ForeignKey(
                        name: "FK_credential_slots_credential_ranges_credential_range_id",
                        column: x => x.credential_range_id,
                        principalSchema: "credential_management",
                        principalTable: "credential_ranges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_credential_type_identifier",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "credential_type_id", "identifier" });

            migrationBuilder.CreateIndex(
                name: "IX_credential_slots_credential_range_id",
                schema: "credential_management",
                table: "credential_slots",
                column: "credential_range_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_slots_tenant_id",
                schema: "credential_management",
                table: "credential_slots",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_credential_slots_tenant_id_credential_id",
                schema: "credential_management",
                table: "credential_slots",
                columns: new[] { "tenant_id", "credential_id" });

            migrationBuilder.CreateIndex(
                name: "ix_credential_slots_tenant_id_range_number",
                schema: "credential_management",
                table: "credential_slots",
                columns: new[] { "tenant_id", "credential_range_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credential_slots_tenant_id_status",
                schema: "credential_management",
                table: "credential_slots",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credential_slots",
                schema: "credential_management");

            migrationBuilder.DropIndex(
                name: "ix_credentials_tenant_id_credential_type_identifier",
                schema: "credential_management",
                table: "credentials");

            migrationBuilder.DropColumn(
                name: "recycle_grace_period",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "recycle_policy",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "requires_confirmed_pacs_revocation",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "next_candidate_number",
                schema: "credential_management",
                table: "credential_ranges");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_tenant_id_identifier",
                schema: "credential_management",
                table: "credentials",
                columns: new[] { "tenant_id", "identifier" },
                unique: true);
        }
    }
}
