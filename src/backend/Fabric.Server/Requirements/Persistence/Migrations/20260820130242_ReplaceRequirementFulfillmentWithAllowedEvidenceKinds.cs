using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Requirements.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRequirementFulfillmentWithAllowedEvidenceKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "allowed_evidence_kinds",
                schema: "requirements",
                table: "requirement_definitions",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.Sql("""
                UPDATE requirements.requirement_definitions
                SET allowed_evidence_kinds = CASE
                    WHEN fulfillment_kind = 'Learning' THEN ARRAY['CourseCompletion']::text[]
                    ELSE ARRAY['Document']::text[]
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE requirements.requirement_evidence
                SET evidence_kind = CASE
                    WHEN evidence_kind = 'LearningCourseCompletion' THEN 'CourseCompletion'
                    ELSE 'Document'
                END;
                """);

            migrationBuilder.DropColumn(
                name: "fulfillment_kind",
                schema: "requirements",
                table: "requirement_definitions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fulfillment_kind",
                schema: "requirements",
                table: "requirement_definitions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE requirements.requirement_definitions
                SET fulfillment_kind = CASE
                    WHEN 'CourseCompletion' = ANY(allowed_evidence_kinds) THEN 'Learning'
                    ELSE 'Document'
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE requirements.requirement_evidence
                SET evidence_kind = CASE
                    WHEN evidence_kind = 'CourseCompletion' THEN 'LearningCourseCompletion'
                    ELSE 'UploadedDocument'
                END;
                """);

            migrationBuilder.DropColumn(
                name: "allowed_evidence_kinds",
                schema: "requirements",
                table: "requirement_definitions");
        }
    }
}
