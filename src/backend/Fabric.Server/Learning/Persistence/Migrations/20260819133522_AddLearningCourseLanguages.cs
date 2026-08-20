using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Learning.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningCourseLanguages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_course_versions_tenant_id_course_id_version_number",
                schema: "learning",
                table: "course_versions");

            migrationBuilder.AddColumn<Guid>(
                name: "course_language_id",
                schema: "learning",
                table: "course_versions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "course_languages",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_languages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_course_versions_tenant_id_course_language_id_version_number",
                schema: "learning",
                table: "course_versions",
                columns: new[] { "tenant_id", "course_language_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_course_languages_tenant_id",
                schema: "learning",
                table: "course_languages",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_languages_tenant_id_course_id_language_code",
                schema: "learning",
                table: "course_languages",
                columns: new[] { "tenant_id", "course_id", "language_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "course_languages",
                schema: "learning");

            migrationBuilder.DropIndex(
                name: "ix_course_versions_tenant_id_course_language_id_version_number",
                schema: "learning",
                table: "course_versions");

            migrationBuilder.DropColumn(
                name: "course_language_id",
                schema: "learning",
                table: "course_versions");

            migrationBuilder.CreateIndex(
                name: "ix_course_versions_tenant_id_course_id_version_number",
                schema: "learning",
                table: "course_versions",
                columns: new[] { "tenant_id", "course_id", "version_number" },
                unique: true);
        }
    }
}
