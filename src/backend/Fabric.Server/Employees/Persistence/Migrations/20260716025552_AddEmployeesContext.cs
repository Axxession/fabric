using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Employees.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeesContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "employees");

            migrationBuilder.CreateTable(
                name: "organization_units",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_units", x => x.id);
                    table.ForeignKey(
                        name: "FK_organization_units_organization_units_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "employees",
                        principalTable: "organization_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manager_employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    employee_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    job_title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hire_date = table.Column<DateOnly>(type: "date", nullable: true),
                    termination_date = table.Column<DateOnly>(type: "date", nullable: true),
                    leave_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.id);
                    table.ForeignKey(
                        name: "FK_employees_employees_manager_employee_id",
                        column: x => x.manager_employee_id,
                        principalSchema: "employees",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_organization_units_organization_unit_id",
                        column: x => x.organization_unit_id,
                        principalSchema: "employees",
                        principalTable: "organization_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_unit_closures",
                schema: "employees",
                columns: table => new
                {
                    ancestor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    descendant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_unit_closures", x => new { x.ancestor_id, x.descendant_id });
                    table.ForeignKey(
                        name: "FK_organization_unit_closures_organization_units_ancestor_id",
                        column: x => x.ancestor_id,
                        principalSchema: "employees",
                        principalTable: "organization_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_unit_closures_organization_units_descendant_id",
                        column: x => x.descendant_id,
                        principalSchema: "employees",
                        principalTable: "organization_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employees_manager_employee_id",
                schema: "employees",
                table: "employees",
                column: "manager_employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_organization_unit_id",
                schema: "employees",
                table: "employees",
                column: "organization_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id",
                schema: "employees",
                table: "employees",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_employee_number",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "employee_number" },
                unique: true,
                filter: "employee_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_identity_id",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "identity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_manager_employee_id",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "manager_employee_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_organization_unit_id",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "organization_unit_id" });

            migrationBuilder.CreateIndex(
                name: "ix_employees_tenant_id_status",
                schema: "employees",
                table: "employees",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_org_unit_closures_tenant_id_ancestor_id_depth",
                schema: "employees",
                table: "organization_unit_closures",
                columns: new[] { "tenant_id", "ancestor_id", "depth" });

            migrationBuilder.CreateIndex(
                name: "ix_org_unit_closures_tenant_id_descendant_id_depth",
                schema: "employees",
                table: "organization_unit_closures",
                columns: new[] { "tenant_id", "descendant_id", "depth" });

            migrationBuilder.CreateIndex(
                name: "IX_organization_unit_closures_descendant_id",
                schema: "employees",
                table: "organization_unit_closures",
                column: "descendant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_unit_closures_tenant_id",
                schema: "employees",
                table: "organization_unit_closures",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_units_parent_id",
                schema: "employees",
                table: "organization_units",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_tenant_id",
                schema: "employees",
                table: "organization_units",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_tenant_id_code",
                schema: "employees",
                table: "organization_units",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_tenant_id_is_active",
                schema: "employees",
                table: "organization_units",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_tenant_id_name",
                schema: "employees",
                table: "organization_units",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_units_tenant_id_parent_id",
                schema: "employees",
                table: "organization_units",
                columns: new[] { "tenant_id", "parent_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employees",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "organization_unit_closures",
                schema: "employees");

            migrationBuilder.DropTable(
                name: "organization_units",
                schema: "employees");
        }
    }
}
