using Fabric.Server.Requirements.Endpoints;

namespace Fabric.Server.Requirements;

public static class RequirementsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapRequirementsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRequirementDefinitionEndpoints();
        app.MapRequirementPolicyEndpoints();
        app.MapRequirementEvidenceEndpoints();
        return app;
    }
}
