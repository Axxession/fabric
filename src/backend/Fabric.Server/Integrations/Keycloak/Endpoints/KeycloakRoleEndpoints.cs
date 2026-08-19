using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Integrations.Keycloak.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Integrations.Keycloak.Endpoints;

public static class KeycloakRoleEndpoints
{
    public static IEndpointRouteBuilder MapKeycloakRoleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/integrations/keycloak/roles")
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{FabricRoleDefaults.AdminRole},{FabricRoleDefaults.IntegratorRole}" });

        group.MapGet("", ListRoles)
            .Produces<KeycloakRoleResponse[]>();

        group.MapGet("/{id}", GetRole)
            .Produces<KeycloakRoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("", CreateRole)
            .Produces<KeycloakRoleResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", UpdateRole)
            .Produces<KeycloakRoleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}", DeleteRole)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListRoles([AsParameters] ListKeycloakRolesRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ListRolesAsync(request, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> GetRole(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.GetRoleAsync(id, cancellationToken)).Match<IResult>(Results.Ok, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> CreateRole([FromBody] CreateKeycloakRoleRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.CreateRoleAsync(request, cancellationToken)).Match<IResult>(response => Results.Created($"/api/integrations/keycloak/roles/{response.Id}", response), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> UpdateRole(string id, [FromBody] UpdateKeycloakRoleRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.UpdateRoleAsync(id, request, cancellationToken)).Match<IResult>(Results.Ok, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> DeleteRole(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.DeleteRoleAsync(id, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));
}
