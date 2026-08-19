using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Tenants.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateTenantIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_integrations",
                schema: "tenancy",
                columns: table => new
                {
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    data_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_integrations", x => new { x.tenant_id, x.name });
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_integrations_tenant_id_name",
                schema: "tenancy",
                table: "tenant_integrations",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO tenancy.tenant_integrations (tenant_id, name, data_json, created_at, updated_at)
                SELECT id,
                       'MicrosoftGraph',
                       jsonb_build_object(
                           'email', jsonb_build_object(
                               'isEnabled', true,
                               'fromEmail', graph_email_from_email,
                               'fromName', graph_email_from_name,
                               'azureTenantId', graph_email_azure_tenant_id,
                               'applicationId', graph_email_application_id,
                               'secret', graph_email_secret,
                               'saveSentItems', graph_email_save_sent_items
                           )
                       ),
                       CURRENT_TIMESTAMP,
                       CURRENT_TIMESTAMP
                FROM tenancy.tenants
                WHERE graph_email_from_email IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO tenancy.tenant_integrations (tenant_id, name, data_json, created_at, updated_at)
                SELECT id,
                       'Keycloak',
                       jsonb_build_object(
                           'adminApi', jsonb_build_object(
                               'isEnabled', true,
                               'url', keycloak_url,
                               'realm', keycloak_realm,
                               'clientId', keycloak_client_id,
                               'clientSecret', keycloak_client_secret
                           )
                       ),
                       CURRENT_TIMESTAMP,
                       CURRENT_TIMESTAMP
                FROM tenancy.tenants
                WHERE keycloak_url IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_integrations",
                schema: "tenancy");
        }
    }
}
