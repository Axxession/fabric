using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUtcMidnightAndCredentialPacsLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "native_assignment_id",
                schema: "access_control",
                table: "credential_pacs_assignments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "native_assignment_id",
                schema: "access_control",
                table: "credential_pacs_assignments");
        }
    }
}
