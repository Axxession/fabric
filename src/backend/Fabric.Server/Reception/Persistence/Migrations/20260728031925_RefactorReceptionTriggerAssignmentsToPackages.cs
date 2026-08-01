using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Reception.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorReceptionTriggerAssignmentsToPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_access_rule_assignments_access_level_type_id",
                schema: "reception",
                table: "access_rule_assignments");

            migrationBuilder.DropIndex(
                name: "ix_access_rule_assignments_location_id",
                schema: "reception",
                table: "access_rule_assignments");

            migrationBuilder.DropColumn(
                name: "access_level_type_id",
                schema: "reception",
                table: "assigned_access_policies");

            migrationBuilder.DropColumn(
                name: "access_level_type_id",
                schema: "reception",
                table: "access_rule_assignments");

            migrationBuilder.DropColumn(
                name: "location_id",
                schema: "reception",
                table: "access_rule_assignments");

            migrationBuilder.RenameColumn(
                name: "system_id",
                schema: "reception",
                table: "assigned_access_policies",
                newName: "package_id");

            migrationBuilder.RenameColumn(
                name: "access_policy_id",
                schema: "reception",
                table: "assigned_access_policies",
                newName: "access_grant_id");

            migrationBuilder.RenameIndex(
                name: "ix_assigned_access_policies_access_policy_id",
                schema: "reception",
                table: "assigned_access_policies",
                newName: "ix_assigned_access_policies_access_grant_id");

            migrationBuilder.RenameColumn(
                name: "system_id",
                schema: "reception",
                table: "access_rule_assignments",
                newName: "package_id");

            migrationBuilder.RenameIndex(
                name: "ix_access_rule_assignments_system_id",
                schema: "reception",
                table: "access_rule_assignments",
                newName: "ix_access_rule_assignments_package_id");

            migrationBuilder.AddColumn<Guid>(
                name: "identity_id",
                schema: "reception",
                table: "expected_arrivals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_expected_arrivals_identity_id",
                schema: "reception",
                table: "expected_arrivals",
                column: "identity_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_expected_arrivals_identity_id",
                schema: "reception",
                table: "expected_arrivals");

            migrationBuilder.DropColumn(
                name: "identity_id",
                schema: "reception",
                table: "expected_arrivals");

            migrationBuilder.RenameColumn(
                name: "package_id",
                schema: "reception",
                table: "assigned_access_policies",
                newName: "system_id");

            migrationBuilder.RenameColumn(
                name: "access_grant_id",
                schema: "reception",
                table: "assigned_access_policies",
                newName: "access_policy_id");

            migrationBuilder.RenameIndex(
                name: "ix_assigned_access_policies_access_grant_id",
                schema: "reception",
                table: "assigned_access_policies",
                newName: "ix_assigned_access_policies_access_policy_id");

            migrationBuilder.RenameColumn(
                name: "package_id",
                schema: "reception",
                table: "access_rule_assignments",
                newName: "system_id");

            migrationBuilder.RenameIndex(
                name: "ix_access_rule_assignments_package_id",
                schema: "reception",
                table: "access_rule_assignments",
                newName: "ix_access_rule_assignments_system_id");

            migrationBuilder.AddColumn<Guid>(
                name: "access_level_type_id",
                schema: "reception",
                table: "assigned_access_policies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "access_level_type_id",
                schema: "reception",
                table: "access_rule_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "location_id",
                schema: "reception",
                table: "access_rule_assignments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_access_rule_assignments_access_level_type_id",
                schema: "reception",
                table: "access_rule_assignments",
                column: "access_level_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_rule_assignments_location_id",
                schema: "reception",
                table: "access_rule_assignments",
                column: "location_id");
        }
    }
}
