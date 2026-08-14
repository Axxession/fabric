using Fabric.Server.Contractors.Endpoints;

namespace Fabric.Server.Contractors;

public static class ContractorEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapContractorModuleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCompanyEndpoints();
        app.MapContractorEndpoints();
        app.MapJobTypeEndpoints();
        app.MapContractorJobEndpoints();
        app.MapContractorJobAssignmentEndpoints();
        return app;
    }
}
