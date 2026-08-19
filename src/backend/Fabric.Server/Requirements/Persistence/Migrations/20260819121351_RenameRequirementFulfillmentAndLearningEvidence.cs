using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Requirements.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameRequirementFulfillmentAndLearningEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "evaluator_kind",
                schema: "requirements",
                table: "requirement_definitions",
                newName: "fulfillment_kind");

            migrationBuilder.Sql("""
                UPDATE requirements.requirement_definitions
                SET fulfillment_kind = 'Document'
                WHERE fulfillment_kind IN ('UploadedDocument', 'ExternalCheck', 'Escort', 'Computed');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE requirements.requirement_definitions
                SET fulfillment_kind = CASE
                    WHEN fulfillment_kind = 'Learning' THEN 'Computed'
                    ELSE 'UploadedDocument'
                END;
                """);

            migrationBuilder.RenameColumn(
                name: "fulfillment_kind",
                schema: "requirements",
                table: "requirement_definitions",
                newName: "evaluator_kind");
        }
    }
}
