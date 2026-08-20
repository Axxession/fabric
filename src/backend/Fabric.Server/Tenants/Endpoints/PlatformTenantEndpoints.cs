using System.Text.Json;
using Fabric.Server.Core;
using Fabric.Server.Integrations.Keycloak;
using Fabric.Server.Integrations.Keycloak.Endpoints;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Contracts;
using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fabric.Server.Tenants.Endpoints;

public static class PlatformTenantEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapPlatformTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/platform/auth/settings", GetPlatformAuthSettings)
            .AllowAnonymous()
            .Produces<PlatformAuthSettingsResponse>();

        RouteGroupBuilder group = app.MapGroup("/api/platform")
            .RequireAuthorization(FabricRoleDefaults.PlatformAdminPolicy);

        group.MapGet("/tenants", ListTenants)
            .Produces<PlatformTenantListItemResponse[]>();

        group.MapGet("/tenants/{tenantId}", GetTenant)
            .Produces<PlatformTenantResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/tenants", CreateTenant)
            .Produces<PlatformTenantResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapPut("/tenants/{tenantId}", UpdateTenant)
            .Produces<PlatformTenantResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/tenants/{tenantId}/deactivate", DeactivateTenant)
            .Produces<PlatformTenantResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/tenants/{tenantId}/activate", ActivateTenant)
            .Produces<PlatformTenantResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/tenants/{tenantId}/keycloak/provision", ProvisionTenantKeycloak)
            .Produces<PlatformTenantResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);

        return app;
    }

    private static IResult GetPlatformAuthSettings(IOptions<AdminOidcOptions> options)
    {
        AdminOidcOptions adminOidc = options.Value;
        return Results.Ok(new PlatformAuthSettingsResponse(new OidcSettingsResponse(
            adminOidc.MetadataUrl!,
            adminOidc.ClientId!,
            adminOidc.RequireHttpsMetadata)));
    }

    private static async Task<IResult> ListTenants(
        TenantsDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        List<PlatformTenantListItemResponse> response = await dbContext.Tenants
            .AsNoTracking()
            .OrderBy(tenant => tenant.DisplayName)
            .ThenBy(tenant => tenant.Id)
            .Select(tenant => new PlatformTenantListItemResponse(
                tenant.Id,
                tenant.DisplayName,
                tenant.IsActive,
                tenant.CreatedAtUtc,
                tenant.UpdatedAtUtc,
                tenant.Configuration.Oidc.ToResponse(),
                (tenant.Configuration.Host ?? new HostSettings()).ToResponse()))
            .ToListAsync(cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetTenant(
        string tenantId,
        TenantsDbContext dbContext,
        IOptions<KeycloakRealmProvisioningOptions> provisioningOptions,
        CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == tenantId.Trim(), cancellationToken);

        if (tenant is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Tenant not found", detail: $"Tenant '{tenantId}' does not exist.");

        List<TenantIntegration> integrations = await dbContext.TenantIntegrations
            .AsNoTracking()
            .Where(item => item.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToPlatformResponse(tenant, integrations, provisioningOptions.Value.IsConfigured()));
    }

    private static async Task<IResult> CreateTenant(
        [FromBody] CreatePlatformTenantRequest request,
        TenantsDbContext dbContext,
        TimeProvider timeProvider,
        IOptions<KeycloakRealmProvisioningOptions> provisioningOptions,
        CancellationToken cancellationToken = default)
    {
        IResult? validationResult = ValidateCreateRequest(request);
        if (validationResult is not null)
            return validationResult;

        string tenantId = request.Id.Trim();
        bool exists = await dbContext.Tenants.AnyAsync(item => item.Id == tenantId, cancellationToken);
        if (exists)
            return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Tenant already exists", detail: $"Tenant '{tenantId}' already exists.");

        DateTimeOffset now = timeProvider.GetUtcNow();
        Tenant tenant = Tenant.Create(
            tenantId,
            request.DisplayName.Trim(),
            new TenantConfiguration
            {
                Oidc = new OidcSettings
                {
                    MetadataUrl = request.Oidc.MetadataUrl.Trim(),
                    ClientId = request.Oidc.ClientId.Trim(),
                    RequireHttpsMetadata = request.Oidc.RequireHttpsMetadata,
                }
            },
            now);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/platform/tenants/{tenant.Id}", ToPlatformResponse(tenant, [], provisioningOptions.Value.IsConfigured()));
    }

    private static async Task<IResult> UpdateTenant(
        string tenantId,
        [FromBody] UpdatePlatformTenantRequest request,
        TenantsDbContext dbContext,
        ITenantStore tenantStore,
        TimeProvider timeProvider,
        IOptions<KeycloakRealmProvisioningOptions> provisioningOptions,
        CancellationToken cancellationToken = default)
    {
        IResult? validationResult = ValidateUpdateRequest(request);
        if (validationResult is not null)
            return validationResult;

        Tenant? tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(item => item.Id == tenantId.Trim(), cancellationToken);

        if (tenant is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Tenant not found", detail: $"Tenant '{tenantId}' does not exist.");

        DateTimeOffset now = timeProvider.GetUtcNow();
        string normalizedDisplayName = request.DisplayName.Trim();
        string normalizedMetadataUrl = request.Oidc.MetadataUrl.Trim();
        string normalizedClientId = request.Oidc.ClientId.Trim();

        await dbContext.Tenants
            .Where(item => item.Id == tenant.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.DisplayName, normalizedDisplayName)
                .SetProperty(item => item.UpdatedAtUtc, now)
                .SetProperty(item => item.Configuration.Oidc.MetadataUrl, normalizedMetadataUrl)
                .SetProperty(item => item.Configuration.Oidc.ClientId, normalizedClientId)
                .SetProperty(item => item.Configuration.Oidc.RequireHttpsMetadata, request.Oidc.RequireHttpsMetadata),
                cancellationToken);

        tenantStore.InvalidateTenant(tenant.Id);

        tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleAsync(item => item.Id == tenant.Id, cancellationToken);

        List<TenantIntegration> integrations = await dbContext.TenantIntegrations
            .AsNoTracking()
            .Where(item => item.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToPlatformResponse(tenant, integrations, provisioningOptions.Value.IsConfigured()));
    }

    private static async Task<IResult> DeactivateTenant(
        string tenantId,
        TenantsDbContext dbContext,
        ITenantStore tenantStore,
        TimeProvider timeProvider,
        IOptions<KeycloakRealmProvisioningOptions> provisioningOptions,
        CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(item => item.Id == tenantId.Trim(), cancellationToken);

        if (tenant is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Tenant not found", detail: $"Tenant '{tenantId}' does not exist.");

        tenant.Deactivate(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        tenantStore.InvalidateTenant(tenant.Id);

        List<TenantIntegration> integrations = await dbContext.TenantIntegrations
            .AsNoTracking()
            .Where(item => item.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToPlatformResponse(tenant, integrations, provisioningOptions.Value.IsConfigured()));
    }

    private static async Task<IResult> ActivateTenant(
        string tenantId,
        TenantsDbContext dbContext,
        ITenantStore tenantStore,
        TimeProvider timeProvider,
        IOptions<KeycloakRealmProvisioningOptions> provisioningOptions,
        CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(item => item.Id == tenantId.Trim(), cancellationToken);

        if (tenant is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Tenant not found", detail: $"Tenant '{tenantId}' does not exist.");

        tenant.Activate(timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        tenantStore.InvalidateTenant(tenant.Id);

        List<TenantIntegration> integrations = await dbContext.TenantIntegrations
            .AsNoTracking()
            .Where(item => item.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToPlatformResponse(tenant, integrations, provisioningOptions.Value.IsConfigured()));
    }

    private static async Task<IResult> ProvisionTenantKeycloak(
        string tenantId,
        TenantsDbContext dbContext,
        ITenantStore tenantStore,
        TimeProvider timeProvider,
        IOptions<TenancyOptions> tenancyOptions,
        IOptions<KeycloakRealmProvisioningOptions> provisioningOptions,
        KeycloakRealmProvisioningService provisioningService,
        CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(item => item.Id == tenantId.Trim(), cancellationToken);

        if (tenant is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Tenant not found", detail: $"Tenant '{tenantId}' does not exist.");

        List<TenantIntegration> integrations = await dbContext.TenantIntegrations
            .Where(item => item.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);

        TenantIntegration? currentKeycloak = integrations.SingleOrDefault(item => item.Name == TenantIntegrationName.Keycloak);
        if (IsKeycloakIntegrationConfigured(currentKeycloak))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Keycloak already configured",
                detail: $"Tenant '{tenant.Id}' already has a configured Keycloak integration.");
        }

        string tenantBaseUrl = ResolveTenantBaseUrl(tenancyOptions.Value.TenantBaseUrl, tenant.Id);
        Result<ProvisionKeycloakRealmResult, KeycloakAdminError> provisionResult = await provisioningService.ProvisionTenantRealmAsync(
            tenant.Id,
            tenant.DisplayName,
            tenantBaseUrl,
            cancellationToken);

        if (provisionResult.IsFailure(out KeycloakAdminError provisionError))
            return ResultsExtensions.Problem(provisionError);

        provisionResult.IsSuccess(out ProvisionKeycloakRealmResult provisionedRealm);
        DateTimeOffset now = timeProvider.GetUtcNow();
        bool requireHttpsMetadata = string.Equals(new Uri(provisionedRealm.MetadataUrl).Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

        await dbContext.Tenants
            .Where(item => item.Id == tenant.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.UpdatedAtUtc, now)
                .SetProperty(item => item.Configuration.Oidc.MetadataUrl, provisionedRealm.MetadataUrl)
                .SetProperty(item => item.Configuration.Oidc.ClientId, provisionedRealm.PortalClientId)
                .SetProperty(item => item.Configuration.Oidc.RequireHttpsMetadata, requireHttpsMetadata),
                cancellationToken);

        string integrationJson = JsonSerializer.Serialize(
            new KeycloakIntegrationConfig
            {
                AdminApi = new KeycloakAdminApiIntegrationConfig
                {
                    IsEnabled = true,
                    Url = provisioningOptions.Value.Url.Trim(),
                    Realm = provisionedRealm.Realm,
                    ClientId = provisionedRealm.FabricClientId,
                    ClientSecret = provisionedRealm.FabricClientSecret,
                }
            },
            JsonOptions);

        if (currentKeycloak is null)
        {
            currentKeycloak = TenantIntegration.Create(tenant.Id, TenantIntegrationName.Keycloak, integrationJson, now);
            dbContext.TenantIntegrations.Add(currentKeycloak);
            integrations.Add(currentKeycloak);
        }
        else
        {
            currentKeycloak.UpdateData(integrationJson, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        tenantStore.InvalidateTenant(tenant.Id);

        tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleAsync(item => item.Id == tenant.Id, cancellationToken);

        return Results.Ok(ToPlatformResponse(tenant, integrations, provisioningService.CanProvision));
    }

    private static PlatformTenantResponse ToPlatformResponse(Tenant tenant, IEnumerable<TenantIntegration> integrations, bool canProvisionKeycloakRealm)
    {
        TenantIntegration? keycloak = integrations.SingleOrDefault(item => item.Name == TenantIntegrationName.Keycloak);
        TenantIntegration? microsoftGraph = integrations.SingleOrDefault(item => item.Name == TenantIntegrationName.MicrosoftGraph);

        return new PlatformTenantResponse(
            tenant.Id,
            tenant.DisplayName,
            tenant.IsActive,
            tenant.CreatedAtUtc,
            tenant.UpdatedAtUtc,
            tenant.Configuration.Oidc.ToResponse(),
            tenant.Configuration.Logo?.ToResponse(),
            (tenant.Configuration.Host ?? new HostSettings()).ToResponse(),
            canProvisionKeycloakRealm,
            ToKeycloakSummary(keycloak),
            ToMicrosoftGraphSummary(microsoftGraph));
    }

    private static PlatformTenantIntegrationSummaryResponse ToKeycloakSummary(TenantIntegration? integration)
    {
        KeycloakIntegrationConfig? config = Deserialize<KeycloakIntegrationConfig>(integration);
        return new PlatformTenantIntegrationSummaryResponse(
            config?.AdminApi.IsConfigured() ?? false,
            config?.AdminApi.IsEnabled ?? false,
            !string.IsNullOrWhiteSpace(config?.AdminApi.ClientSecret),
            integration?.UpdatedAt);
    }

    private static PlatformTenantIntegrationSummaryResponse ToMicrosoftGraphSummary(TenantIntegration? integration)
    {
        MicrosoftGraphIntegrationConfig? config = Deserialize<MicrosoftGraphIntegrationConfig>(integration);
        return new PlatformTenantIntegrationSummaryResponse(
            config?.Email.IsConfigured() ?? false,
            config?.Email.IsEnabled ?? false,
            !string.IsNullOrWhiteSpace(config?.Email.Secret),
            integration?.UpdatedAt);
    }

    private static bool IsKeycloakIntegrationConfigured(TenantIntegration? integration) =>
        Deserialize<KeycloakIntegrationConfig>(integration)?.AdminApi.IsConfigured() ?? false;

    private static string ResolveTenantBaseUrl(string tenantBaseUrl, string tenantId)
    {
        string resolved = tenantBaseUrl.Replace("{tenant}", tenantId, StringComparison.OrdinalIgnoreCase).TrimEnd('/');
        if (!Uri.TryCreate(resolved, UriKind.Absolute, out _))
            throw new InvalidOperationException("Tenancy:TenantBaseUrl must be an absolute URL.");

        return resolved;
    }

    private static HostSettingsResponse ToResponse(this HostSettings host) =>
        new(host.AssignmentMode);

    private static TConfig? Deserialize<TConfig>(TenantIntegration? integration) where TConfig : class =>
        integration is null ? null : JsonSerializer.Deserialize<TConfig>(integration.DataJson, JsonOptions);

    private static IResult? ValidateCreateRequest(CreatePlatformTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
            return ValidationProblem("Tenant id is required.");

        string tenantId = request.Id.Trim();
        if (tenantId.Length > 100)
            return ValidationProblem("Tenant id must be 100 characters or fewer.");

        if (!tenantId.All(character => char.IsLetterOrDigit(character) || character is '-' or '_'))
            return ValidationProblem("Tenant id may only contain letters, numbers, hyphens, and underscores.");

        return ValidateUpdateCore(request.DisplayName, request.Oidc);
    }

    private static IResult? ValidateUpdateRequest(UpdatePlatformTenantRequest request) =>
        ValidateUpdateCore(request.DisplayName, request.Oidc);

    private static IResult? ValidateUpdateCore(string displayName, UpdateOidcSettingsRequest oidc)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return ValidationProblem("Display name is required.");

        if (displayName.Trim().Length > 200)
            return ValidationProblem("Display name must be 200 characters or fewer.");

        if (string.IsNullOrWhiteSpace(oidc.MetadataUrl) || !Uri.TryCreate(oidc.MetadataUrl, UriKind.Absolute, out Uri? metadataUrl))
            return ValidationProblem("OIDC metadata URL must be an absolute URL.");

        if (oidc.RequireHttpsMetadata && metadataUrl.Scheme != Uri.UriSchemeHttps)
            return ValidationProblem("OIDC metadata URL must use HTTPS when HTTPS metadata is required.");

        if (string.IsNullOrWhiteSpace(oidc.ClientId))
            return ValidationProblem("OIDC client ID is required.");

        return null;
    }

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid platform tenant request", detail: detail);
}
