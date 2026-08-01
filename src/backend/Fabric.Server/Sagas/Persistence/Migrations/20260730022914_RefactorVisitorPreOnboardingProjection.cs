using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorVisitorPreOnboardingProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_visitor_pre_onboarding_sagas_state",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas");

            migrationBuilder.RenameColumn(
                name: "state",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                newName: "visitor_response_status");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancellation_requested_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expired_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE sagas.visitor_pre_onboarding_sagas
                SET
                    cancellation_requested_at = CASE WHEN visitor_response_status = 'Cancelling' THEN created_at ELSE cancellation_requested_at END,
                    cancelled_at = CASE WHEN visitor_response_status = 'Cancelled' THEN created_at ELSE cancelled_at END,
                    expired_at = CASE WHEN visitor_response_status = 'Expired' THEN expires_at ELSE expired_at END,
                    visitor_response_status = CASE
                        WHEN visitor_response_status = 'Confirmed' THEN 'Confirmed'
                        WHEN visitor_response_status = 'Rejected' THEN 'Rejected'
                        ELSE 'Pending'
                    END;
                """);

            migrationBuilder.CreateTable(
                name: "visitor_pre_onboarding_saga_audit_entries",
                schema: "sagas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    saga_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    details_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visitor_pre_onboarding_saga_audit_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_visitor_pre_onboarding_sagas_cancelled_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                column: "cancelled_at");

            migrationBuilder.CreateIndex(
                name: "ix_visitor_pre_onboarding_sagas_expired_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                column: "expired_at");

            migrationBuilder.CreateIndex(
                name: "ix_visitor_pre_onboarding_saga_audit_entries_tenant_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_audit_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_vpo_saga_audit_entries_occurred_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_audit_entries",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_vpo_saga_audit_entries_saga_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_audit_entries",
                column: "saga_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "visitor_pre_onboarding_saga_audit_entries",
                schema: "sagas");

            migrationBuilder.DropIndex(
                name: "ix_visitor_pre_onboarding_sagas_cancelled_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas");

            migrationBuilder.DropIndex(
                name: "ix_visitor_pre_onboarding_sagas_expired_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas");

            migrationBuilder.Sql(
                """
                UPDATE sagas.visitor_pre_onboarding_sagas
                SET visitor_response_status = CASE
                    WHEN cancelled_at IS NOT NULL THEN 'Cancelled'
                    WHEN cancellation_requested_at IS NOT NULL THEN 'Cancelling'
                    WHEN expired_at IS NOT NULL THEN 'Expired'
                    WHEN visitor_response_status = 'Confirmed' THEN 'Confirmed'
                    WHEN visitor_response_status = 'Rejected' THEN 'Rejected'
                    WHEN invitation_sent_at IS NOT NULL THEN 'AwaitingConfirmation'
                    WHEN arrival_id IS NOT NULL THEN 'SendingInvitation'
                    WHEN credential_id IS NOT NULL OR qr_code IS NOT NULL THEN 'RegisteringArrival'
                    ELSE 'GeneratingQr'
                END;
                """);

            migrationBuilder.DropColumn(
                name: "cancellation_requested_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas");

            migrationBuilder.DropColumn(
                name: "expired_at",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas");

            migrationBuilder.RenameColumn(
                name: "visitor_response_status",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                newName: "state");

            migrationBuilder.CreateIndex(
                name: "ix_visitor_pre_onboarding_sagas_state",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                column: "state");
        }
    }
}
