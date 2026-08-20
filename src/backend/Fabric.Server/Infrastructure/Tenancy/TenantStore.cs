using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Fabric.Server.Infrastructure.Tenancy;

public sealed class TenantStore(TenantsDbContext dbContext, IMemoryCache cache) : ITenantStore
{
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    public async Task<TenantInfo?> GetTenantAsync(string tenantId, CancellationToken cancellationToken)
    {
        string normalizedTenantId = tenantId.Trim();
        string cacheKey = GetCacheKey(normalizedTenantId);

        if (cache.TryGetValue(cacheKey, out TenantInfo? cachedTenant))
            return cachedTenant;

        Tenant? tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == normalizedTenantId && t.IsActive, cancellationToken);

        if (tenant is null)
            return null;

        var tenantInfo = new TenantInfo(tenant.Id, tenant.Configuration);
        cache.Set(cacheKey, tenantInfo, _cacheDuration);

        return tenantInfo;
    }


    public async Task<List<TenantInfo>> GetAllTenantsAsync(CancellationToken cancellationToken)
    {
        List<Tenant> tenants = await dbContext.Tenants
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return tenants.ConvertAll(t => new TenantInfo(t.Id, t.Configuration));
    }

    public void InvalidateTenant(string tenantId)
    {
        string normalizedTenantId = tenantId.Trim();
        cache.Remove(GetCacheKey(normalizedTenantId));
    }

    private static string GetCacheKey(string tenantId) => $"tenant:{tenantId}";
}
