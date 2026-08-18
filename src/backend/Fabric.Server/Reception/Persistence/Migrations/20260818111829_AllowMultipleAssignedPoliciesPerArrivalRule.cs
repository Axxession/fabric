using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Reception.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleAssignedPoliciesPerArrivalRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_assigned_access_policies_arrival_id_rule_assignment_id",
                schema: "reception",
                table: "assigned_access_policies");

            migrationBuilder.CreateIndex(
                name: "ix_assigned_access_policies_arrival_id_rule_assignment_id",
                schema: "reception",
                table: "assigned_access_policies",
                columns: new[] { "arrival_id", "rule_assignment_id", "access_grant_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_assigned_access_policies_arrival_id_rule_assignment_id",
                schema: "reception",
                table: "assigned_access_policies");

            migrationBuilder.CreateIndex(
                name: "ix_assigned_access_policies_arrival_id_rule_assignment_id",
                schema: "reception",
                table: "assigned_access_policies",
                columns: new[] { "arrival_id", "rule_assignment_id" },
                unique: true);
        }
    }
}
