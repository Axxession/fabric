using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Learning.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "learning");

            migrationBuilder.CreateTable(
                name: "attempts",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_activity_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completion_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    success_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    score_scaled = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    is_scored = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "course_scos",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sco_identifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    launch_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    resource_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    manifest_order = table.Column<int>(type: "integer", nullable: false),
                    mastery_score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_scos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "course_versions",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    scorm_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    emits_score = table.Column<bool>(type: "boolean", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    manifest_checksum = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_course_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    current_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "enrollments",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assigned_by_identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    latest_attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_identity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enrollments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "launch_sessions",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sco_id = table.Column<Guid>(type: "uuid", nullable: true),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_launch_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scorm_progress",
                schema: "learning",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sco_id = table.Column<Guid>(type: "uuid", nullable: true),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scorm_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    completion_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    success_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    score_scaled = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    bookmark_location = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    session_time = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    suspend_data = table.Column<string>(type: "character varying(32000)", maxLength: 32000, nullable: true),
                    raw_cmi_data = table.Column<string>(type: "jsonb", nullable: false),
                    last_committed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scorm_progress", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attempts_tenant_id",
                schema: "learning",
                table: "attempts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_attempts_tenant_id_enrollment_id_started_at",
                schema: "learning",
                table: "attempts",
                columns: new[] { "tenant_id", "enrollment_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_course_scos_tenant_id",
                schema: "learning",
                table: "course_scos",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_scos_tenant_id_course_version_id_manifest_order",
                schema: "learning",
                table: "course_scos",
                columns: new[] { "tenant_id", "course_version_id", "manifest_order" });

            migrationBuilder.CreateIndex(
                name: "ix_course_versions_tenant_id",
                schema: "learning",
                table: "course_versions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_versions_tenant_id_course_id_version_number",
                schema: "learning",
                table: "course_versions",
                columns: new[] { "tenant_id", "course_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_courses_tenant_id",
                schema: "learning",
                table: "courses",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_courses_tenant_id_code",
                schema: "learning",
                table: "courses",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_tenant_id",
                schema: "learning",
                table: "enrollments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_enrollments_tenant_id_course_id_identity_id_status",
                schema: "learning",
                table: "enrollments",
                columns: new[] { "tenant_id", "course_id", "identity_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_launch_sessions_tenant_id",
                schema: "learning",
                table: "launch_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_launch_sessions_tenant_id_token",
                schema: "learning",
                table: "launch_sessions",
                columns: new[] { "tenant_id", "token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scorm_progress_tenant_id",
                schema: "learning",
                table: "scorm_progress",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_scorm_progress_tenant_id_attempt_id_sco_id",
                schema: "learning",
                table: "scorm_progress",
                columns: new[] { "tenant_id", "attempt_id", "sco_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attempts",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "course_scos",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "course_versions",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "courses",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "enrollments",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "launch_sessions",
                schema: "learning");

            migrationBuilder.DropTable(
                name: "scorm_progress",
                schema: "learning");
        }
    }
}
