using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGenericProviderCredentialTypeTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "provider_credential_type_id",
                schema: "access_control",
                table: "credential_type_targets");

            migrationBuilder.AddColumn<string>(
                name: "target_type",
                schema: "access_control",
                table: "credential_type_targets",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "unipass");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "target_type",
                schema: "access_control",
                table: "credential_type_targets");

            migrationBuilder.AddColumn<Guid>(
                name: "provider_credential_type_id",
                schema: "access_control",
                table: "credential_type_targets",
                type: "uuid",
                nullable: true);
        }
    }
}
