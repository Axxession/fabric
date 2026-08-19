using Fabric.Server.Core;
using Fabric.Server.Integrations.Keycloak.Contracts;

namespace Fabric.Server.Integrations.Keycloak;

public interface IKeycloakTenantAdmin
{
    Task<Result<IReadOnlyList<KeycloakUserResponse>, KeycloakAdminError>> ListUsersAsync(ListKeycloakUsersRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakUserResponse, KeycloakAdminError>> GetUserAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<KeycloakUserResponse, KeycloakAdminError>> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakUserResponse, KeycloakAdminError>> UpdateUserAsync(string id, UpdateKeycloakUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> ResetUserPasswordAsync(string id, ResetKeycloakUserPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> DeleteUserAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakGroupMembershipResponse>, KeycloakAdminError>> ListUserGroupsAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> JoinUserGroupAsync(string id, string groupId, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> LeaveUserGroupAsync(string id, string groupId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> ListUserRolesAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> AddUserRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> RemoveUserRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> ListRolesAsync(ListKeycloakRolesRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakRoleResponse, KeycloakAdminError>> GetRoleAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<KeycloakRoleResponse, KeycloakAdminError>> CreateRoleAsync(CreateKeycloakRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakRoleResponse, KeycloakAdminError>> UpdateRoleAsync(string id, UpdateKeycloakRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> DeleteRoleAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakGroupResponse>, KeycloakAdminError>> ListGroupsAsync(ListKeycloakGroupsRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakGroupResponse, KeycloakAdminError>> GetGroupAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<KeycloakGroupResponse, KeycloakAdminError>> CreateGroupAsync(CreateKeycloakGroupRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakGroupResponse, KeycloakAdminError>> UpdateGroupAsync(string id, UpdateKeycloakGroupRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> DeleteGroupAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakUserMembershipResponse>, KeycloakAdminError>> ListGroupMembersAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> ListGroupRolesAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> AddGroupRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default);
    Task<Result<KeycloakAdminError>> RemoveGroupRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default);
}
