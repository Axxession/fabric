using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fabric.Server.AccessCatalog.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameGrantSourceKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                update access_catalog.access_grants as grant
                set source_kind = 'VisitInvitation',
                    source_id = coalesce(arrival.invitation_id, grant.source_id)
                from reception.expected_arrivals as arrival
                where grant.source_kind = 'ReceptionArrival'
                  and arrival.id = grant.source_id;

                update access_catalog.access_grants as grant
                set source_kind = 'ContractorAssignment',
                    source_id = coalesce(arrival.job_assignment_id, grant.source_id)
                from reception.expected_arrivals as arrival
                where grant.source_kind = 'ContractorJob'
                  and arrival.id = grant.source_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                update access_catalog.access_grants as grant
                set source_kind = 'ReceptionArrival',
                    source_id = arrival.id
                from reception.expected_arrivals as arrival
                where grant.source_kind = 'VisitInvitation'
                  and arrival.invitation_id = grant.source_id;

                update access_catalog.access_grants as grant
                set source_kind = 'ContractorJob',
                    source_id = arrival.id
                from reception.expected_arrivals as arrival
                where grant.source_kind = 'ContractorAssignment'
                  and arrival.job_assignment_id = grant.source_id;
                """);
        }
    }
}
