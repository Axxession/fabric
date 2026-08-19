using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyTenantIntegrationColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenants_graph_email_all_or_none",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tenants_keycloak_all_or_none",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "graph_email_application_id",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "graph_email_azure_tenant_id",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "graph_email_from_email",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "graph_email_from_name",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "graph_email_save_sent_items",
                schema: "tenancy",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "graph_email_secret",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "graph_email_application_id",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "graph_email_azure_tenant_id",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "graph_email_from_email",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "graph_email_from_name",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "graph_email_save_sent_items",
                schema: "tenancy",
                table: "tenants",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "graph_email_secret",
                schema: "tenancy",
                table: "tenants",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

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
                name: "ck_tenants_graph_email_all_or_none",
                schema: "tenancy",
                table: "tenants",
                sql: "(graph_email_from_email IS NULL AND graph_email_from_name IS NULL AND graph_email_azure_tenant_id IS NULL AND graph_email_application_id IS NULL AND graph_email_secret IS NULL AND graph_email_save_sent_items IS NULL) OR (graph_email_from_email IS NOT NULL AND graph_email_from_name IS NOT NULL AND graph_email_azure_tenant_id IS NOT NULL AND graph_email_application_id IS NOT NULL AND graph_email_secret IS NOT NULL AND graph_email_save_sent_items IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tenants_keycloak_all_or_none",
                schema: "tenancy",
                table: "tenants",
                sql: "(keycloak_url IS NULL AND keycloak_realm IS NULL AND keycloak_client_id IS NULL AND keycloak_client_secret IS NULL) OR (keycloak_url IS NOT NULL AND keycloak_realm IS NOT NULL AND keycloak_client_id IS NOT NULL AND keycloak_client_secret IS NOT NULL)");
        }
    }
}
