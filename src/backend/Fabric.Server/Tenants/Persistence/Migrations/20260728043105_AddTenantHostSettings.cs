using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantHostSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "host_assignment_mode",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "AllEmployees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "host_assignment_mode",
                schema: "tenancy",
                table: "tenants");
        }
    }
}
