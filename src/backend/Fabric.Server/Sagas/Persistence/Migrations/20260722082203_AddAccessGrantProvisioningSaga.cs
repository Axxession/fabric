using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessGrantProvisioningSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_grant_provisioning_saga_events",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    saga_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_grant_provisioning_saga_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "access_grant_provisioning_sagas",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_grant_provisioning_sagas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_provisioning_saga_events_created_at",
                schema: "sagas",
                table: "access_grant_provisioning_saga_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_provisioning_saga_events_next_retry_at",
                schema: "sagas",
                table: "access_grant_provisioning_saga_events",
                column: "next_retry_at");

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_provisioning_saga_events_processed_at",
                schema: "sagas",
                table: "access_grant_provisioning_saga_events",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_provisioning_saga_events_tenant_id",
                schema: "sagas",
                table: "access_grant_provisioning_saga_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_provisioning_sagas_access_grant_id",
                schema: "sagas",
                table: "access_grant_provisioning_sagas",
                column: "access_grant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_provisioning_sagas_next_retry_at",
                schema: "sagas",
                table: "access_grant_provisioning_sagas",
                column: "next_retry_at");

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_provisioning_sagas_tenant_id",
                schema: "sagas",
                table: "access_grant_provisioning_sagas",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_grant_provisioning_saga_events",
                schema: "sagas");

            migrationBuilder.DropTable(
                name: "access_grant_provisioning_sagas",
                schema: "sagas");
        }
    }
}
