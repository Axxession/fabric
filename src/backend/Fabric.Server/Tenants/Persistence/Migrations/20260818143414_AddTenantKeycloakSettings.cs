using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantKeycloakSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "keycloak_client_id",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "keycloak_client_secret",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "keycloak_realm",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "keycloak_url",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenants_keycloak_all_or_none",
                schema: "tenancy",
                table: "tenants",
                sql: "(keycloak_url IS NULL AND keycloak_realm IS NULL AND keycloak_client_id IS NULL AND keycloak_client_secret IS NULL) OR (keycloak_url IS NOT NULL AND keycloak_realm IS NOT NULL AND keycloak_client_id IS NOT NULL AND keycloak_client_secret IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenants_keycloak_all_or_none",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "keycloak_client_id",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "keycloak_client_secret",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "keycloak_realm",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "keycloak_url",
                schema: "tenancy",
                table: "tenants");
        }
    }
}
