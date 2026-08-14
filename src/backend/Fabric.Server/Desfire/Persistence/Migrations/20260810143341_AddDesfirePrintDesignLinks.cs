using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Desfire.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDesfirePrintDesignLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PrintDesignId",
                schema: "desfire",
                table: "encoding_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrintDesignId",
                schema: "desfire",
                table: "encoding_batches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_encoding_runs_PrintDesignId",
                schema: "desfire",
                table: "encoding_runs",
                column: "PrintDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_encoding_batches_PrintDesignId",
                schema: "desfire",
                table: "encoding_batches",
                column: "PrintDesignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_encoding_runs_PrintDesignId",
                schema: "desfire",
                table: "encoding_runs");

            migrationBuilder.DropIndex(
                name: "IX_encoding_batches_PrintDesignId",
                schema: "desfire",
                table: "encoding_batches");

            migrationBuilder.DropColumn(
                name: "PrintDesignId",
                schema: "desfire",
                table: "encoding_runs");

            migrationBuilder.DropColumn(
                name: "PrintDesignId",
                schema: "desfire",
                table: "encoding_batches");
        }
    }
}
