using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.CredentialManagement.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialTypeQrIdentifierFormatting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "identifier_number_length",
                schema: "credential_management",
                table: "credential_types",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identifier_padding_character",
                schema: "credential_management",
                table: "credential_types",
                type: "character varying(1)",
                maxLength: 1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identifier_padding_direction",
                schema: "credential_management",
                table: "credential_types",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identifier_prefix",
                schema: "credential_management",
                table: "credential_types",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identifier_suffix",
                schema: "credential_management",
                table: "credential_types",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "identifier_number_length",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "identifier_padding_character",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "identifier_padding_direction",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "identifier_prefix",
                schema: "credential_management",
                table: "credential_types");

            migrationBuilder.DropColumn(
                name: "identifier_suffix",
                schema: "credential_management",
                table: "credential_types");
        }
    }
}
