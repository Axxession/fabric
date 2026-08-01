using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameVisitorPreOnboardingOrganizerNotificationsToHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "send_confirm_notification_to_organizer",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                newName: "send_confirm_notification_to_host");

            migrationBuilder.RenameColumn(
                name: "send_arrival_notification_to_organizer",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                newName: "send_arrival_notification_to_host");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "send_confirm_notification_to_host",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                newName: "send_confirm_notification_to_organizer");

            migrationBuilder.RenameColumn(
                name: "send_arrival_notification_to_host",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                newName: "send_arrival_notification_to_organizer");
        }
    }
}
