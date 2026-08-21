using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Reception.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionKioskSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reception_kiosk_sessions",
                schema: "reception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kiosk_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arrival_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    current_step = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    stop_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    stop_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_interaction_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retention_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    requires_face_picture = table.Column<bool>(type: "boolean", nullable: false),
                    requires_identity_document_check = table.Column<bool>(type: "boolean", nullable: false),
                    requires_compliance_check = table.Column<bool>(type: "boolean", nullable: false),
                    face_picture_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    identity_document_check_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    compliance_check_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    onboard_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    face_picture_storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    identity_document_storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reception_kiosk_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reception_kiosk_sessions_tenant_id",
                schema: "reception",
                table: "reception_kiosk_sessions",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_reception_kiosk_sessions_tenant_id_arrival_id_started_at",
                schema: "reception",
                table: "reception_kiosk_sessions",
                columns: new[] { "tenant_id", "arrival_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reception_kiosk_sessions_tenant_id_kiosk_id_started_at",
                schema: "reception",
                table: "reception_kiosk_sessions",
                columns: new[] { "tenant_id", "kiosk_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_reception_kiosk_sessions_tenant_id_retention_until",
                schema: "reception",
                table: "reception_kiosk_sessions",
                columns: new[] { "tenant_id", "retention_until" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reception_kiosk_sessions",
                schema: "reception");
        }
    }
}
