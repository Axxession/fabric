using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Employees.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEmployeesCrudModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_employees_tenant_id_status",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "leave_started_at",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "employees",
                table: "employees");

            migrationBuilder.RenameColumn(
                name: "termination_date",
                schema: "employees",
                table: "employees",
                newName: "contract_start_date");

            migrationBuilder.RenameColumn(
                name: "suspended_at",
                schema: "employees",
                table: "employees",
                newName: "archived_at");

            migrationBuilder.RenameColumn(
                name: "hire_date",
                schema: "employees",
                table: "employees",
                newName: "contract_end_date");

            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                schema: "employees",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "directory_id",
                schema: "employees",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "employees",
                table: "employees",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "employees",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "employees",
                table: "employees",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "employee_leave_periods",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_leave_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_leave_periods_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_suspension_periods",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_suspension_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_suspension_periods_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_work_locations",
                schema: "employees",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_work_locations", x => new { x.employee_id, x.location_id });
                    table.ForeignKey(
                        name: "FK_employee_work_locations_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personas",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "employee_personas",
                schema: "employees",
                columns: table => new
                {
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    persona_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_personas", x => new { x.employee_id, x.persona_id });
                    table.ForeignKey(
                        name: "FK_employee_personas_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_personas_personas_persona_id",
                        column: x => x.persona_id,
                        principalSchema: "employees",
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_directory_id",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "directory_id" },
                unique: true,
                filter: "directory_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_email",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_leave_periods_employee_id",
                schema: "employees",
                table: "employee_leave_periods",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_leave_periods_tenant_id",
                schema: "employees",
                table: "employee_leave_periods",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_leave_periods_tenant_id_employee_id_dates",
                schema: "employees",
                table: "employee_leave_periods",
                columns: new[] { "tenant_id", "employee_id", "from_date", "until_date" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_personas_persona_id",
                schema: "employees",
                table: "employee_personas",
                column: "persona_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_personas_tenant_id",
                schema: "employees",
                table: "employee_personas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_personas_tenant_id_persona_id",
                schema: "employees",
                table: "employee_personas",
                columns: new[] { "tenant_id", "persona_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_suspension_periods_employee_id",
                schema: "employees",
                table: "employee_suspension_periods",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_suspension_periods_tenant_id",
                schema: "employees",
                table: "employee_suspension_periods",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_suspension_periods_tenant_id_employee_id_dates",
                schema: "employees",
                table: "employee_suspension_periods",
                columns: new[] { "tenant_id", "employee_id", "from_date", "until_date" });

            migrationBuilder.CreateIndex(
                name: "ix_employee_work_locations_tenant_id",
                schema: "employees",
                table: "employee_work_locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_work_locations_tenant_id_location_id",
                schema: "employees",
                table: "employee_work_locations",
                columns: new[] { "tenant_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_personas_tenant_id",
                schema: "employees",
                table: "personas",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_personas_tenant_id_is_active",
                schema: "employees",
                table: "personas",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_personas_tenant_id_name",
                schema: "employees",
                table: "personas",
                columns: new[] { "tenant_id", "name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_leave_periods",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "employee_personas",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "employee_suspension_periods",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "employee_work_locations",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "personas",
                schema: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_tenant_id_directory_id",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "ix_employees_tenant_id_email",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "birth_date",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "directory_id",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "employees",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "employees",
                table: "employees");

            migrationBuilder.RenameColumn(
                name: "contract_start_date",
                schema: "employees",
                table: "employees",
                newName: "termination_date");

            migrationBuilder.RenameColumn(
                name: "contract_end_date",
                schema: "employees",
                table: "employees",
                newName: "hire_date");

            migrationBuilder.RenameColumn(
                name: "archived_at",
                schema: "employees",
                table: "employees",
                newName: "suspended_at");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "leave_started_at",
                schema: "employees",
                table: "employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "employees",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_status",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "status" });
        }
    }
}
