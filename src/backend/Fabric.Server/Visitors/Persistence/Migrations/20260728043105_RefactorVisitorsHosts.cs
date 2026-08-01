using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Visitors.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorVisitorsHosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_visit_invitations_visits_visit_id",
                schema: "visitors",
                table: "visit_invitations");

            migrationBuilder.DropTable(
                name: "organizers",
                schema: "visitors");

            migrationBuilder.RenameColumn(
                name: "organizer_id",
                schema: "visitors",
                table: "visits",
                newName: "host_employee_id");

            migrationBuilder.CreateTable(
                name: "host_assignments",
                schema: "visitors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_host_assignments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_host_assignments_tenant_id",
                schema: "visitors",
                table: "host_assignments",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_host_assignments_tenant_id_employee_id",
                schema: "visitors",
                table: "host_assignments",
                columns: new[] { "tenant_id", "employee_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_visit_invitations_visits_visit_id",
                schema: "visitors",
                table: "visit_invitations",
                column: "visit_id",
                principalSchema: "visitors",
                principalTable: "visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_visit_invitations_visits_visit_id",
                schema: "visitors",
                table: "visit_invitations");

            migrationBuilder.DropTable(
                name: "host_assignments",
                schema: "visitors");

            migrationBuilder.RenameColumn(
                name: "host_employee_id",
                schema: "visitors",
                table: "visits",
                newName: "organizer_id");

            migrationBuilder.CreateTable(
                name: "organizers",
                schema: "visitors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organizers_tenant_id",
                schema: "visitors",
                table: "organizers",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizers_tenant_id_email",
                schema: "visitors",
                table: "organizers",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_visit_invitations_visits_visit_id",
                schema: "visitors",
                table: "visit_invitations",
                column: "visit_id",
                principalSchema: "visitors",
                principalTable: "visits",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
