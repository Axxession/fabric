using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Printing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintDesignDefaultRenderProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultRenderProfile",
                schema: "printing",
                table: "print_designs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultRenderProfile",
                schema: "printing",
                table: "print_designs");
        }
    }
}
