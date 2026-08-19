using Fabric.Server.Core;

namespace Fabric.Server.Integrations.Keycloak.Contracts;

public sealed record ListKeycloakUsersRequest : BaseListRequest
{
    public string? Search { get; init; }
}

public sealed record KeycloakUserResponse(
    string Id,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive);

public sealed record CreateKeycloakUserRequest(
    string Username,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive);

public sealed record UpdateKeycloakUserRequest(
    string Username,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive);

public sealed record ResetKeycloakUserPasswordRequest(
    string Password,
    bool Temporary);

public sealed record ListKeycloakRolesRequest : BaseListRequest
{
    public string? Search { get; init; }
}

public sealed record KeycloakRoleResponse(
    string Id,
    string Name,
    string Description);

public sealed record CreateKeycloakRoleRequest(
    string Name,
    string? Description);

public sealed record UpdateKeycloakRoleRequest(
    string Name,
    string? Description);

public sealed record ListKeycloakGroupsRequest : BaseListRequest
{
    public string? Search { get; init; }
}

public sealed record KeycloakGroupResponse(
    string Id,
    string Name,
    string Path);

public sealed record KeycloakGroupMembershipResponse(
    string Id,
    string Name,
    string Path);

public sealed record KeycloakUserMembershipResponse(
    string Id,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive);

public sealed record CreateKeycloakGroupRequest(string Name);

public sealed record UpdateKeycloakGroupRequest(string Name);

public sealed record UpdateKeycloakRealmRoleAssignmentsRequest(string[] RoleIds);
