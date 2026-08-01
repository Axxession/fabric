using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeLifecycleAutomationSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_access_automation_reconciliations",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_employee_access_automation_reconciliations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_lifecycle_automation_settings",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    disable_employee_on_leave = table.Column<bool>(type: "boolean", nullable: false),
                    disabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reenabled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_full_reconciled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_lifecycle_automation_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_lifecycle_ou_package_rules",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_lifecycle_ou_package_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_lifecycle_persona_package_rules",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_lifecycle_persona_package_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employee_access_automation_reconciliations_tenant_id",
                schema: "sagas",
                table: "employee_access_automation_reconciliations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_access_automation_reconciliations_tenant_id_employee_id",
                schema: "sagas",
                table: "employee_access_automation_reconciliations",
                columns: new[] { "tenant_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_access_automation_reconciliations_tenant_id_scheduled_for",
                schema: "sagas",
                table: "employee_access_automation_reconciliations",
                columns: new[] { "tenant_id", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_automation_settings_tenant_id",
                schema: "sagas",
                table: "employee_lifecycle_automation_settings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_ou_package_rules_tenant_id",
                schema: "sagas",
                table: "employee_lifecycle_ou_package_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_ou_package_rules_tenant_id_ou_package_id",
                schema: "sagas",
                table: "employee_lifecycle_ou_package_rules",
                columns: new[] { "tenant_id", "organization_unit_id", "package_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_persona_package_rules_tenant_id",
                schema: "sagas",
                table: "employee_lifecycle_persona_package_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_persona_package_rules_tenant_id_persona_package_id",
                schema: "sagas",
                table: "employee_lifecycle_persona_package_rules",
                columns: new[] { "tenant_id", "persona_id", "package_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_access_automation_reconciliations",
                schema: "sagas");

            migrationBuilder.DropTable(
                name: "employee_lifecycle_automation_settings",
                schema: "sagas");

            migrationBuilder.DropTable(
                name: "employee_lifecycle_ou_package_rules",
                schema: "sagas");

            migrationBuilder.DropTable(
                name: "employee_lifecycle_persona_package_rules",
                schema: "sagas");
        }
    }
}
