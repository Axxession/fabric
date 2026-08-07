using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessControl.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPacsSubjectConformityAndBlocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "conformity_details",
                schema: "access_control",
                table: "pacs_subjects",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "conformity_status",
                schema: "access_control",
                table: "pacs_subjects",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<bool>(
                name: "is_manual_provisioning_blocked",
                schema: "access_control",
                table: "pacs_subjects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_conformity_checked_at",
                schema: "access_control",
                table: "pacs_subjects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_conformity_error",
                schema: "access_control",
                table: "pacs_subjects",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "manual_provisioning_blocked_at",
                schema: "access_control",
                table: "pacs_subjects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "manual_provisioning_blocked_reason",
                schema: "access_control",
                table: "pacs_subjects",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "anomaly_block_mode",
                schema: "access_control",
                table: "access_control_systems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "WarnOnly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "conformity_details",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "conformity_status",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "is_manual_provisioning_blocked",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "last_conformity_checked_at",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "last_conformity_error",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "manual_provisioning_blocked_at",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "manual_provisioning_blocked_reason",
                schema: "access_control",
                table: "pacs_subjects");

            migrationBuilder.DropColumn(
                name: "anomaly_block_mode",
                schema: "access_control",
                table: "access_control_systems");
        }
    }
}
