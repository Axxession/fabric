using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorJobAutomationSagas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contractor_job_access_automation_reconciliations",
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
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
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
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("pk_contractor_job_onboarding_reconciliations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_job_package_rules",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_job_package_rules", x => x.id);
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

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_package_rules_tenant_id",
                schema: "sagas",
                table: "contractor_job_package_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_package_rules_tenant_id_job_type_package_location",
                schema: "sagas",
                table: "contractor_job_package_rules",
                columns: new[] { "tenant_id", "job_type_id", "package_id", "location_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_job_access_automation_reconciliations",
                schema: "sagas");

            migrationBuilder.DropTable(
                name: "contractor_job_onboarding_reconciliations",
                schema: "sagas");

            migrationBuilder.DropTable(
                name: "contractor_job_package_rules",
                schema: "sagas");
        }
    }
}
