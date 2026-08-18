using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorAssignmentAutomationMailbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contractor_assignment_automation_mailboxes",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_known_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lease_owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_assignment_automation_mailboxes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_assignment_automation_mailboxes_tenant_id",
                schema: "sagas",
                table: "contractor_assignment_automation_mailboxes",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_assignment_automation_mailboxes_tenant_id_assignment_id",
                schema: "sagas",
                table: "contractor_assignment_automation_mailboxes",
                columns: new[] { "tenant_id", "assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contractor_assignment_automation_mailboxes_tenant_id_lease_until",
                schema: "sagas",
                table: "contractor_assignment_automation_mailboxes",
                columns: new[] { "tenant_id", "lease_until" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_assignment_automation_mailboxes_tenant_id_scheduled_for",
                schema: "sagas",
                table: "contractor_assignment_automation_mailboxes",
                columns: new[] { "tenant_id", "scheduled_for" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_assignment_automation_mailboxes",
                schema: "sagas");
        }
    }
}
