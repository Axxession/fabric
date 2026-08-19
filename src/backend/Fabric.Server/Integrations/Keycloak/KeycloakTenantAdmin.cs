using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Fabric.Server.Core;
using Fabric.Server.Integrations.Keycloak.Contracts;
using Fabric.Server.Tenants.Application;
using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Integrations.Keycloak;

public sealed class KeycloakTenantAdmin(
    TenantIntegrationService tenantIntegrationService,
    IHttpClientFactory httpClientFactory,
    KeycloakAdminTokenProvider tokenProvider,
    ILogger<KeycloakTenantAdmin> logger) : IKeycloakTenantAdmin
{
    public Task<Result<IReadOnlyList<KeycloakUserResponse>, KeycloakAdminError>> ListUsersAsync(ListKeycloakUsersRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            string path = BuildPath(config.Realm, "users", request.Search, request.Page, request.PageSize);
            KeycloakUserRepresentation[]? users = await SendAsync<KeycloakUserRepresentation[]>(client, HttpMethod.Get, path, null, ct);
            return Result.Success<IReadOnlyList<KeycloakUserResponse>, KeycloakAdminError>((users ?? []).Select(MapUser).ToArray());
        }, cancellationToken);

    public Task<Result<KeycloakUserResponse, KeycloakAdminError>> GetUserAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            KeycloakUserRepresentation user = await SendRequiredAsync<KeycloakUserRepresentation>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(id)}", null, ct);
            return Result.Success<KeycloakUserResponse, KeycloakAdminError>(MapUser(user));
        }, cancellationToken);

    public Task<Result<KeycloakUserResponse, KeycloakAdminError>> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return Result.Failure<KeycloakUserResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, "Username is required."));

            if (string.IsNullOrWhiteSpace(request.Email))
                return Result.Failure<KeycloakUserResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, "Email is required."));

            using HttpResponseMessage response = await SendResponseAsync(
                client,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users",
                new KeycloakUserWriteRequest(
                    request.Username.Trim(),
                    request.Email.Trim(),
                    request.FirstName.Trim(),
                    request.LastName.Trim(),
                    request.IsActive),
                ct);

            if (response.StatusCode is not HttpStatusCode.Created)
                return await FailureFromResponse<KeycloakUserResponse>(response, "Keycloak user creation failed.", ct);

            string? id = ReadResourceId(response.Headers.Location);
            if (id is null)
                return Result.Failure<KeycloakUserResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak user creation did not return a resource location."));

            return await GetUserAsync(id, ct);
        }, cancellationToken);

    public Task<Result<KeycloakUserResponse, KeycloakAdminError>> UpdateUserAsync(string id, UpdateKeycloakUserRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return Result.Failure<KeycloakUserResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, "Username is required."));

            using HttpResponseMessage response = await SendResponseAsync(
                client,
                HttpMethod.Put,
                $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(id)}",
                new KeycloakUserWriteRequest(
                    request.Username.Trim(),
                    request.Email.Trim(),
                    request.FirstName.Trim(),
                    request.LastName.Trim(),
                    request.IsActive),
                ct);

            if (response.StatusCode is not HttpStatusCode.NoContent)
                return await FailureFromResponse<KeycloakUserResponse>(response, "Keycloak user update failed.", ct);

            return await GetUserAsync(id, ct);
        }, cancellationToken);

    public Task<Result<KeycloakAdminError>> ResetUserPasswordAsync(string id, ResetKeycloakUserPasswordRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Password))
                return Result.Failure<KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, "Password is required."));

            using HttpResponseMessage response = await SendResponseAsync(
                client,
                HttpMethod.Put,
                $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(id)}/reset-password",
                new KeycloakResetPasswordRequest("password", request.Password, request.Temporary),
                ct);

            return response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK
                ? Result.Success<KeycloakAdminError>()
                : await FailureFromResponse(response, "Keycloak password reset failed.", ct);
        }, cancellationToken);

    public Task<Result<KeycloakAdminError>> DeleteUserAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            using HttpResponseMessage response = await SendResponseAsync(client, HttpMethod.Delete, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(id)}", null, ct);
            return response.StatusCode is HttpStatusCode.NoContent
                ? Result.Success<KeycloakAdminError>()
                : await FailureFromResponse(response, "Keycloak user deletion failed.", ct);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakGroupMembershipResponse>, KeycloakAdminError>> ListUserGroupsAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            KeycloakGroupRepresentation[]? groups = await SendAsync<KeycloakGroupRepresentation[]>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(id)}/groups", null, ct);
            return Result.Success<IReadOnlyList<KeycloakGroupMembershipResponse>, KeycloakAdminError>((groups ?? []).Select(MapUserGroup).ToArray());
        }, cancellationToken);

    public Task<Result<KeycloakAdminError>> JoinUserGroupAsync(string id, string groupId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            using HttpResponseMessage response = await SendResponseAsync(client, HttpMethod.Put, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(id)}/groups/{Uri.EscapeDataString(groupId)}", null, ct);
            return response.StatusCode is HttpStatusCode.NoContent
                ? Result.Success<KeycloakAdminError>()
                : await FailureFromResponse(response, "Joining Keycloak group failed.", ct);
        }, cancellationToken);

    public Task<Result<KeycloakAdminError>> LeaveUserGroupAsync(string id, string groupId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            using HttpResponseMessage response = await SendResponseAsync(client, HttpMethod.Delete, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(id)}/groups/{Uri.EscapeDataString(groupId)}", null, ct);
            return response.StatusCode is HttpStatusCode.NoContent
                ? Result.Success<KeycloakAdminError>()
                : await FailureFromResponse(response, "Leaving Keycloak group failed.", ct);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> ListUserRolesAsync(string id, CancellationToken cancellationToken = default) =>
        ListRealmRolesAsync($"users/{Uri.EscapeDataString(id)}/role-mappings/realm", cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> AddUserRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default) =>
        AddRealmRolesAsync($"users/{Uri.EscapeDataString(id)}/role-mappings/realm", $"users/{Uri.EscapeDataString(id)}/role-mappings/realm", request, cancellationToken);

    public Task<Result<KeycloakAdminError>> RemoveUserRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default) =>
        RemoveRealmRolesAsync($"users/{Uri.EscapeDataString(id)}/role-mappings/realm", request, cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> ListRolesAsync(ListKeycloakRolesRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            string path = BuildPath(config.Realm, "roles", request.Search, request.Page, request.PageSize);
            KeycloakRoleRepresentation[]? roles = await SendAsync<KeycloakRoleRepresentation[]>(client, HttpMethod.Get, path, null, ct);
            return Result.Success<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>((roles ?? []).Select(MapRole).ToArray());
        }, cancellationToken);

    public Task<Result<KeycloakRoleResponse, KeycloakAdminError>> GetRoleAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            KeycloakRoleRepresentation role = await SendRequiredAsync<KeycloakRoleRepresentation>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/roles-by-id/{Uri.EscapeDataString(id)}", null, ct);
            return Result.Success<KeycloakRoleResponse, KeycloakAdminError>(MapRole(role));
        }, cancellationToken);

    public Task<Result<KeycloakRoleResponse, KeycloakAdminError>> CreateRoleAsync(CreateKeycloakRoleRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result.Failure<KeycloakRoleResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, "Role name is required."));

            using HttpResponseMessage response = await SendResponseAsync(
                client,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(config.Realm)}/roles",
                new KeycloakRoleWriteRequest(request.Name.Trim(), request.Description?.Trim() ?? string.Empty),
                ct);

            if (response.StatusCode is not HttpStatusCode.Created)
                return await FailureFromResponse<KeycloakRoleResponse>(response, "Keycloak role creation failed.", ct);

            string? id = ReadResourceId(response.Headers.Location);
            if (id is null)
                return Result.Failure<KeycloakRoleResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak role creation did not return a resource location."));

            return await GetRoleAsync(id, ct);
        }, cancellationToken);

    public Task<Result<KeycloakRoleResponse, KeycloakAdminError>> UpdateRoleAsync(string id, UpdateKeycloakRoleRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            Result<KeycloakRoleResponse, KeycloakAdminError> currentResult = await GetRoleAsync(id, ct);
            if (currentResult.IsFailure(out KeycloakAdminError currentError))
                return Result.Failure<KeycloakRoleResponse, KeycloakAdminError>(currentError);

            currentResult.IsSuccess(out KeycloakRoleResponse currentRole);
            using HttpResponseMessage response = await SendResponseAsync(
                client,
                HttpMethod.Put,
                $"admin/realms/{Uri.EscapeDataString(config.Realm)}/roles-by-id/{Uri.EscapeDataString(id)}",
                new KeycloakRoleWriteRequest(request.Name.Trim(), request.Description?.Trim() ?? string.Empty),
                ct);

            if (response.StatusCode is not HttpStatusCode.NoContent)
                return await FailureFromResponse<KeycloakRoleResponse>(response, "Keycloak role update failed.", ct);

            return string.Equals(currentRole.Name, request.Name.Trim(), StringComparison.Ordinal)
                ? await GetRoleAsync(id, ct)
                : await GetRoleByNameAsync(request.Name.Trim(), ct);
        }, cancellationToken);

    public Task<Result<KeycloakAdminError>> DeleteRoleAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            using HttpResponseMessage response = await SendResponseAsync(client, HttpMethod.Delete, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/roles-by-id/{Uri.EscapeDataString(id)}", null, ct);
            return response.StatusCode is HttpStatusCode.NoContent
                ? Result.Success<KeycloakAdminError>()
                : await FailureFromResponse(response, "Keycloak role deletion failed.", ct);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakGroupResponse>, KeycloakAdminError>> ListGroupsAsync(ListKeycloakGroupsRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            string path = BuildPath(config.Realm, "groups", request.Search, request.Page, request.PageSize);
            KeycloakGroupRepresentation[]? groups = await SendAsync<KeycloakGroupRepresentation[]>(client, HttpMethod.Get, path, null, ct);
            return Result.Success<IReadOnlyList<KeycloakGroupResponse>, KeycloakAdminError>((groups ?? []).Select(MapGroup).ToArray());
        }, cancellationToken);

    public Task<Result<KeycloakGroupResponse, KeycloakAdminError>> GetGroupAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            KeycloakGroupRepresentation group = await SendRequiredAsync<KeycloakGroupRepresentation>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/groups/{Uri.EscapeDataString(id)}", null, ct);
            return Result.Success<KeycloakGroupResponse, KeycloakAdminError>(MapGroup(group));
        }, cancellationToken);

    public Task<Result<KeycloakGroupResponse, KeycloakAdminError>> CreateGroupAsync(CreateKeycloakGroupRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result.Failure<KeycloakGroupResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, "Group name is required."));

            using HttpResponseMessage response = await SendResponseAsync(
                client,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(config.Realm)}/groups",
                new KeycloakGroupWriteRequest(request.Name.Trim()),
                ct);

            if (response.StatusCode is not HttpStatusCode.Created)
                return await FailureFromResponse<KeycloakGroupResponse>(response, "Keycloak group creation failed.", ct);

            string? id = ReadResourceId(response.Headers.Location);
            if (id is null)
                return Result.Failure<KeycloakGroupResponse, KeycloakAdminError>(new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak group creation did not return a resource location."));

            return await GetGroupAsync(id, ct);
        }, cancellationToken);

    public Task<Result<KeycloakGroupResponse, KeycloakAdminError>> UpdateGroupAsync(string id, UpdateKeycloakGroupRequest request, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            using HttpResponseMessage response = await SendResponseAsync(
                client,
                HttpMethod.Put,
                $"admin/realms/{Uri.EscapeDataString(config.Realm)}/groups/{Uri.EscapeDataString(id)}",
                new KeycloakGroupWriteRequest(request.Name.Trim()),
                ct);

            if (response.StatusCode is not HttpStatusCode.NoContent)
                return await FailureFromResponse<KeycloakGroupResponse>(response, "Keycloak group update failed.", ct);

            return await GetGroupAsync(id, ct);
        }, cancellationToken);

    public Task<Result<KeycloakAdminError>> DeleteGroupAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            using HttpResponseMessage response = await SendResponseAsync(client, HttpMethod.Delete, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/groups/{Uri.EscapeDataString(id)}", null, ct);
            return response.StatusCode is HttpStatusCode.NoContent
                ? Result.Success<KeycloakAdminError>()
                : await FailureFromResponse(response, "Keycloak group deletion failed.", ct);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakUserMembershipResponse>, KeycloakAdminError>> ListGroupMembersAsync(string id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            KeycloakUserRepresentation[]? members = await SendAsync<KeycloakUserRepresentation[]>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/groups/{Uri.EscapeDataString(id)}/members", null, ct);
            return Result.Success<IReadOnlyList<KeycloakUserMembershipResponse>, KeycloakAdminError>((members ?? []).Select(MapGroupMember).ToArray());
        }, cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> ListGroupRolesAsync(string id, CancellationToken cancellationToken = default) =>
        ListRealmRolesAsync($"groups/{Uri.EscapeDataString(id)}/role-mappings/realm", cancellationToken);

    public Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> AddGroupRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default) =>
        AddRealmRolesAsync($"groups/{Uri.EscapeDataString(id)}/role-mappings/realm", $"groups/{Uri.EscapeDataString(id)}/role-mappings/realm", request, cancellationToken);

    public Task<Result<KeycloakAdminError>> RemoveGroupRolesAsync(string id, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken = default) =>
        RemoveRealmRolesAsync($"groups/{Uri.EscapeDataString(id)}/role-mappings/realm", request, cancellationToken);

    private Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> ListRealmRolesAsync(string relativePath, CancellationToken cancellationToken) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            KeycloakRoleRepresentation[]? roles = await SendAsync<KeycloakRoleRepresentation[]>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/{relativePath}", null, ct);
            return Result.Success<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>((roles ?? []).Select(MapRole).ToArray());
        }, cancellationToken);

    private Task<Result<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>> AddRealmRolesAsync(string updatePath, string listPath, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            Result<KeycloakRoleMappingRepresentation[], KeycloakAdminError> rolesResult = await ResolveRoleRepresentationsAsync(client, config, request.RoleIds, ct);
            if (rolesResult.IsFailure(out KeycloakAdminError roleError))
                return Result.Failure<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>(roleError);

            rolesResult.IsSuccess(out KeycloakRoleMappingRepresentation[] roles);
            using HttpResponseMessage response = await SendResponseAsync(client, HttpMethod.Post, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/{updatePath}", roles, ct);
            if (response.StatusCode is not HttpStatusCode.NoContent)
                return await FailureFromResponse<IReadOnlyList<KeycloakRoleResponse>>(response, "Keycloak realm role assignment failed.", ct);

            KeycloakRoleRepresentation[]? assignedRoles = await SendAsync<KeycloakRoleRepresentation[]>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/{listPath}", null, ct);
            return Result.Success<IReadOnlyList<KeycloakRoleResponse>, KeycloakAdminError>((assignedRoles ?? []).Select(MapRole).ToArray());
        }, cancellationToken);

    private Task<Result<KeycloakAdminError>> RemoveRealmRolesAsync(string relativePath, UpdateKeycloakRealmRoleAssignmentsRequest request, CancellationToken cancellationToken) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            Result<KeycloakRoleMappingRepresentation[], KeycloakAdminError> rolesResult = await ResolveRoleRepresentationsAsync(client, config, request.RoleIds, ct);
            if (rolesResult.IsFailure(out KeycloakAdminError roleError))
                return Result.Failure<KeycloakAdminError>(roleError);

            rolesResult.IsSuccess(out KeycloakRoleMappingRepresentation[] roles);
            using HttpResponseMessage response = await SendResponseAsync(client, HttpMethod.Delete, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/{relativePath}", roles, ct);
            return response.StatusCode is HttpStatusCode.NoContent
                ? Result.Success<KeycloakAdminError>()
                : await FailureFromResponse(response, "Keycloak realm role removal failed.", ct);
        }, cancellationToken);

    private async Task<Result<KeycloakRoleMappingRepresentation[], KeycloakAdminError>> ResolveRoleRepresentationsAsync(HttpClient client, KeycloakAdminApiIntegrationConfig config, IEnumerable<string> roleIds, CancellationToken cancellationToken)
    {
        string[] ids = roleIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length == 0)
            return Result.Success<KeycloakRoleMappingRepresentation[], KeycloakAdminError>([]);

        var roles = new List<KeycloakRoleMappingRepresentation>(ids.Length);
        foreach (string id in ids)
        {
            try
            {
                KeycloakRoleRepresentation role = await SendRequiredAsync<KeycloakRoleRepresentation>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/roles-by-id/{Uri.EscapeDataString(id)}", null, cancellationToken);
                roles.Add(new KeycloakRoleMappingRepresentation(role.Id, role.Name));
            }
            catch (KeycloakAdminException exception)
            {
                return Result.Failure<KeycloakRoleMappingRepresentation[], KeycloakAdminError>(exception.Error);
            }
        }

        return Result.Success<KeycloakRoleMappingRepresentation[], KeycloakAdminError>(roles.ToArray());
    }

    private Task<Result<KeycloakRoleResponse, KeycloakAdminError>> GetRoleByNameAsync(string name, CancellationToken cancellationToken) =>
        ExecuteAsync(async (client, config, ct) =>
        {
            KeycloakRoleRepresentation role = await SendRequiredAsync<KeycloakRoleRepresentation>(client, HttpMethod.Get, $"admin/realms/{Uri.EscapeDataString(config.Realm)}/roles/{Uri.EscapeDataString(name)}", null, ct);
            return Result.Success<KeycloakRoleResponse, KeycloakAdminError>(MapRole(role));
        }, cancellationToken);

    private async Task<Result<T, KeycloakAdminError>> ExecuteAsync<T>(Func<HttpClient, KeycloakAdminApiIntegrationConfig, CancellationToken, Task<Result<T, KeycloakAdminError>>> action, CancellationToken cancellationToken)
    {
        Result<(HttpClient Client, KeycloakAdminApiIntegrationConfig Config), KeycloakAdminError> setupResult = await CreateClientAsync(cancellationToken);
        if (setupResult.IsFailure(out KeycloakAdminError setupError))
            return Result.Failure<T, KeycloakAdminError>(setupError);

        setupResult.IsSuccess(out (HttpClient Client, KeycloakAdminApiIntegrationConfig Config) setup);
        using HttpClient client = setup.Client;

        try
        {
            return await action(client, setup.Config, cancellationToken);
        }
        catch (KeycloakAdminException exception)
        {
            return Result.Failure<T, KeycloakAdminError>(exception.Error);
        }
    }

    private async Task<Result<KeycloakAdminError>> ExecuteAsync(Func<HttpClient, KeycloakAdminApiIntegrationConfig, CancellationToken, Task<Result<KeycloakAdminError>>> action, CancellationToken cancellationToken)
    {
        Result<(HttpClient Client, KeycloakAdminApiIntegrationConfig Config), KeycloakAdminError> setupResult = await CreateClientAsync(cancellationToken);
        if (setupResult.IsFailure(out KeycloakAdminError setupError))
            return Result.Failure<KeycloakAdminError>(setupError);

        setupResult.IsSuccess(out (HttpClient Client, KeycloakAdminApiIntegrationConfig Config) setup);
        using HttpClient client = setup.Client;

        try
        {
            return await action(client, setup.Config, cancellationToken);
        }
        catch (KeycloakAdminException exception)
        {
            return Result.Failure<KeycloakAdminError>(exception.Error);
        }
    }

    private async Task<Result<(HttpClient Client, KeycloakAdminApiIntegrationConfig Config), KeycloakAdminError>> CreateClientAsync(CancellationToken cancellationToken)
    {
        KeycloakAdminApiIntegrationConfig? config = await tenantIntegrationService.GetKeycloakAdminApiConfigAsync(cancellationToken);
        if (config is null || !config.IsEnabled)
        {
            return Result.Failure<(HttpClient Client, KeycloakAdminApiIntegrationConfig Config), KeycloakAdminError>(
                new KeycloakAdminError(KeycloakAdminErrorCode.IntegrationDisabled, "Keycloak admin integration is disabled for this tenant."));
        }

        if (!config.IsConfigured())
        {
            return Result.Failure<(HttpClient Client, KeycloakAdminApiIntegrationConfig Config), KeycloakAdminError>(
                new KeycloakAdminError(KeycloakAdminErrorCode.IntegrationNotConfigured, "Keycloak admin integration is not fully configured for this tenant."));
        }

        Result<AuthenticationHeaderValue, KeycloakAdminError> tokenResult = await tokenProvider.GetAuthorizationHeaderAsync(config, cancellationToken);
        if (tokenResult.IsFailure(out KeycloakAdminError tokenError))
            return Result.Failure<(HttpClient Client, KeycloakAdminApiIntegrationConfig Config), KeycloakAdminError>(tokenError);

        tokenResult.IsSuccess(out AuthenticationHeaderValue authorizationHeader);
        HttpClient client = httpClientFactory.CreateClient(KeycloakIntegrationServiceCollectionExtensions.HttpClientName);
        client.BaseAddress = new Uri($"{config.Url.TrimEnd('/')}/");
        client.DefaultRequestHeaders.Authorization = authorizationHeader;
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return Result.Success<(HttpClient Client, KeycloakAdminApiIntegrationConfig Config), KeycloakAdminError>((client, config));
    }

    private async Task<T> SendRequiredAsync<T>(HttpClient client, HttpMethod method, string relativePath, object? body, CancellationToken cancellationToken)
    {
        T? result = await SendAsync<T>(client, method, relativePath, body, cancellationToken);
        if (result is null)
            throw new KeycloakAdminException(new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak response body was empty."));

        return result;
    }

    private async Task<T?> SendAsync<T>(HttpClient client, HttpMethod method, string relativePath, object? body, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendResponseAsync(client, method, relativePath, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new KeycloakAdminException(await MapErrorAsync(response, "Keycloak request failed.", cancellationToken));

        if (response.StatusCode is HttpStatusCode.NoContent)
            return default;

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        T? value = KeycloakJson.Deserialize<T>(json);
        return value;
    }

    private async Task<HttpResponseMessage> SendResponseAsync(HttpClient client, HttpMethod method, string relativePath, object? body, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(method, relativePath);
        if (body is not null)
        {
            request.Content = body switch
            {
                KeycloakUserWriteRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakRoleWriteRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakGroupWriteRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakResetPasswordRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakRoleMappingRepresentation[] value => KeycloakJson.CreateJsonContent(value),
                _ => throw new InvalidOperationException($"Type {body.GetType().FullName} is not registered for Keycloak request serialization."),
            };
        }

        KeycloakTenantAdminLog.SendingAdminRequest(logger, method.Method, relativePath);
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<Result<T, KeycloakAdminError>> FailureFromResponse<T>(HttpResponseMessage response, string prefix, CancellationToken cancellationToken) =>
        Result.Failure<T, KeycloakAdminError>(await MapErrorAsync(response, prefix, cancellationToken));

    private async Task<Result<KeycloakAdminError>> FailureFromResponse(HttpResponseMessage response, string prefix, CancellationToken cancellationToken) =>
        Result.Failure<KeycloakAdminError>(await MapErrorAsync(response, prefix, cancellationToken));

    private async Task<KeycloakAdminError> MapErrorAsync(HttpResponseMessage response, string prefix, CancellationToken cancellationToken)
    {
        string detail = await ReadErrorDetailAsync(response, prefix, cancellationToken);
        KeycloakTenantAdminLog.AdminRequestFailed(
            logger,
            response.RequestMessage?.Method.Method ?? "UNKNOWN",
            response.RequestMessage?.RequestUri?.ToString() ?? "UNKNOWN",
            (int)response.StatusCode,
            detail);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, detail),
            HttpStatusCode.NotFound => new KeycloakAdminError(KeycloakAdminErrorCode.NotFound, detail),
            HttpStatusCode.Conflict => new KeycloakAdminError(KeycloakAdminErrorCode.Conflict, detail),
            _ => new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, detail),
        };
    }

    private static async Task<string> ReadErrorDetailAsync(HttpResponseMessage response, string prefix, CancellationToken cancellationToken)
    {
        string detail = await response.Content.ReadAsStringAsync(cancellationToken);
        string normalized = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail.Trim()}";
        return $"{prefix} Status {(int)response.StatusCode}.{normalized}".Trim();
    }

    private static string BuildPath(string realm, string segment, string? search, int page, int pageSize)
    {
        var builder = new StringBuilder($"admin/realms/{Uri.EscapeDataString(realm)}/{segment}?");
        if (!string.IsNullOrWhiteSpace(search))
            builder.Append($"search={Uri.EscapeDataString(search.Trim())}&");

        builder.Append($"first={Math.Max(page, 0) * Math.Max(pageSize, 1)}&max={Math.Max(pageSize, 1)}");
        return builder.ToString();
    }

    private static string? ReadResourceId(Uri? location)
    {
        if (location is null)
            return null;

        string[] segments = location.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.LastOrDefault();
    }

    private static KeycloakUserResponse MapUser(KeycloakUserRepresentation user) =>
        new(
            user.Id,
            user.Username,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            user.Email ?? string.Empty,
            user.Enabled);

    private static KeycloakRoleResponse MapRole(KeycloakRoleRepresentation role) =>
        new(
            role.Id,
            role.Name,
            role.Description ?? string.Empty);

    private static KeycloakGroupResponse MapGroup(KeycloakGroupRepresentation group) =>
        new(
            group.Id,
            group.Name,
            group.Path ?? string.Empty);

    private static KeycloakGroupMembershipResponse MapUserGroup(KeycloakGroupRepresentation group) =>
        new(
            group.Id,
            group.Name,
            group.Path ?? string.Empty);

    private static KeycloakUserMembershipResponse MapGroupMember(KeycloakUserRepresentation user) =>
        new(
            user.Id,
            user.Username,
            user.FirstName ?? string.Empty,
            user.LastName ?? string.Empty,
            user.Email ?? string.Empty,
            user.Enabled);

    private sealed class KeycloakAdminException(KeycloakAdminError error) : Exception(error.Detail)
    {
        public KeycloakAdminError Error { get; } = error;
    }
}

internal static partial class KeycloakTenantAdminLog
{
    [LoggerMessage(
        EventId = 18101,
        Level = LogLevel.Trace,
        Message = "Sending Keycloak admin request {Method} {RelativePath}")]
    public static partial void SendingAdminRequest(ILogger logger, string method, string relativePath);

    [LoggerMessage(
        EventId = 18102,
        Level = LogLevel.Warning,
        Message = "Keycloak admin request failed for {Method} {RequestUri}. Status {StatusCode}. Detail: {Detail}")]
    public static partial void AdminRequestFailed(ILogger logger, string method, string requestUri, int statusCode, string detail);
}
