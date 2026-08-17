using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Contractors.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorJobOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_identity_id",
                schema: "contractors",
                table: "contractor_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_contractor_jobs_tenant_id_created_by_identity_id",
                schema: "contractors",
                table: "contractor_jobs",
                columns: new[] { "tenant_id", "created_by_identity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_contractor_jobs_tenant_id_created_by_identity_id",
                schema: "contractors",
                table: "contractor_jobs");

            migrationBuilder.DropColumn(
                name: "created_by_identity_id",
                schema: "contractors",
                table: "contractor_jobs");
        }
    }
}
