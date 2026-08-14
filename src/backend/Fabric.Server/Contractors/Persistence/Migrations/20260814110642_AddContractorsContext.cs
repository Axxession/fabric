using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Contractors.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorsContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "contractors");

            migrationBuilder.CreateTable(
                name: "companies",
                schema: "contractors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    company_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_types",
                schema: "contractors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contractors",
                schema: "contractors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractors", x => x.id);
                    table.ForeignKey(
                        name: "FK_contractors_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "contractors",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contractor_jobs",
                schema: "contractors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    planned_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    planned_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_contractor_jobs_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "contractors",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_contractor_jobs_job_types_job_type_id",
                        column: x => x.job_type_id,
                        principalSchema: "contractors",
                        principalTable: "job_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contractor_job_assignments",
                schema: "contractors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contractor_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contractor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_job_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_contractor_job_assignments_contractor_jobs_contractor_job_id",
                        column: x => x.contractor_job_id,
                        principalSchema: "contractors",
                        principalTable: "contractor_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contractor_job_assignments_contractors_contractor_id",
                        column: x => x.contractor_id,
                        principalSchema: "contractors",
                        principalTable: "contractors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_id",
                schema: "contractors",
                table: "companies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_id_code",
                schema: "contractors",
                table: "companies",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_id_company_number",
                schema: "contractors",
                table: "companies",
                columns: new[] { "tenant_id", "company_number" },
                unique: true,
                filter: "company_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_companies_tenant_id_is_active",
                schema: "contractors",
                table: "companies",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_contractor_job_assignments_contractor_id",
                schema: "contractors",
                table: "contractor_job_assignments",
                column: "contractor_id");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_job_assignments_contractor_job_id",
                schema: "contractors",
                table: "contractor_job_assignments",
                column: "contractor_job_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_assignments_tenant_id",
                schema: "contractors",
                table: "contractor_job_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_assignments_tenant_id_assigned_from",
                schema: "contractors",
                table: "contractor_job_assignments",
                columns: new[] { "tenant_id", "assigned_from" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_assignments_tenant_id_assigned_until",
                schema: "contractors",
                table: "contractor_job_assignments",
                columns: new[] { "tenant_id", "assigned_until" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_assignments_tenant_id_contractor_id",
                schema: "contractors",
                table: "contractor_job_assignments",
                columns: new[] { "tenant_id", "contractor_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_assignments_tenant_id_contractor_job_id",
                schema: "contractors",
                table: "contractor_job_assignments",
                columns: new[] { "tenant_id", "contractor_job_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_assignments_tenant_id_status",
                schema: "contractors",
                table: "contractor_job_assignments",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_contractor_jobs_company_id",
                schema: "contractors",
                table: "contractor_jobs",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_jobs_job_type_id",
                schema: "contractors",
                table: "contractor_jobs",
                column: "job_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id",
                schema: "contractors",
                table: "contractor_jobs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id_company_id",
                schema: "contractors",
                table: "contractor_jobs",
                columns: new[] { "tenant_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id_job_type_id",
                schema: "contractors",
                table: "contractor_jobs",
                columns: new[] { "tenant_id", "job_type_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id_location_id",
                schema: "contractors",
                table: "contractor_jobs",
                columns: new[] { "tenant_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id_planned_end",
                schema: "contractors",
                table: "contractor_jobs",
                columns: new[] { "tenant_id", "planned_end" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id_planned_start",
                schema: "contractors",
                table: "contractor_jobs",
                columns: new[] { "tenant_id", "planned_start" });

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id_status",
                schema: "contractors",
                table: "contractor_jobs",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_contractors_company_id",
                schema: "contractors",
                table: "contractors",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractors_tenant_id",
                schema: "contractors",
                table: "contractors",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractors_tenant_id_archived_at",
                schema: "contractors",
                table: "contractors",
                columns: new[] { "tenant_id", "archived_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contractors_tenant_id_company_id",
                schema: "contractors",
                table: "contractors",
                columns: new[] { "tenant_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contractors_tenant_id_email",
                schema: "contractors",
                table: "contractors",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_job_types_tenant_id",
                schema: "contractors",
                table: "job_types",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_job_types_tenant_id_code",
                schema: "contractors",
                table: "job_types",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_job_types_tenant_id_is_active",
                schema: "contractors",
                table: "job_types",
                columns: new[] { "tenant_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_job_assignments",
                schema: "contractors");

            migrationBuilder.DropTable(
                name: "contractor_jobs",
                schema: "contractors");

            migrationBuilder.DropTable(
                name: "contractors",
                schema: "contractors");

            migrationBuilder.DropTable(
                name: "job_types",
                schema: "contractors");

            migrationBuilder.DropTable(
                name: "companies",
                schema: "contractors");
        }
    }
}
