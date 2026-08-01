using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorCredentialGraceWindows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "grace_end_minutes",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "grace_start_minutes",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grace_end_minutes",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs");

            migrationBuilder.DropColumn(
                name: "grace_start_minutes",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs");
        }
    }
}
