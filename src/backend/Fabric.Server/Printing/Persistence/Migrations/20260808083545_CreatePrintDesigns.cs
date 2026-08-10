using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Printing.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreatePrintDesigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "printing");

            migrationBuilder.CreateTable(
                name: "print_designs",
                schema: "printing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SurfaceKind = table.Column<int>(type: "integer", nullable: false),
                    DesignJson = table.Column<string>(type: "jsonb", nullable: false),
                    MediaLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MediaWidth = table.Column<double>(type: "double precision", nullable: false),
                    MediaHeight = table.Column<double>(type: "double precision", nullable: false),
                    MediaOrientation = table.Column<int>(type: "integer", nullable: false),
                    Dpi = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_print_designs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_print_designs_MediaLabel",
                schema: "printing",
                table: "print_designs",
                column: "MediaLabel");

            migrationBuilder.CreateIndex(
                name: "IX_print_designs_Name_Version",
                schema: "printing",
                table: "print_designs",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_print_designs_SurfaceKind",
                schema: "printing",
                table: "print_designs",
                column: "SurfaceKind");

            migrationBuilder.CreateIndex(
                name: "ix_print_designs_tenant_id",
                schema: "printing",
                table: "print_designs",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "print_designs",
                schema: "printing");
        }
    }
}
