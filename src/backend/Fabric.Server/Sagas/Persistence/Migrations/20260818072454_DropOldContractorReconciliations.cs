using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropOldContractorReconciliations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_job_access_automation_reconciliations",
                schema: "sagas");

            migrationBuilder.DropTable(
                name: "contractor_job_onboarding_reconciliations",
                schema: "sagas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contractor_job_access_automation_reconciliations",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_known_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_job_access_automation_reconciliations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_job_onboarding_reconciliations",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_known_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_job_onboarding_reconciliations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_access_automation_reconciliations_tenant_id",
                schema: "sagas",
                table: "contractor_job_access_automation_reconciliations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_access_automation_reconciliations_tenant_id_assignment_id",
                schema: "sagas",
                table: "contractor_job_access_automation_reconciliations",
                columns: new[] { "tenant_id", "assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_access_automation_reconciliations_tenant_id_scheduled_for",
                schema: "sagas",
                table: "contractor_job_access_automation_reconciliations",
                columns: new[] { "tenant_id", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_onboarding_reconciliations_tenant_id",
                schema: "sagas",
                table: "contractor_job_onboarding_reconciliations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_onboarding_reconciliations_tenant_id_assignment_id",
                schema: "sagas",
                table: "contractor_job_onboarding_reconciliations",
                columns: new[] { "tenant_id", "assignment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_onboarding_reconciliations_tenant_id_scheduled_for",
                schema: "sagas",
                table: "contractor_job_onboarding_reconciliations",
                columns: new[] { "tenant_id", "scheduled_for" });
        }
    }
}
