using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Sagas.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseQrCredentialTypeForVisitorPreOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_vpo_config_access_control_qr_ids",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs");

            migrationBuilder.DropColumn(
                name: "badge_type_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs");

            migrationBuilder.DropColumn(
                name: "qr_generation_mode",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs");

            migrationBuilder.RenameColumn(
                name: "system_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                newName: "qr_credential_type_id");

            migrationBuilder.AddColumn<Guid>(
                name: "credential_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "credential_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_sagas");

            migrationBuilder.RenameColumn(
                name: "qr_credential_type_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                newName: "system_id");

            migrationBuilder.AddColumn<Guid>(
                name: "badge_type_id",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "qr_generation_mode",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vpo_config_access_control_qr_ids",
                schema: "sagas",
                table: "visitor_pre_onboarding_saga_configs",
                sql: "(qr_generation_mode <> 'AccessControlQr') OR (system_id IS NOT NULL AND badge_type_id IS NOT NULL)");
        }
    }
}
