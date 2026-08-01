using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Employees.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeLifecycleScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_lifecycle_events",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_lifecycle_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_lifecycle_events_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_lifecycle_recalculations",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    processing_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_lifecycle_recalculations", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_lifecycle_recalculations_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_lifecycle_states",
                schema: "employees",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_lifecycle_states", x => x.employee_id);
                    table.ForeignKey(
                        name: "FK_employee_lifecycle_states_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_lifecycle_events_employee_id",
                schema: "employees",
                table: "employee_lifecycle_events",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_events_tenant_id",
                schema: "employees",
                table: "employee_lifecycle_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_events_tenant_id_employee_id_effective_at",
                schema: "employees",
                table: "employee_lifecycle_events",
                columns: new[] { "tenant_id", "employee_id", "effective_at" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_lifecycle_recalculations_employee_id",
                schema: "employees",
                table: "employee_lifecycle_recalculations",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_recalculations_tenant_id",
                schema: "employees",
                table: "employee_lifecycle_recalculations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_recalculations_tenant_id_employee_id_schedule_reason",
                schema: "employees",
                table: "employee_lifecycle_recalculations",
                columns: new[] { "tenant_id", "employee_id", "scheduled_for", "reason" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_recalculations_tenant_id_status_scheduled_for",
                schema: "employees",
                table: "employee_lifecycle_recalculations",
                columns: new[] { "tenant_id", "status", "scheduled_for" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_states_tenant_id",
                schema: "employees",
                table: "employee_lifecycle_states",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_lifecycle_states_tenant_id_current_status",
                schema: "employees",
                table: "employee_lifecycle_states",
                columns: new[] { "tenant_id", "current_status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_lifecycle_events",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "employee_lifecycle_recalculations",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "employee_lifecycle_states",
                schema: "employees");
        }
    }
}
