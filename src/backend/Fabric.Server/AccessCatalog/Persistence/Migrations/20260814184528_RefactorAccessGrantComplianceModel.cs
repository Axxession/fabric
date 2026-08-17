using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessCatalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAccessGrantComplianceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_grant_locations",
                schema: "access_catalog");

            migrationBuilder.AddColumn<string>(
                name: "approval_status",
                schema: "access_catalog",
                table: "access_grants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "compliance_status",
                schema: "access_catalog",
                table: "access_grants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "compliant_until",
                schema: "access_catalog",
                table: "access_grants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_compliance_evaluated_at",
                schema: "access_catalog",
                table: "access_grants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "location_id",
                schema: "access_catalog",
                table: "access_grants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "replaced_by_id",
                schema: "access_catalog",
                table: "access_grants",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE access_catalog.access_grants
                SET approval_status = CASE
                    WHEN assignment_channel = 'CatalogRequest' THEN 'Approved'
                    ELSE 'NotRequired'
                END
                WHERE approval_status IS NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE access_catalog.access_grants
                SET compliance_status = 'Compliant'
                WHERE compliance_status IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "approval_status",
                schema: "access_catalog",
                table: "access_grants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "compliance_status",
                schema: "access_catalog",
                table: "access_grants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "location_id",
                schema: "access_catalog",
                table: "access_grants",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "grant_requirement_results",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    evidence_reference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grant_requirement_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_grant_requirement_results_access_grants_access_grant_id",
                        column: x => x.access_grant_id,
                        principalSchema: "access_catalog",
                        principalTable: "access_grants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grant_requirements",
                schema: "access_catalog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_policy_kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    derived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_grant_requirements", x => x.id);
                    table.ForeignKey(
                        name: "fk_grant_requirements_access_grants_access_grant_id",
                        column: x => x.access_grant_id,
                        principalSchema: "access_catalog",
                        principalTable: "access_grants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_grant_requirement_results_access_grant_id",
                schema: "access_catalog",
                table: "grant_requirement_results",
                column: "access_grant_id");

            migrationBuilder.CreateIndex(
                name: "ix_grant_requirement_results_tenant_id",
                schema: "access_catalog",
                table: "grant_requirement_results",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_grant_requirement_results_tenant_id_access_grant_id_requirement_definition_id",
                schema: "access_catalog",
                table: "grant_requirement_results",
                columns: new[] { "tenant_id", "access_grant_id", "requirement_definition_id" });

            migrationBuilder.CreateIndex(
                name: "IX_grant_requirements_access_grant_id",
                schema: "access_catalog",
                table: "grant_requirements",
                column: "access_grant_id");

            migrationBuilder.CreateIndex(
                name: "ix_grant_requirements_tenant_id",
                schema: "access_catalog",
                table: "grant_requirements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_grant_requirements_tenant_id_access_grant_id",
                schema: "access_catalog",
                table: "grant_requirements",
                columns: new[] { "tenant_id", "access_grant_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grant_requirement_results",
                schema: "access_catalog");

            migrationBuilder.DropTable(
                name: "grant_requirements",
                schema: "access_catalog");

            migrationBuilder.DropColumn(
                name: "approval_status",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "compliance_status",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "compliant_until",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "last_compliance_evaluated_at",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "location_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "replaced_by_id",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.CreateTable(
                name: "access_grant_locations",
                schema: "access_catalog",
                columns: table => new
                {
                    access_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_grant_locations", x => new { x.access_grant_id, x.location_id });
                    table.ForeignKey(
                        name: "fk_access_grant_locations_access_grants_access_grant_id",
                        column: x => x.access_grant_id,
                        principalSchema: "access_catalog",
                        principalTable: "access_grants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_locations_tenant_id",
                schema: "access_catalog",
                table: "access_grant_locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_locations_tenant_id_access_grant_id_location_id",
                schema: "access_catalog",
                table: "access_grant_locations",
                columns: new[] { "tenant_id", "access_grant_id", "location_id" },
                unique: true);
        }
    }
}
