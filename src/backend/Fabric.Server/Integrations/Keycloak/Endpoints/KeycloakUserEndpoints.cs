using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Integrations.Keycloak.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fabric.Server.Integrations.Keycloak.Endpoints;

public static class KeycloakUserEndpoints
{
    public static IEndpointRouteBuilder MapKeycloakUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/integrations/keycloak/users")
            .RequireAuthorization(new AuthorizeAttribute { Roles = $"{FabricRoleDefaults.AdminRole},{FabricRoleDefaults.IntegratorRole}" });

        group.MapGet("", ListUsers)
            .Produces<KeycloakUserResponse[]>();

        group.MapGet("/{id}", GetUser)
            .Produces<KeycloakUserResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("", CreateUser)
            .Produces<KeycloakUserResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", UpdateUser)
            .Produces<KeycloakUserResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPost("/{id}/reset-password", ResetUserPassword)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}", DeleteUser)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/{id}/groups", ListUserGroups)
            .Produces<KeycloakGroupMembershipResponse[]>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{id}/groups/{groupId}", JoinUserGroup)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}/groups/{groupId}", LeaveUserGroup)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapGet("/{id}/roles", ListUserRoles)
            .Produces<KeycloakRoleResponse[]>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/roles", AddUserRoles)
            .Produces<KeycloakRoleResponse[]>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{id}/roles", RemoveUserRoles)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListUsers([AsParameters] ListKeycloakUsersRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ListUsersAsync(request, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> GetUser(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.GetUserAsync(id, cancellationToken)).Match<IResult>(Results.Ok, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> CreateUser([FromBody] CreateKeycloakUserRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default)
    {
        Result<KeycloakUserResponse, KeycloakAdminError> result = await admin.CreateUserAsync(request, cancellationToken);
        return result.Match<IResult>(response => Results.Created($"/api/integrations/keycloak/users/{response.Id}", response), error => ResultsExtensions.Problem(error));
    }

    private static async Task<IResult> UpdateUser(string id, [FromBody] UpdateKeycloakUserRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.UpdateUserAsync(id, request, cancellationToken)).Match<IResult>(Results.Ok, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> ResetUserPassword(string id, [FromBody] ResetKeycloakUserPasswordRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ResetUserPasswordAsync(id, request, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> DeleteUser(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.DeleteUserAsync(id, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> ListUserGroups(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ListUserGroupsAsync(id, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> JoinUserGroup(string id, string groupId, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.JoinUserGroupAsync(id, groupId, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> LeaveUserGroup(string id, string groupId, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.LeaveUserGroupAsync(id, groupId, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));

    private static async Task<IResult> ListUserRoles(string id, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.ListUserRolesAsync(id, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> AddUserRoles(string id, [FromBody] UpdateKeycloakRealmRoleAssignmentsRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.AddUserRolesAsync(id, request, cancellationToken)).Match<IResult>(items => Results.Ok(items.ToArray()), error => ResultsExtensions.Problem(error));

    private static async Task<IResult> RemoveUserRoles(string id, [FromBody] UpdateKeycloakRealmRoleAssignmentsRequest request, IKeycloakTenantAdmin admin, CancellationToken cancellationToken = default) =>
        (await admin.RemoveUserRolesAsync(id, request, cancellationToken)).Match<IResult>(Results.NoContent, error => ResultsExtensions.Problem(error));
}
