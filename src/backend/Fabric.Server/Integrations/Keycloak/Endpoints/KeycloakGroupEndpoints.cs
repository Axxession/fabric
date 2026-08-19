using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Integrations.Keycloak.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Integrations.Keycloak.Endpoints;

public static class KeycloakGroupEndpoints
{
    public static IEndpointRouteBuilder MapKeycloakGroupEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/integrations/keycloak/groups")
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{FabricRoleDefaults.AdminRole},{FabricRoleDefaults.IntegratorRole}" });

        group.MapGet("", ListGroups)
            .Produces<KeycloakGroupResponse[]>();

        group.MapGet("/{id}", GetGroup)
            .Produces<KeycloakGroupResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("", CreateGroup)
            .Produces<KeycloakGroupResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", UpdateGroup)
            .Produces<KeycloakGroupResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}", DeleteGroup)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/members", ListGroupMembers)
            .Produces<KeycloakUserMembershipResponse[]>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/roles", ListGroupRoles)
            .Produces<KeycloakRoleResponse[]>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/roles", AddGroupRoles)
            .Produces<KeycloakRoleResponse[]>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}/roles", RemoveGroupRoles)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListGroups([AsParameters] ListKeycloakGroupsRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ListGroupsAsync(request, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> GetGroup(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.GetGroupAsync(id, cancellationToken)).Match<IResult>(Results.Ok, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> CreateGroup([FromBody] CreateKeycloakGroupRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.CreateGroupAsync(request, cancellationToken)).Match<IResult>(response => Results.Created($"/api/integrations/keycloak/groups/{response.Id}", response), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> UpdateGroup(string id, [FromBody] UpdateKeycloakGroupRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.UpdateGroupAsync(id, request, cancellationToken)).Match<IResult>(Results.Ok, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> DeleteGroup(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.DeleteGroupAsync(id, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> ListGroupMembers(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ListGroupMembersAsync(id, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> ListGroupRoles(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ListGroupRolesAsync(id, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> AddGroupRoles(string id, [FromBody] UpdateKeycloakRealmRoleAssignmentsRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.AddGroupRolesAsync(id, request, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> RemoveGroupRoles(string id, [FromBody] UpdateKeycloakRealmRoleAssignmentsRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.RemoveGroupRolesAsync(id, request, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));
}
