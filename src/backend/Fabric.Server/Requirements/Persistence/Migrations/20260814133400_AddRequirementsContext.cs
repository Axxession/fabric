using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Requirements.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequirementsContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "requirements");

            migrationBuilder.CreateTable(
                name: "enforcement_zones",
                schema: "requirements",
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
                    table.PrimaryKey("pk_enforcement_zones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "requirement_definitions",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    evaluator_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requirement_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enforcement_zone_access_policies",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enforcement_zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enforcement_zone_access_policies", x => x.id);
                    table.ForeignKey(
                        name: "fk_enforcement_zone_access_policies_enforcement_zones_enforcement_zone_id",
                        column: x => x.enforcement_zone_id,
                        principalSchema: "requirements",
                        principalTable: "enforcement_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enforcement_zone_locations",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enforcement_zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enforcement_zone_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_enforcement_zone_locations_enforcement_zones_enforcement_zone_id",
                        column: x => x.enforcement_zone_id,
                        principalSchema: "requirements",
                        principalTable: "enforcement_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "zone_compliances",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enforcement_zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    calculated_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason_summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zone_compliances", x => x.id);
                    table.ForeignKey(
                        name: "fk_zone_compliances_enforcement_zones_enforcement_zone_id",
                        column: x => x.enforcement_zone_id,
                        principalSchema: "requirements",
                        principalTable: "enforcement_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contractor_job_requirement_policies",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enforcement_zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_job_requirement_policies", x => x.id);
                    table.ForeignKey(
                        name: "fk_contractor_job_requirement_policies_enforcement_zones_enforcement_zone_id",
                        column: x => x.enforcement_zone_id,
                        principalSchema: "requirements",
                        principalTable: "enforcement_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contractor_job_requirement_policies_requirement_definitions_requirement_definition_id",
                        column: x => x.requirement_definition_id,
                        principalSchema: "requirements",
                        principalTable: "requirement_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "requirement_evidence",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_reference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    file_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    content = table.Column<byte[]>(type: "bytea", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requirement_evidence", x => x.id);
                    table.ForeignKey(
                        name: "fk_requirement_evidence_requirement_definitions_requirement_definition_id",
                        column: x => x.requirement_definition_id,
                        principalSchema: "requirements",
                        principalTable: "requirement_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "zone_requirement_policies",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enforcement_zone_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zone_requirement_policies", x => x.id);
                    table.ForeignKey(
                        name: "fk_zone_requirement_policies_enforcement_zones_enforcement_zone_id",
                        column: x => x.enforcement_zone_id,
                        principalSchema: "requirements",
                        principalTable: "enforcement_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_zone_requirement_policies_requirement_definitions_requirement_definition_id",
                        column: x => x.requirement_definition_id,
                        principalSchema: "requirements",
                        principalTable: "requirement_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projected_zone_access_assignments",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_compliance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enforcement_zone_access_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projected_zone_access_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_projected_zone_access_assignments_enforcement_zone_access_policies_policy_id",
                        column: x => x.enforcement_zone_access_policy_id,
                        principalSchema: "requirements",
                        principalTable: "enforcement_zone_access_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_projected_zone_access_assignments_zone_compliances_zone_compliance_id",
                        column: x => x.zone_compliance_id,
                        principalSchema: "requirements",
                        principalTable: "zone_compliances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "zone_compliance_requirement_results",
                schema: "requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_compliance_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    evidence_reference = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_zone_compliance_requirement_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_zone_compliance_requirement_results_requirement_definitions_requirement_definition_id",
                        column: x => x.requirement_definition_id,
                        principalSchema: "requirements",
                        principalTable: "requirement_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_zone_compliance_requirement_results_zone_compliances_zone_compliance_id",
                        column: x => x.zone_compliance_id,
                        principalSchema: "requirements",
                        principalTable: "zone_compliances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contractor_job_requirement_policies_enforcement_zone_id",
                schema: "requirements",
                table: "contractor_job_requirement_policies",
                column: "enforcement_zone_id");

            migrationBuilder.CreateIndex(
                name: "IX_contractor_job_requirement_policies_requirement_definition_~",
                schema: "requirements",
                table: "contractor_job_requirement_policies",
                column: "requirement_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_requirement_policies_tenant_id",
                schema: "requirements",
                table: "contractor_job_requirement_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_job_requirement_policies_tenant_id_zone_jobtype_requirement",
                schema: "requirements",
                table: "contractor_job_requirement_policies",
                columns: new[] { "tenant_id", "enforcement_zone_id", "job_type_id", "requirement_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enforcement_zone_access_policies_enforcement_zone_id",
                schema: "requirements",
                table: "enforcement_zone_access_policies",
                column: "enforcement_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_enforcement_zone_access_policies_tenant_id",
                schema: "requirements",
                table: "enforcement_zone_access_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_enforcement_zone_access_policies_tenant_id_zone_access_item",
                schema: "requirements",
                table: "enforcement_zone_access_policies",
                columns: new[] { "tenant_id", "enforcement_zone_id", "access_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_enforcement_zone_locations_enforcement_zone_id",
                schema: "requirements",
                table: "enforcement_zone_locations",
                column: "enforcement_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_enforcement_zone_locations_tenant_id",
                schema: "requirements",
                table: "enforcement_zone_locations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_enforcement_zone_locations_tenant_id_location_id",
                schema: "requirements",
                table: "enforcement_zone_locations",
                columns: new[] { "tenant_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enforcement_zone_locations_tenant_id_zone_location",
                schema: "requirements",
                table: "enforcement_zone_locations",
                columns: new[] { "tenant_id", "enforcement_zone_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enforcement_zones_tenant_id",
                schema: "requirements",
                table: "enforcement_zones",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_enforcement_zones_tenant_id_code",
                schema: "requirements",
                table: "enforcement_zones",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projected_zone_access_assignments_enforcement_zone_access_p~",
                schema: "requirements",
                table: "projected_zone_access_assignments",
                column: "enforcement_zone_access_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_projected_zone_access_assignments_tenant_id",
                schema: "requirements",
                table: "projected_zone_access_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_projected_zone_access_assignments_tenant_id_zone_compliance_policy_location",
                schema: "requirements",
                table: "projected_zone_access_assignments",
                columns: new[] { "tenant_id", "zone_compliance_id", "enforcement_zone_access_policy_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projected_zone_access_assignments_zone_compliance_id",
                schema: "requirements",
                table: "projected_zone_access_assignments",
                column: "zone_compliance_id");

            migrationBuilder.CreateIndex(
                name: "ix_requirement_definitions_tenant_id",
                schema: "requirements",
                table: "requirement_definitions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_requirement_definitions_tenant_id_code",
                schema: "requirements",
                table: "requirement_definitions",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requirement_evidence_requirement_definition_id",
                schema: "requirements",
                table: "requirement_evidence",
                column: "requirement_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_requirement_evidence_tenant_id",
                schema: "requirements",
                table: "requirement_evidence",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_requirement_evidence_tenant_id_identity_requirement",
                schema: "requirements",
                table: "requirement_evidence",
                columns: new[] { "tenant_id", "identity_id", "requirement_definition_id" });

            migrationBuilder.CreateIndex(
                name: "IX_zone_compliance_requirement_results_requirement_definition_~",
                schema: "requirements",
                table: "zone_compliance_requirement_results",
                column: "requirement_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_compliance_requirement_results_tenant_id",
                schema: "requirements",
                table: "zone_compliance_requirement_results",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_compliance_requirement_results_zone_compliance_id",
                schema: "requirements",
                table: "zone_compliance_requirement_results",
                column: "zone_compliance_id");

            migrationBuilder.CreateIndex(
                name: "IX_zone_compliances_enforcement_zone_id",
                schema: "requirements",
                table: "zone_compliances",
                column: "enforcement_zone_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_compliances_tenant_id",
                schema: "requirements",
                table: "zone_compliances",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_compliances_tenant_id_identity_zone",
                schema: "requirements",
                table: "zone_compliances",
                columns: new[] { "tenant_id", "identity_id", "enforcement_zone_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_zone_requirement_policies_enforcement_zone_id",
                schema: "requirements",
                table: "zone_requirement_policies",
                column: "enforcement_zone_id");

            migrationBuilder.CreateIndex(
                name: "IX_zone_requirement_policies_requirement_definition_id",
                schema: "requirements",
                table: "zone_requirement_policies",
                column: "requirement_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_requirement_policies_tenant_id",
                schema: "requirements",
                table: "zone_requirement_policies",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_zone_requirement_policies_tenant_id_zone_requirement_subject",
                schema: "requirements",
                table: "zone_requirement_policies",
                columns: new[] { "tenant_id", "enforcement_zone_id", "requirement_definition_id", "subject_kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_job_requirement_policies",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "enforcement_zone_locations",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "projected_zone_access_assignments",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "requirement_evidence",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "zone_compliance_requirement_results",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "zone_requirement_policies",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "enforcement_zone_access_policies",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "zone_compliances",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "requirement_definitions",
                schema: "requirements");

            migrationBuilder.DropTable(
                name: "enforcement_zones",
                schema: "requirements");
        }
    }
}
