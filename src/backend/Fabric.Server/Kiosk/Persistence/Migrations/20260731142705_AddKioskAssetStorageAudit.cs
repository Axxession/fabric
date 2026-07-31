using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Kiosk.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKioskAssetStorageAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "uploaded_by_display_name",
                schema: "kiosk",
                table: "assets",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_by_email",
                schema: "kiosk",
                table: "assets",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uploaded_by_oid",
                schema: "kiosk",
                table: "assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                schema: "kiosk",
                table: "assets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Private");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uploaded_by_display_name",
                schema: "kiosk",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "uploaded_by_email",
                schema: "kiosk",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "uploaded_by_oid",
                schema: "kiosk",
                table: "assets");

            migrationBuilder.DropColumn(
                name: "visibility",
                schema: "kiosk",
                table: "assets");
        }
    }
}
