using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.Identities.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentitiesInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identities");

            migrationBuilder.CreateTable(
                name: "identities",
                schema: "identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    preferred_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    display_name = table.Column<string>(type: "character varying(401)", maxLength: 401, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contractor_affiliations",
                schema: "identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contractor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contractor_affiliations", x => x.id);
                    table.ForeignKey(
                        name: "FK_contractor_affiliations_identities_identity_id",
                        column: x => x.identity_id,
                        principalSchema: "identities",
                        principalTable: "identities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employee_affiliations",
                schema: "identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employee_affiliations", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_affiliations_identities_identity_id",
                        column: x => x.identity_id,
                        principalSchema: "identities",
                        principalTable: "identities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visitor_affiliations",
                schema: "identities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    visitor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visitor_affiliations", x => x.id);
                    table.ForeignKey(
                        name: "FK_visitor_affiliations_identities_identity_id",
                        column: x => x.identity_id,
                        principalSchema: "identities",
                        principalTable: "identities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contractor_affiliations_identity_id",
                schema: "identities",
                table: "contractor_affiliations",
                column: "identity_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_affiliations_tenant_id",
                schema: "identities",
                table: "contractor_affiliations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_contractor_affiliations_tenant_id_contractor_id",
                schema: "identities",
                table: "contractor_affiliations",
                columns: new[] { "tenant_id", "contractor_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contractor_affiliations_tenant_id_identity_id",
                schema: "identities",
                table: "contractor_affiliations",
                columns: new[] { "tenant_id", "identity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_affiliations_identity_id",
                schema: "identities",
                table: "employee_affiliations",
                column: "identity_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_affiliations_tenant_id",
                schema: "identities",
                table: "employee_affiliations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_employee_affiliations_tenant_id_employee_id",
                schema: "identities",
                table: "employee_affiliations",
                columns: new[] { "tenant_id", "employee_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employee_affiliations_tenant_id_identity_id",
                schema: "identities",
                table: "employee_affiliations",
                columns: new[] { "tenant_id", "identity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_identities_tenant_id",
                schema: "identities",
                table: "identities",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_identities_tenant_id_display_name",
                schema: "identities",
                table: "identities",
                columns: new[] { "tenant_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_identities_tenant_id_email",
                schema: "identities",
                table: "identities",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_identities_tenant_id_status",
                schema: "identities",
                table: "identities",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_visitor_affiliations_identity_id",
                schema: "identities",
                table: "visitor_affiliations",
                column: "identity_id");

            migrationBuilder.CreateIndex(
                name: "ix_visitor_affiliations_tenant_id",
                schema: "identities",
                table: "visitor_affiliations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_visitor_affiliations_tenant_id_identity_id",
                schema: "identities",
                table: "visitor_affiliations",
                columns: new[] { "tenant_id", "identity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_visitor_affiliations_tenant_id_visitor_id",
                schema: "identities",
                table: "visitor_affiliations",
                columns: new[] { "tenant_id", "visitor_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contractor_affiliations",
                schema: "identities");

            migrationBuilder.DropTable(
                name: "employee_affiliations",
                schema: "identities");

            migrationBuilder.DropTable(
                name: "visitor_affiliations",
                schema: "identities");

            migrationBuilder.DropTable(
                name: "identities",
                schema: "identities");
        }
    }
}
