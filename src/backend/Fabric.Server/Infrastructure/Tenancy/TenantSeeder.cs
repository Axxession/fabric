using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Fabric.Server.Infrastructure.Tenancy;

public sealed class TenantSeeder(TenantsDbContext dbContext, IOptions<TenancyOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (options.Value.Mode != TenancyMode.SingleTenant)
            return;

        DefaultTenantOptions defaultTenant = options.Value.DefaultTenant;

        OidcSettings configuredOidc = new()
        {
            MetadataUrl = defaultTenant.Oidc.MetadataUrl!,
            ClientId = defaultTenant.Oidc.ClientId!,
            RequireHttpsMetadata = defaultTenant.Oidc.RequireHttpsMetadata
        };

        Tenant? tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(item => item.Id == defaultTenant.Id, cancellationToken);

        if (tenant is null)
        {
            tenant = Tenant.Create(defaultTenant.Id, new TenantConfiguration
            {
                Oidc = configuredOidc
            });

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
            await SeedIntegrationsAsync(defaultTenant, cancellationToken);
            return;
        }

        if (tenant.Configuration.Oidc == configuredOidc)
        {
            await SeedIntegrationsAsync(defaultTenant, cancellationToken);
            return;
        }

        await dbContext.Tenants
            .Where(item => item.Id == defaultTenant.Id)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Configuration.Oidc.MetadataUrl, _ => configuredOidc.MetadataUrl)
                    .SetProperty(item => item.Configuration.Oidc.ClientId, _ => configuredOidc.ClientId)
                    .SetProperty(item => item.Configuration.Oidc.RequireHttpsMetadata, _ => configuredOidc.RequireHttpsMetadata),
                cancellationToken);

        await SeedIntegrationsAsync(defaultTenant, cancellationToken);
    }

    private async Task SeedIntegrationsAsync(DefaultTenantOptions defaultTenant, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (defaultTenant.GraphEmail is not null)
        {
            MicrosoftGraphIntegrationConfig graph = new()
            {
                Email = new MicrosoftGraphEmailIntegrationConfig
                {
                    IsEnabled = true,
                    FromEmail = defaultTenant.GraphEmail.FromEmail,
                    FromName = defaultTenant.GraphEmail.FromName,
                    AzureTenantId = defaultTenant.GraphEmail.AzureTenantId,
                    ApplicationId = defaultTenant.GraphEmail.ApplicationId,
                    Secret = defaultTenant.GraphEmail.Secret,
                    SaveSentItems = defaultTenant.GraphEmail.SaveSentItems,
                }
            };

            await UpsertIntegrationAsync(defaultTenant.Id, TenantIntegrationName.MicrosoftGraph, JsonSerializer.Serialize(graph, JsonOptions), now, cancellationToken);
        }

        if (defaultTenant.Keycloak is not null)
        {
            KeycloakIntegrationConfig keycloak = new()
            {
                AdminApi = new KeycloakAdminApiIntegrationConfig
                {
                    IsEnabled = true,
                    Url = defaultTenant.Keycloak.Url,
                    Realm = defaultTenant.Keycloak.Realm,
                    ClientId = defaultTenant.Keycloak.ClientId,
                    ClientSecret = defaultTenant.Keycloak.ClientSecret,
                }
            };

            await UpsertIntegrationAsync(defaultTenant.Id, TenantIntegrationName.Keycloak, JsonSerializer.Serialize(keycloak, JsonOptions), now, cancellationToken);
        }
    }

    private async Task UpsertIntegrationAsync(string tenantId, TenantIntegrationName name, string dataJson, DateTimeOffset now, CancellationToken cancellationToken)
    {
        TenantIntegration? integration = await dbContext.TenantIntegrations
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Name == name, cancellationToken);

        if (integration is null)
        {
            dbContext.TenantIntegrations.Add(TenantIntegration.Create(tenantId, name, dataJson, now));
        }
        else
        {
            integration.UpdateData(dataJson, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
