using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Infrastructure;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Tenants.Contracts;
using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Tenants.Endpoints;

public static class TenantsEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tenants/settings", GetTenantSettings)
            .AllowAnonymous()
            .WithDescription("Retrieve tenant settings")
            .WithSummary("Retrieve tenant settings")
            .Produces<TenantSettingsResponse>();

        app.MapGet("/api/tenants/admin/settings", GetAdminTenantSettings)
            .WithDescription("Retrieve editable tenant settings")
            .WithSummary("Retrieve editable tenant settings")
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.IntegratorRole })
            .Produces<AdminTenantSettingsResponse>();

        app.MapPut("/api/tenants/admin/settings", UpdateAdminTenantSettings)
            .WithDescription("Update editable tenant settings")
            .WithSummary("Update editable tenant settings")
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.IntegratorRole })
            .Produces<AdminTenantSettingsResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        return app;
    }

    private static IResult GetTenantSettings(ITenantContext tenantContext, IApplicationVersionProvider versionProvider) =>
        Results.Ok(tenantContext.Configuration.ToResponse(versionProvider.GetVersion()));

    private static IResult GetAdminTenantSettings(ITenantContext tenantContext, IApplicationVersionProvider versionProvider) =>
        Results.Ok(tenantContext.Configuration.ToAdminResponse(versionProvider.GetVersion()));

    private static async Task<IResult> UpdateAdminTenantSettings(
        [FromBody] UpdateTenantSettingsRequest request,
        ITenantContext tenantContext,
        IApplicationVersionProvider versionProvider,
        ITenantStore tenantStore,
        TenantsDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        IResult? validationResult = ValidateRequest(request);
        if (validationResult is not null)
            return validationResult;

        Tenant? tenant = await dbContext.Tenants.SingleOrDefaultAsync(t => t.Id == tenantContext.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Tenant not found",
                detail: $"Tenant '{tenantContext.TenantId}' does not exist.");
        }

        TenantConfiguration configuration = tenant.Configuration with
        {
            Oidc = new OidcSettings
            {
                MetadataUrl = request.Oidc.MetadataUrl.Trim(),
                ClientId = request.Oidc.ClientId.Trim(),
                RequireHttpsMetadata = request.Oidc.RequireHttpsMetadata
            }
        };

        tenant.UpdateConfiguration(configuration, timeProvider.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        tenantStore.InvalidateTenant(tenant.Id);

        return Results.Ok(configuration.ToAdminResponse(versionProvider.GetVersion()));
    }

    private static IResult? ValidateRequest(UpdateTenantSettingsRequest request)
    {
        if (request.Oidc is null)
            return ValidationProblem("OIDC settings are required.");

        if (string.IsNullOrWhiteSpace(request.Oidc.MetadataUrl) || !Uri.TryCreate(request.Oidc.MetadataUrl, UriKind.Absolute, out Uri? metadataUrl))
            return ValidationProblem("OIDC metadata URL must be an absolute URL.");

        if (request.Oidc.RequireHttpsMetadata && metadataUrl.Scheme != Uri.UriSchemeHttps)
            return ValidationProblem("OIDC metadata URL must use HTTPS when HTTPS metadata is required.");

        if (string.IsNullOrWhiteSpace(request.Oidc.ClientId))
            return ValidationProblem("OIDC client ID is required.");

        return null;
    }

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid tenant settings", detail: detail);
}
