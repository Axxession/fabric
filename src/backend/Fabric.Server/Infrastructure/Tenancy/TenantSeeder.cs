using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fabric.Server.Infrastructure.Tenancy;

public sealed class TenantSeeder(TenantsDbContext dbContext, IOptions<TenancyOptions> options)
{
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
                Oidc = configuredOidc,
                GraphEmail = defaultTenant.GraphEmail
            });

            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (tenant.Configuration.Oidc == configuredOidc)
            return;

        await dbContext.Tenants
            .Where(item => item.Id == defaultTenant.Id)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(item => item.Configuration.Oidc.MetadataUrl, _ => configuredOidc.MetadataUrl)
                    .SetProperty(item => item.Configuration.Oidc.ClientId, _ => configuredOidc.ClientId)
                    .SetProperty(item => item.Configuration.Oidc.RequireHttpsMetadata, _ => configuredOidc.RequireHttpsMetadata),
                cancellationToken);
    }
}
