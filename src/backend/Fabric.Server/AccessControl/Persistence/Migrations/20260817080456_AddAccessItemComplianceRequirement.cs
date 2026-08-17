using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessItemComplianceRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_compliance_required",
                schema: "access_control",
                table: "access_items",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_compliance_required",
                schema: "access_control",
                table: "access_items");
        }
    }
}
