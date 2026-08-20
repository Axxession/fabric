using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningRequirementRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_requirement_rules",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requirement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    satisfaction_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    minimum_score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_requirement_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_learning_requirement_rules_tenant_id",
                schema: "sagas",
                table: "learning_requirement_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_learning_requirement_rules_tenant_id_requirement_definition_id_course_id",
                schema: "sagas",
                table: "learning_requirement_rules",
                columns: new[] { "tenant_id", "requirement_definition_id", "course_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "learning_requirement_rules",
                schema: "sagas");
        }
    }
}
