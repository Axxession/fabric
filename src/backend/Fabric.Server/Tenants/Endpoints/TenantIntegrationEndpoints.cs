using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Tenants.Application;
using Fabric.Server.Tenants.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Tenants.Endpoints;

public static class TenantIntegrationEndpoints
{
    public static IEndpointRouteBuilder MapTenantIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/tenant-integrations")
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.IntegratorRole });

        group.MapGet("/keycloak", GetKeycloak)
            .Produces<KeycloakIntegrationResponse>();

        group.MapPut("/keycloak", UpdateKeycloak)
            .Produces<KeycloakIntegrationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/microsoft-graph", GetMicrosoftGraph)
            .Produces<MicrosoftGraphIntegrationResponse>();

        group.MapPut("/microsoft-graph", UpdateMicrosoftGraph)
            .Produces<MicrosoftGraphIntegrationResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> GetKeycloak(TenantIntegrationService service, CancellationToken cancellationToken = default) =>
        Results.Ok(await service.GetKeycloakAsync(cancellationToken));

    private static async Task<IResult> UpdateKeycloak([FromBody] UpdateKeycloakIntegrationRequest request, TenantIntegrationService service, CancellationToken cancellationToken = default)
    {
        Result<KeycloakIntegrationResponse, string> result = await service.UpdateKeycloakAsync(request, cancellationToken);
        return result.Match<IResult>(
            response => Results.Ok(response),
            error => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Keycloak integration settings", detail: error));
    }

    private static async Task<IResult> GetMicrosoftGraph(TenantIntegrationService service, CancellationToken cancellationToken = default) =>
        Results.Ok(await service.GetMicrosoftGraphAsync(cancellationToken));

    private static async Task<IResult> UpdateMicrosoftGraph([FromBody] UpdateMicrosoftGraphIntegrationRequest request, TenantIntegrationService service, CancellationToken cancellationToken = default)
    {
        Result<MicrosoftGraphIntegrationResponse, string> result = await service.UpdateMicrosoftGraphAsync(request, cancellationToken);
        return result.Match<IResult>(
            response => Results.Ok(response),
            error => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid Microsoft Graph integration settings", detail: error));
    }
}
