using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Desfire.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameEncodingToBadgeJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "encoding_batches",
                schema: "desfire");

            migrationBuilder.DropTable(
                name: "encoding_runs",
                schema: "desfire");

            migrationBuilder.RenameColumn(
                name: "EncodingRunId",
                schema: "desfire",
                table: "device_leases",
                newName: "BadgeJobId");

            migrationBuilder.CreateTable(
                name: "badge_batches",
                schema: "desfire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EncoderId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransformationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrintDesignId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OriginalInputJson = table.Column<string>(type: "jsonb", nullable: false),
                    NormalizedRowsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badge_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "badge_jobs",
                schema: "desfire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransformationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    EncoderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrintDesignId = table.Column<Guid>(type: "uuid", nullable: true),
                    KioskSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    ResolvedVariablesJson = table.Column<string>(type: "jsonb", nullable: false),
                    PlanSummaryJson = table.Column<string>(type: "jsonb", nullable: false),
                    CommandAuditJson = table.Column<string>(type: "jsonb", nullable: false),
                    CardUid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    HardwareAgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestedAgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestedDeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VariableConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    ClaimedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_badge_jobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_badge_batches_EncoderId",
                schema: "desfire",
                table: "badge_batches",
                column: "EncoderId");

            migrationBuilder.CreateIndex(
                name: "IX_badge_batches_PrintDesignId",
                schema: "desfire",
                table: "badge_batches",
                column: "PrintDesignId");

            migrationBuilder.CreateIndex(
                name: "ix_badge_batches_tenant_id",
                schema: "desfire",
                table: "badge_batches",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_batches_TransformationId",
                schema: "desfire",
                table: "badge_batches",
                column: "TransformationId");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_BatchId",
                schema: "desfire",
                table: "badge_jobs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_CardUid",
                schema: "desfire",
                table: "badge_jobs",
                column: "CardUid");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_EncoderId",
                schema: "desfire",
                table: "badge_jobs",
                column: "EncoderId");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_KioskSessionId",
                schema: "desfire",
                table: "badge_jobs",
                column: "KioskSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_PrintDesignId",
                schema: "desfire",
                table: "badge_jobs",
                column: "PrintDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_RequestedAgentId_RequestedDeviceId",
                schema: "desfire",
                table: "badge_jobs",
                columns: new[] { "RequestedAgentId", "RequestedDeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_Source",
                schema: "desfire",
                table: "badge_jobs",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_Status_Priority_RequestedAt",
                schema: "desfire",
                table: "badge_jobs",
                columns: new[] { "Status", "Priority", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_badge_jobs_tenant_id",
                schema: "desfire",
                table: "badge_jobs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_badge_jobs_TransformationId",
                schema: "desfire",
                table: "badge_jobs",
                column: "TransformationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "badge_batches",
                schema: "desfire");

            migrationBuilder.DropTable(
                name: "badge_jobs",
                schema: "desfire");

            migrationBuilder.RenameColumn(
                name: "BadgeJobId",
                schema: "desfire",
                table: "device_leases",
                newName: "EncodingRunId");

            migrationBuilder.CreateTable(
                name: "encoding_batches",
                schema: "desfire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EncoderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedRowsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OriginalInputJson = table.Column<string>(type: "jsonb", nullable: false),
                    PrintDesignId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TransformationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encoding_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "encoding_runs",
                schema: "desfire",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CardUid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ClaimExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClaimedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CommandAuditJson = table.Column<string>(type: "jsonb", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EncoderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    HardwareAgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InputJson = table.Column<string>(type: "jsonb", nullable: false),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    KioskSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlanSummaryJson = table.Column<string>(type: "jsonb", nullable: false),
                    PrintDesignId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    RequestedAgentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestedDeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ResolvedVariablesJson = table.Column<string>(type: "jsonb", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TransformationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariableConfigJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encoding_runs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_encoding_batches_EncoderId",
                schema: "desfire",
                table: "encoding_batches",
                column: "EncoderId");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_batches_PrintDesignId",
                schema: "desfire",
                table: "encoding_batches",
                column: "PrintDesignId");

            migrationBuilder.CreateIndex(
                name: "ix_encoding_batches_tenant_id",
                schema: "desfire",
                table: "encoding_batches",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_batches_TransformationId",
                schema: "desfire",
                table: "encoding_batches",
                column: "TransformationId");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_BatchId",
                schema: "desfire",
                table: "encoding_runs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_CardUid",
                schema: "desfire",
                table: "encoding_runs",
                column: "CardUid");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_EncoderId",
                schema: "desfire",
                table: "encoding_runs",
                column: "EncoderId");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_KioskSessionId",
                schema: "desfire",
                table: "encoding_runs",
                column: "KioskSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_PrintDesignId",
                schema: "desfire",
                table: "encoding_runs",
                column: "PrintDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_RequestedAgentId_RequestedDeviceId",
                schema: "desfire",
                table: "encoding_runs",
                columns: new[] { "RequestedAgentId", "RequestedDeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_Source",
                schema: "desfire",
                table: "encoding_runs",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_Status_Priority_RequestedAt",
                schema: "desfire",
                table: "encoding_runs",
                columns: new[] { "Status", "Priority", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_encoding_runs_tenant_id",
                schema: "desfire",
                table: "encoding_runs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_TransformationId",
                schema: "desfire",
                table: "encoding_runs",
                column: "TransformationId");
        }
    }
}
