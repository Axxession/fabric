using Microsoft.Extensions.Options;

namespace Fabric.Server.Infrastructure.Tenancy;

public sealed class TenantContextMiddleware(
    RequestDelegate next,
    IOptions<TenancyOptions> options)
{
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContextAccessor tenantContext,
        ITenantStore tenantStore)
    {
        if (IsPlatformRequest(context.Request.Path))
        {
            await next(context);
            return;
        }

        string tenantId = options.Value.Mode switch
        {
            TenancyMode.SingleTenant => options.Value.DefaultTenant.Id,
            TenancyMode.MultiTenant => GetTenantFromHost(context.Request.Host.Host),
            _ => options.Value.DefaultTenant.Id,
        };

        TenantInfo? tenant = await tenantStore.GetTenantAsync(tenantId, context.RequestAborted);
        if (tenant is null)
        {
            await Results.Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Tenant not found",
                    detail: $"Tenant '{tenantId}' does not exist.")
                .ExecuteAsync(context);
            return;
        }

        tenantContext.SetTenant(tenant);
        await next(context);
    }

    private static string GetTenantFromHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return TenantContext.DefaultTenantId;

        int firstDot = host.IndexOf('.');
        return firstDot > 0 ? host[..firstDot] : host;
    }

    private static bool IsPlatformRequest(PathString path) =>
        path.StartsWithSegments("/api/platform", StringComparison.OrdinalIgnoreCase)
        || path.StartsWithSegments("/platform", StringComparison.OrdinalIgnoreCase);
}
