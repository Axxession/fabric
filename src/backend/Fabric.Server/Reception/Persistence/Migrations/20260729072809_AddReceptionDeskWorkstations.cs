using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Reception.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceptionDeskWorkstations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reception_desk_workstations",
                schema: "reception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    api_key_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    api_key_salt = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reception_desk_workstations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reception_desk_workstations_tenant_id",
                schema: "reception",
                table: "reception_desk_workstations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_reception_desk_workstations_tenant_id_location_id",
                schema: "reception",
                table: "reception_desk_workstations",
                columns: new[] { "tenant_id", "location_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reception_desk_workstations",
                schema: "reception");
        }
    }
}
