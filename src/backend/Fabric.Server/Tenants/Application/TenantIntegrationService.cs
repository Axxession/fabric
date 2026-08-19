using System.Text.Json;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Contracts;
using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tenants.Application;

public sealed class TenantIntegrationService(
    TenantsDbContext dbContext,
    ITenantContext tenantContext,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<KeycloakIntegrationResponse> GetKeycloakAsync(CancellationToken cancellationToken = default)
    {
        KeycloakIntegrationConfig config = await GetConfigAsync<KeycloakIntegrationConfig>(TenantIntegrationName.Keycloak, cancellationToken) ?? new();
        return new KeycloakIntegrationResponse(new KeycloakAdminApiIntegrationResponse(
            config.AdminApi.IsEnabled,
            config.AdminApi.Url,
            config.AdminApi.Realm,
            config.AdminApi.ClientId,
            !string.IsNullOrWhiteSpace(config.AdminApi.ClientSecret)));
    }

    public async Task<MicrosoftGraphIntegrationResponse> GetMicrosoftGraphAsync(CancellationToken cancellationToken = default)
    {
        MicrosoftGraphIntegrationConfig config = await GetConfigAsync<MicrosoftGraphIntegrationConfig>(TenantIntegrationName.MicrosoftGraph, cancellationToken) ?? new();
        return new MicrosoftGraphIntegrationResponse(new MicrosoftGraphEmailIntegrationResponse(
            config.Email.IsEnabled,
            config.Email.FromEmail,
            config.Email.FromName,
            config.Email.AzureTenantId,
            config.Email.ApplicationId,
            config.Email.SaveSentItems,
            !string.IsNullOrWhiteSpace(config.Email.Secret)));
    }

    public async Task<Result<KeycloakIntegrationResponse, string>> UpdateKeycloakAsync(UpdateKeycloakIntegrationRequest request, CancellationToken cancellationToken = default)
    {
        KeycloakIntegrationConfig current = await GetConfigAsync<KeycloakIntegrationConfig>(TenantIntegrationName.Keycloak, cancellationToken) ?? new();
        string clientSecret = string.IsNullOrWhiteSpace(request.AdminApi.ClientSecret) ? current.AdminApi.ClientSecret : request.AdminApi.ClientSecret.Trim();
        KeycloakIntegrationConfig next = new()
        {
            AdminApi = new KeycloakAdminApiIntegrationConfig
            {
                IsEnabled = request.AdminApi.IsEnabled,
                Url = request.AdminApi.Url.Trim(),
                Realm = request.AdminApi.Realm.Trim(),
                ClientId = request.AdminApi.ClientId.Trim(),
                ClientSecret = clientSecret,
            }
        };

        if (next.AdminApi.IsEnabled)
        {
            if (string.IsNullOrWhiteSpace(next.AdminApi.Url) || !Uri.TryCreate(next.AdminApi.Url, UriKind.Absolute, out _))
                return Result.Failure<KeycloakIntegrationResponse, string>("Keycloak URL must be an absolute URL.");

            if (string.IsNullOrWhiteSpace(next.AdminApi.Realm))
                return Result.Failure<KeycloakIntegrationResponse, string>("Keycloak realm is required when the admin API is enabled.");

            if (string.IsNullOrWhiteSpace(next.AdminApi.ClientId))
                return Result.Failure<KeycloakIntegrationResponse, string>("Keycloak client ID is required when the admin API is enabled.");

            if (string.IsNullOrWhiteSpace(next.AdminApi.ClientSecret))
                return Result.Failure<KeycloakIntegrationResponse, string>("Keycloak client secret is required when the admin API is enabled.");
        }

        await UpsertConfigAsync(TenantIntegrationName.Keycloak, next, cancellationToken);
        return Result.Success<KeycloakIntegrationResponse, string>(await GetKeycloakAsync(cancellationToken));
    }

    public async Task<Result<MicrosoftGraphIntegrationResponse, string>> UpdateMicrosoftGraphAsync(UpdateMicrosoftGraphIntegrationRequest request, CancellationToken cancellationToken = default)
    {
        MicrosoftGraphIntegrationConfig current = await GetConfigAsync<MicrosoftGraphIntegrationConfig>(TenantIntegrationName.MicrosoftGraph, cancellationToken) ?? new();
        string secret = string.IsNullOrWhiteSpace(request.Email.Secret) ? current.Email.Secret : request.Email.Secret.Trim();
        MicrosoftGraphIntegrationConfig next = new()
        {
            Email = new MicrosoftGraphEmailIntegrationConfig
            {
                IsEnabled = request.Email.IsEnabled,
                FromEmail = request.Email.FromEmail.Trim(),
                FromName = request.Email.FromName.Trim(),
                AzureTenantId = request.Email.AzureTenantId.Trim(),
                ApplicationId = request.Email.ApplicationId.Trim(),
                Secret = secret,
                SaveSentItems = request.Email.SaveSentItems,
            }
        };

        if (next.Email.IsEnabled && !next.Email.IsConfigured())
            return Result.Failure<MicrosoftGraphIntegrationResponse, string>("Microsoft Graph email settings must include sender email, sender name, Azure tenant ID, application ID and secret when email is enabled.");

        await UpsertConfigAsync(TenantIntegrationName.MicrosoftGraph, next, cancellationToken);
        return Result.Success<MicrosoftGraphIntegrationResponse, string>(await GetMicrosoftGraphAsync(cancellationToken));
    }

    public async Task<MicrosoftGraphEmailIntegrationConfig?> GetMicrosoftGraphEmailConfigAsync(CancellationToken cancellationToken = default)
    {
        MicrosoftGraphIntegrationConfig? config = await GetConfigAsync<MicrosoftGraphIntegrationConfig>(TenantIntegrationName.MicrosoftGraph, cancellationToken);
        return config?.Email;
    }

    private async Task<TConfig?> GetConfigAsync<TConfig>(TenantIntegrationName name, CancellationToken cancellationToken) where TConfig : class
    {
        TenantIntegration? integration = await dbContext.TenantIntegrations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.TenantId == tenantContext.TenantId && item.Name == name, cancellationToken);

        return integration is null ? null : JsonSerializer.Deserialize<TConfig>(integration.DataJson, JsonOptions);
    }

    private async Task UpsertConfigAsync<TConfig>(TenantIntegrationName name, TConfig config, CancellationToken cancellationToken) where TConfig : class
    {
        TenantIntegration? integration = await dbContext.TenantIntegrations
            .SingleOrDefaultAsync(item => item.TenantId == tenantContext.TenantId && item.Name == name, cancellationToken);

        string json = JsonSerializer.Serialize(config, JsonOptions);
        DateTimeOffset now = timeProvider.GetUtcNow();

        if (integration is null)
        {
            dbContext.TenantIntegrations.Add(TenantIntegration.Create(tenantContext.TenantId, name, json, now));
        }
        else
        {
            integration.UpdateData(json, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
