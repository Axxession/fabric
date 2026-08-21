using Fabric.Server.Infrastructure.Tenancy;

namespace Fabric.Server.Automation;

public sealed class AutomationTenantScopeRunner(IServiceScopeFactory scopeFactory)
{
    public async Task RunInTenantScopeAsync(string tenantId, Func<IServiceProvider, CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ITenantStore tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
        TenantInfo? tenant = await tenantStore.GetTenantAsync(tenantId.Trim(), cancellationToken);
        if (tenant is null)
            throw new InvalidOperationException($"Tenant '{tenantId}' does not exist.");

        ITenantContextAccessor tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        tenantContext.SetTenant(tenant);
        await action(scope.ServiceProvider, cancellationToken);
    }

    public async Task<TResult> RunInTenantScopeAsync<TResult>(string tenantId, Func<IServiceProvider, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ITenantStore tenantStore = scope.ServiceProvider.GetRequiredService<ITenantStore>();
        TenantInfo? tenant = await tenantStore.GetTenantAsync(tenantId.Trim(), cancellationToken);
        if (tenant is null)
            throw new InvalidOperationException($"Tenant '{tenantId}' does not exist.");

        ITenantContextAccessor tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContextAccessor>();
        tenantContext.SetTenant(tenant);
        return await action(scope.ServiceProvider, cancellationToken);
    }
}
