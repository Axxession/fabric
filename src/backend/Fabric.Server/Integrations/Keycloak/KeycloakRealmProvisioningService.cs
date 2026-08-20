using System.Net;
using System.Net.Http.Headers;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Tenants.Domain;

namespace Fabric.Server.Integrations.Keycloak;

public sealed class KeycloakRealmProvisioningService(
    IHttpClientFactory httpClientFactory,
    KeycloakAdminTokenProvider tokenProvider,
    Microsoft.Extensions.Options.IOptions<KeycloakRealmProvisioningOptions> options,
    ILogger<KeycloakRealmProvisioningService> logger)
{
    private const string PortalClientId = "portal";
    private const string FabricClientId = "fabric";
    private const string InitialAdminUsername = "admin";
    private const string InitialAdminFirstName = "Initial";
    private const string InitialAdminLastName = "Admin";
    private const string InitialAdminPassword = "axxession";
    private const string RealmRoleMapper = "oidc-usermodel-realm-role-mapper";
    private static readonly TimeSpan AdminClientLookupDelay = TimeSpan.FromMilliseconds(250);
    private const int AdminClientLookupAttempts = 10;
    private static readonly string[] SeededRealmRoles =
    [
        FabricRoleDefaults.AdminRole,
        FabricRoleDefaults.HostRole,
        FabricRoleDefaults.ManagerRole,
        FabricRoleDefaults.SecurityOfficerRole,
        FabricRoleDefaults.IntegratorRole,
        FabricRoleDefaults.ContractorEnrollmentRole,
        FabricRoleDefaults.ContractorPlanningRole,
    ];

    public bool CanProvision => options.Value.IsConfigured();

    public async Task<Result<ProvisionKeycloakRealmResult, KeycloakAdminError>> ProvisionTenantRealmAsync(
        string tenantId,
        string displayName,
        string tenantBaseUrl,
        CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured())
        {
            return Result.Failure<ProvisionKeycloakRealmResult, KeycloakAdminError>(
                new KeycloakAdminError(KeycloakAdminErrorCode.IntegrationNotConfigured, "Platform Keycloak realm provisioning is not configured."));
        }

        if (!Uri.TryCreate(tenantBaseUrl, UriKind.Absolute, out Uri? tenantUri))
        {
            return Result.Failure<ProvisionKeycloakRealmResult, KeycloakAdminError>(
                new KeycloakAdminError(KeycloakAdminErrorCode.InvalidRequest, "Tenant base URL must be an absolute URL."));
        }

        KeycloakAdminApiIntegrationConfig config = options.Value.ToAdminApiConfig();

        string realm = tenantId.Trim();
        string tenantBase = tenantBaseUrl.TrimEnd('/');
        string tenantOrigin = tenantUri.GetLeftPart(UriPartial.Authority);
        GrantedRealmAccess? grantedRealmAccess = null;
        Result<ProvisionKeycloakRealmResult, KeycloakAdminError>? result = null;

        try
        {
            using HttpClient masterClient = await CreateAuthorizedClientAsync(config, cancellationToken);
            await EnsureSuccessAsync(
                masterClient,
                HttpMethod.Post,
                "admin/realms",
                new KeycloakRealmWriteRequest(realm, displayName.Trim(), true),
                HttpStatusCode.Created,
                "Keycloak realm creation failed.",
                cancellationToken);

            grantedRealmAccess = await GetRealmAccessAsync(masterClient, config, realm, cancellationToken);

            using HttpClient tenantRealmClient = await CreateAuthorizedClientAsync(config, cancellationToken);

            foreach (string roleName in SeededRealmRoles)
            {
                await EnsureSuccessAsync(
                    tenantRealmClient,
                    HttpMethod.Post,
                    $"admin/realms/{Uri.EscapeDataString(realm)}/roles",
                    new KeycloakRoleWriteRequest(roleName, $"Fabric role {roleName}."),
                    HttpStatusCode.Created,
                    $"Keycloak realm role creation failed for '{roleName}'.",
                    cancellationToken);
            }

            await EnsureSuccessAsync(
                tenantRealmClient,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(realm)}/clients",
                new KeycloakClientWriteRequest(
                    PortalClientId,
                    "openid-connect",
                    true,
                    true,
                    false,
                    false,
                    true,
                    false,
                    tenantBase,
                    tenantBase,
                    [$"{tenantBase}/*"],
                    [tenantOrigin]),
                HttpStatusCode.Created,
                "Keycloak portal client creation failed.",
                cancellationToken);

            KeycloakClientRepresentation portalClient = await GetRequiredClientAsync(tenantRealmClient, realm, PortalClientId, cancellationToken);

            await EnsureSuccessAsync(
                tenantRealmClient,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(realm)}/clients/{Uri.EscapeDataString(portalClient.Id)}/protocol-mappers/models",
                new KeycloakProtocolMapperWriteRequest(
                    "realm-roles",
                    "openid-connect",
                    RealmRoleMapper,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["access.token.claim"] = "true",
                        ["id.token.claim"] = "true",
                        ["userinfo.token.claim"] = "true",
                        ["claim.name"] = "roles",
                        ["jsonType.label"] = "String",
                        ["multivalued"] = "true",
                        ["usermodel.realmRoleMapping.rolePrefix"] = string.Empty,
                    }),
                HttpStatusCode.Created,
                "Keycloak portal role mapper creation failed.",
                cancellationToken);

            await EnsureSuccessAsync(
                tenantRealmClient,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(realm)}/clients",
                new KeycloakClientWriteRequest(
                    FabricClientId,
                    "openid-connect",
                    true,
                    false,
                    false,
                    false,
                    false,
                    true,
                    null,
                    null,
                    [],
                    []),
                HttpStatusCode.Created,
                "Keycloak fabric client creation failed.",
                cancellationToken);

            KeycloakClientRepresentation fabricClient = await GetRequiredClientAsync(tenantRealmClient, realm, FabricClientId, cancellationToken);
            KeycloakClientSecretRepresentation clientSecret = await SendRequiredAsync<KeycloakClientSecretRepresentation>(
                tenantRealmClient,
                HttpMethod.Get,
                $"admin/realms/{Uri.EscapeDataString(realm)}/clients/{Uri.EscapeDataString(fabricClient.Id)}/client-secret",
                null,
                "Keycloak fabric client secret request failed.",
                cancellationToken);

            if (string.IsNullOrWhiteSpace(clientSecret.Value))
            {
                return Result.Failure<ProvisionKeycloakRealmResult, KeycloakAdminError>(
                    new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak fabric client secret response did not include a secret value."));
            }

            KeycloakUserIdRepresentation serviceAccount = await SendRequiredAsync<KeycloakUserIdRepresentation>(
                tenantRealmClient,
                HttpMethod.Get,
                $"admin/realms/{Uri.EscapeDataString(realm)}/clients/{Uri.EscapeDataString(fabricClient.Id)}/service-account-user",
                null,
                "Keycloak fabric service account request failed.",
                cancellationToken);

            foreach (string managementClientId in new[] { "account", "realm-management" })
            {
                KeycloakClientRepresentation managementClient = await GetRequiredClientAsync(tenantRealmClient, realm, managementClientId, cancellationToken);
                KeycloakClientRoleRepresentation[] roles = await SendRequiredAsync<KeycloakClientRoleRepresentation[]>(
                    tenantRealmClient,
                    HttpMethod.Get,
                    $"admin/realms/{Uri.EscapeDataString(realm)}/clients/{Uri.EscapeDataString(managementClient.Id)}/roles",
                    null,
                    $"Keycloak {managementClientId} role request failed.",
                    cancellationToken);

                if (roles.Length == 0)
                    continue;

                KeycloakClientRoleMappingWriteRequest[] assignments = roles
                    .Where(role => !string.IsNullOrWhiteSpace(role.Id) && !string.IsNullOrWhiteSpace(role.Name))
                    .Select(role => new KeycloakClientRoleMappingWriteRequest(role.Id, role.Name, role.ClientRole, role.ContainerId))
                    .ToArray();

                if (assignments.Length == 0)
                    continue;

                await EnsureSuccessAsync(
                    tenantRealmClient,
                    HttpMethod.Post,
                    $"admin/realms/{Uri.EscapeDataString(realm)}/users/{Uri.EscapeDataString(serviceAccount.Id)}/role-mappings/clients/{Uri.EscapeDataString(managementClient.Id)}",
                    assignments,
                    HttpStatusCode.NoContent,
                    $"Keycloak {managementClientId} role assignment failed.",
                    cancellationToken);
            }

            using HttpResponseMessage createUserResponse = await SendResponseAsync(
                tenantRealmClient,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(realm)}/users",
                new KeycloakUserWriteRequest(
                    InitialAdminUsername,
                    $"{InitialAdminUsername}@{realm}.local",
                    InitialAdminFirstName,
                    InitialAdminLastName,
                    true),
                cancellationToken);

            if (createUserResponse.StatusCode is not HttpStatusCode.Created)
                throw new KeycloakRealmProvisioningException(await MapErrorAsync(createUserResponse, "Keycloak initial admin user creation failed.", cancellationToken));

            string? userId = ReadResourceId(createUserResponse.Headers.Location);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new KeycloakRealmProvisioningException(
                    new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak initial admin user creation did not return a resource location."));
            }

            await EnsureSuccessAsync(
                tenantRealmClient,
                HttpMethod.Put,
                $"admin/realms/{Uri.EscapeDataString(realm)}/users/{Uri.EscapeDataString(userId)}/reset-password",
                new KeycloakResetPasswordRequest("password", InitialAdminPassword, true),
                HttpStatusCode.NoContent,
                "Keycloak initial admin password setup failed.",
                cancellationToken);

            KeycloakRoleRepresentation[] seededRoles = await Task.WhenAll(
                    SeededRealmRoles.Select(roleName => GetRequiredRealmRoleAsync(tenantRealmClient, realm, roleName, cancellationToken)))
                .ConfigureAwait(false);

            KeycloakRoleMappingRepresentation[] initialAdminRoles = seededRoles
                .Where(role => !string.IsNullOrWhiteSpace(role.Id) && !string.IsNullOrWhiteSpace(role.Name))
                .Select(role => new KeycloakRoleMappingRepresentation(role.Id, role.Name))
                .ToArray();

            await EnsureSuccessAsync(
                tenantRealmClient,
                HttpMethod.Post,
                $"admin/realms/{Uri.EscapeDataString(realm)}/users/{Uri.EscapeDataString(userId)}/role-mappings/realm",
                initialAdminRoles,
                HttpStatusCode.NoContent,
                "Keycloak initial admin realm role assignment failed.",
                cancellationToken);

            string metadataUrl = $"{config.Url.TrimEnd('/')}/realms/{Uri.EscapeDataString(realm)}/.well-known/openid-configuration";
            result = Result.Success<ProvisionKeycloakRealmResult, KeycloakAdminError>(
                new ProvisionKeycloakRealmResult(realm, metadataUrl, PortalClientId, FabricClientId, clientSecret.Value.Trim()));
        }
        catch (KeycloakRealmProvisioningException exception)
        {
            result = Result.Failure<ProvisionKeycloakRealmResult, KeycloakAdminError>(exception.Error);
        }

        if (grantedRealmAccess is not null)
        {
            try
            {
                using HttpClient cleanupClient = await CreateAuthorizedClientAsync(config, cancellationToken);
                await RemoveRealmAccessAsync(cleanupClient, config, grantedRealmAccess, cancellationToken);
            }
            catch (KeycloakRealmProvisioningException cleanupException)
            {
                KeycloakRealmProvisioningLog.RealmAccessCleanupFailed(logger, grantedRealmAccess.Realm, cleanupException.Error.Detail);
                if (result is { } currentResult && currentResult.IsSuccess(out _))
                {
                    result = Result.Failure<ProvisionKeycloakRealmResult, KeycloakAdminError>(cleanupException.Error);
                }
            }
        }

        return result ?? Result.Failure<ProvisionKeycloakRealmResult, KeycloakAdminError>(
            new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, "Keycloak realm provisioning did not produce a result."));
    }

    private async Task<GrantedRealmAccess> GetRealmAccessAsync(HttpClient masterClient, KeycloakAdminApiIntegrationConfig config, string realm, CancellationToken cancellationToken)
    {
        KeycloakClientRepresentation bootstrapClient = await GetRequiredClientAsync(masterClient, config.Realm, config.ClientId, cancellationToken);
        KeycloakUserIdRepresentation bootstrapServiceAccount = await SendRequiredAsync<KeycloakUserIdRepresentation>(
            masterClient,
            HttpMethod.Get,
            $"admin/realms/{Uri.EscapeDataString(config.Realm)}/clients/{Uri.EscapeDataString(bootstrapClient.Id)}/service-account-user",
            null,
            "Keycloak bootstrap service account request failed.",
            cancellationToken);

        KeycloakClientRepresentation targetRealmAdminClient = await GetRequiredClientWithRetryAsync(
            masterClient,
            config.Realm,
            $"{realm}-realm",
            cancellationToken);

        KeycloakClientRoleRepresentation[] roles = await SendRequiredAsync<KeycloakClientRoleRepresentation[]>(
            masterClient,
            HttpMethod.Get,
            $"admin/realms/{Uri.EscapeDataString(config.Realm)}/clients/{Uri.EscapeDataString(targetRealmAdminClient.Id)}/roles",
            null,
            $"Keycloak realm admin roles lookup failed for '{realm}'.",
            cancellationToken);

        KeycloakClientRoleMappingWriteRequest[] mappings = roles
            .Where(role => !string.IsNullOrWhiteSpace(role.Id) && !string.IsNullOrWhiteSpace(role.Name))
            .Select(role => new KeycloakClientRoleMappingWriteRequest(role.Id, role.Name, role.ClientRole, role.ContainerId))
            .ToArray();

        if (mappings.Length == 0)
        {
            throw new KeycloakRealmProvisioningException(
                new KeycloakAdminError(KeycloakAdminErrorCode.NotFound, $"Keycloak admin client '{realm}-realm' did not expose any roles to grant."));
        }

        await EnsureSuccessAsync(
            masterClient,
            HttpMethod.Post,
            $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(bootstrapServiceAccount.Id)}/role-mappings/clients/{Uri.EscapeDataString(targetRealmAdminClient.Id)}",
            mappings,
            HttpStatusCode.NoContent,
            $"Keycloak bootstrap realm access assignment failed for '{realm}'.",
            cancellationToken);

        tokenProvider.Invalidate(config);
        return new GrantedRealmAccess(realm, targetRealmAdminClient.Id, bootstrapServiceAccount.Id, mappings);
    }

    private async Task RemoveRealmAccessAsync(HttpClient masterClient, KeycloakAdminApiIntegrationConfig config, GrantedRealmAccess access, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(
            masterClient,
            HttpMethod.Delete,
            $"admin/realms/{Uri.EscapeDataString(config.Realm)}/users/{Uri.EscapeDataString(access.ServiceAccountUserId)}/role-mappings/clients/{Uri.EscapeDataString(access.RealmAdminClientId)}",
            access.Roles,
            HttpStatusCode.NoContent,
            $"Keycloak bootstrap realm access cleanup failed for '{access.Realm}'.",
            cancellationToken);

        tokenProvider.Invalidate(config);
    }

    private async Task<HttpClient> CreateAuthorizedClientAsync(KeycloakAdminApiIntegrationConfig config, CancellationToken cancellationToken)
    {
        Result<AuthenticationHeaderValue, KeycloakAdminError> authResult = await tokenProvider.GetAuthorizationHeaderAsync(config, cancellationToken);
        if (authResult.IsFailure(out KeycloakAdminError authError))
            throw new KeycloakRealmProvisioningException(authError);

        authResult.IsSuccess(out AuthenticationHeaderValue authorizationHeader);
        HttpClient client = httpClientFactory.CreateClient(KeycloakIntegrationServiceCollectionExtensions.HttpClientName);
        client.BaseAddress = new Uri($"{config.Url.TrimEnd('/')}/");
        client.DefaultRequestHeaders.Authorization = authorizationHeader;
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private async Task<KeycloakClientRepresentation> GetRequiredClientAsync(HttpClient client, string realm, string clientId, CancellationToken cancellationToken)
    {
        KeycloakClientRepresentation[] clients = await SendRequiredAsync<KeycloakClientRepresentation[]>(
            client,
            HttpMethod.Get,
            $"admin/realms/{Uri.EscapeDataString(realm)}/clients?clientId={Uri.EscapeDataString(clientId)}",
            null,
            $"Keycloak client lookup failed for '{clientId}'.",
            cancellationToken);

        KeycloakClientRepresentation? match = clients.FirstOrDefault(item => string.Equals(item.ClientId, clientId, StringComparison.Ordinal));
        return match ?? throw new KeycloakRealmProvisioningException(
            new KeycloakAdminError(KeycloakAdminErrorCode.NotFound, $"Keycloak client '{clientId}' was not found after provisioning."));
    }

    private async Task<KeycloakClientRepresentation> GetRequiredClientWithRetryAsync(HttpClient client, string realm, string clientId, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < AdminClientLookupAttempts; attempt++)
        {
            try
            {
                return await GetRequiredClientAsync(client, realm, clientId, cancellationToken);
            }
            catch (KeycloakRealmProvisioningException exception) when (exception.Error.Code == KeycloakAdminErrorCode.NotFound && attempt + 1 < AdminClientLookupAttempts)
            {
                await Task.Delay(AdminClientLookupDelay, cancellationToken);
            }
        }

        throw new KeycloakRealmProvisioningException(
            new KeycloakAdminError(KeycloakAdminErrorCode.NotFound, $"Keycloak client '{clientId}' was not found after waiting for realm bootstrap objects."));
    }

    private async Task<KeycloakRoleRepresentation> GetRequiredRealmRoleAsync(HttpClient client, string realm, string roleName, CancellationToken cancellationToken) =>
        await SendRequiredAsync<KeycloakRoleRepresentation>(
            client,
            HttpMethod.Get,
            $"admin/realms/{Uri.EscapeDataString(realm)}/roles/{Uri.EscapeDataString(roleName)}",
            null,
            $"Keycloak realm role lookup failed for '{roleName}'.",
            cancellationToken);

    private async Task EnsureSuccessAsync(HttpClient client, HttpMethod method, string relativePath, object? body, HttpStatusCode expectedStatusCode, string prefix, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendResponseAsync(client, method, relativePath, body, cancellationToken);
        if (response.StatusCode == expectedStatusCode)
            return;

        throw new KeycloakRealmProvisioningException(await MapErrorAsync(response, prefix, cancellationToken));
    }

    private async Task<T> SendRequiredAsync<T>(HttpClient client, HttpMethod method, string relativePath, object? body, string prefix, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendResponseAsync(client, method, relativePath, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new KeycloakRealmProvisioningException(await MapErrorAsync(response, prefix, cancellationToken));

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        T? value = KeycloakJson.Deserialize<T>(json);
        return value ?? throw new KeycloakRealmProvisioningException(
            new KeycloakAdminError(KeycloakAdminErrorCode.ExternalServiceError, $"{prefix} Response body was empty."));
    }

    private async Task<HttpResponseMessage> SendResponseAsync(HttpClient client, HttpMethod method, string relativePath, object? body, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(method, relativePath);
        if (body is not null)
        {
            request.Content = body switch
            {
                KeycloakRealmWriteRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakClientWriteRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakProtocolMapperWriteRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakResetPasswordRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakRoleMappingRepresentation[] value => KeycloakJson.CreateJsonContent(value),
                KeycloakRoleWriteRequest value => KeycloakJson.CreateJsonContent(value),
                KeycloakClientRoleMappingWriteRequest[] value => KeycloakJson.CreateJsonContent(value),
                KeycloakUserWriteRequest value => KeycloakJson.CreateJsonContent(value),
                _ => throw new InvalidOperationException($"Type {body.GetType().FullName} is not registered for Keycloak request serialization."),
            };
        }

        KeycloakRealmProvisioningLog.SendingAdminRequest(logger, method.Method, relativePath);
        return await client.SendAsync(request, cancellationToken);
    }

    private async Task<KeycloakAdminError> MapErrorAsync(HttpResponseMessage response, string prefix, CancellationToken cancellationToken)
    {
        string detail = await ReadErrorDetailAsync(response, prefix, cancellationToken);
        KeycloakRealmProvisioningLog.AdminRequestFailed(
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

    private static string? ReadResourceId(Uri? location)
    {
        if (location is null)
            return null;

        string[] segments = location.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.LastOrDefault();
    }

    private static async Task<string> ReadErrorDetailAsync(HttpResponseMessage response, string prefix, CancellationToken cancellationToken)
    {
        string detail = await response.Content.ReadAsStringAsync(cancellationToken);
        string normalized = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail.Trim()}";
        return $"{prefix} Status {(int)response.StatusCode}.{normalized}".Trim();
    }

    private sealed class KeycloakRealmProvisioningException(KeycloakAdminError error) : Exception(error.Detail)
    {
        public KeycloakAdminError Error { get; } = error;
    }
}

public sealed record ProvisionKeycloakRealmResult(
    string Realm,
    string MetadataUrl,
    string PortalClientId,
    string FabricClientId,
    string FabricClientSecret);

internal sealed record GrantedRealmAccess(
    string Realm,
    string RealmAdminClientId,
    string ServiceAccountUserId,
    KeycloakClientRoleMappingWriteRequest[] Roles);

internal static partial class KeycloakRealmProvisioningLog
{
    [LoggerMessage(
        EventId = 18150,
        Level = LogLevel.Trace,
        Message = "Sending Keycloak provisioning request {Method} {RelativePath}")]
    public static partial void SendingAdminRequest(ILogger logger, string method, string relativePath);

    [LoggerMessage(
        EventId = 18151,
        Level = LogLevel.Warning,
        Message = "Keycloak provisioning request failed for {Method} {RequestUri}. Status {StatusCode}. Detail: {Detail}")]
    public static partial void AdminRequestFailed(ILogger logger, string method, string requestUri, int statusCode, string detail);

    [LoggerMessage(
        EventId = 18152,
        Level = LogLevel.Warning,
        Message = "Keycloak bootstrap realm access cleanup failed for realm {Realm}. Detail: {Detail}")]
    public static partial void RealmAccessCleanupFailed(ILogger logger, string realm, string detail);
}
