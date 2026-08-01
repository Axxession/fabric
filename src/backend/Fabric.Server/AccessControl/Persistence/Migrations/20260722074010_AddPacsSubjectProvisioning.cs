using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPacsSubjectProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "current_state",
                schema: "access_control",
                table: "pacs_subjects",
                newName: "state");

            migrationBuilder.AddColumn<string>(
                name: "email",
                schema: "access_control",
                table: "pacs_subjects",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "first_name",
                schema: "access_control",
                table: "pacs_subjects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "last_name",
                schema: "access_control",
                table: "pacs_subjects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_synchronized_at",
                schema: "access_control",
                table: "pacs_subjects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "pacs_subject_provisionings",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pacs_subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    desired_state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    desired_first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    desired_last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    desired_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_known_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pacs_subject_provisionings", x => x.id);
                    table.ForeignKey(
                        name: "fk_pacs_subject_provisionings_pacs_subjects_pacs_subject_id",
                        column: x => x.pacs_subject_id,
                        principalSchema: "access_control",
                        principalTable: "pacs_subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pacs_subject_provisionings_pacs_subject_id",
                schema: "access_control",
                table: "pacs_subject_provisionings",
                column: "pacs_subject_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_subject_provisionings_tenant_id",
                schema: "access_control",
                table: "pacs_subject_provisionings",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacs_subject_provisionings_tenant_id_pacs_subject_id",
                schema: "access_control",
                table: "pacs_subject_provisionings",
                columns: new[] { "tenant_id", "pacs_subject_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pacs_subject_provisionings_tenant_id_status_scheduled_for",
                schema: "access_control",
                table: "pacs_subject_provisionings",
                columns: new[] { "tenant_id", "status", "scheduled_for" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pacs_subject_provisionings",
                schema: "access_control");

            migrationBuilder.DropColumn(
                name: "email",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "first_name",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "last_name",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "last_synchronized_at",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.RenameColumn(
                name: "state",
                schema: "access_control",
                table: "pacs_subjects",
                newName: "current_state");
        }
    }
}
