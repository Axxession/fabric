using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Reception.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractorJobArrivalAssignmentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_expected_arrivals_tenant_id_job_assignment_id",
                schema: "reception",
                table: "expected_arrivals",
                columns: new[] { "tenant_id", "job_assignment_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_expected_arrivals_tenant_id_job_assignment_id",
                schema: "reception",
                table: "expected_arrivals");
        }
    }
}
