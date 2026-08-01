using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessGrantMaterializationOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_grant_materialization_outcomes",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    access_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_grant_materialization_outcomes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_materialization_outcomes_tenant_grant_id",
                schema: "sagas",
                table: "access_grant_materialization_outcomes",
                columns: new[] { "tenant_id", "access_grant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_materialization_outcomes_tenant_grant_item_location",
                schema: "sagas",
                table: "access_grant_materialization_outcomes",
                columns: new[] { "tenant_id", "access_grant_id", "access_item_id", "location_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_access_grant_materialization_outcomes_tenant_id",
                schema: "sagas",
                table: "access_grant_materialization_outcomes",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_grant_materialization_outcomes",
                schema: "sagas");
        }
    }
}
