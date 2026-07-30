using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessCatalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessGrantRevokeMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "revoke_cause",
                schema: "access_catalog",
                table: "access_grants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revoked_by",
                schema: "access_catalog",
                table: "access_grants",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "revoke_cause",
                schema: "access_catalog",
                table: "access_grants");

            migrationBuilder.DropColumn(
                name: "revoked_by",
                schema: "access_catalog",
                table: "access_grants");
        }
    }
}
