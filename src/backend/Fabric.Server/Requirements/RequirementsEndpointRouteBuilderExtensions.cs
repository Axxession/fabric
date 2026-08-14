using Fabric.Server.Requirements.Endpoints;

namespace Fabric.Server.Requirements;

public static class RequirementsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRequirementsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapEnforcementZoneEndpoints();
        app.MapRequirementDefinitionEndpoints();
        app.MapRequirementPolicyEndpoints();
        app.MapRequirementEvidenceEndpoints();
        app.MapZoneComplianceEndpoints();
        return app;
    }
}
