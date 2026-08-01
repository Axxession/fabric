using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Visitors.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorIdentityLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "identity_id",
                schema: "visitors",
                table: "visitors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_visitors_tenant_id_identity_id",
                schema: "visitors",
                table: "visitors",
                columns: new[] { "tenant_id", "identity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_visitors_tenant_id_identity_id",
                schema: "visitors",
                table: "visitors");

            migrationBuilder.DropColumn(
                name: "identity_id",
                schema: "visitors",
                table: "visitors");
        }
    }
}
