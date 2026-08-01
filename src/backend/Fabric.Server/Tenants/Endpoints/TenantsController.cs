using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Infrastructure;
using Fabric.Server.Notifications;
using Fabric.Server.Tenants.Contracts;
using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Fabric.Server.Tenants.Endpoints;

public static class TenantsEndpoints
{
    private static readonly Regex HexColorRegex = new("^#(?:[0-9a-fA-F]{3}){1,2}$", RegexOptions.Compiled);

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
            .Produces<AdminTenantSettingsResponse>();

        app.MapPut("/api/tenants/admin/settings", UpdateAdminTenantSettings)
            .WithDescription("Update editable tenant settings")
            .WithSummary("Update editable tenant settings")
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
        CancellationToken cancellationToken = default)
    {
        IResult? validationResult = ValidateRequest(request, tenantContext.Configuration.GraphEmail);
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
            },
            Theme = new ThemeSettings
            {
                BackgroundColor = request.Theme.BackgroundColor.Trim(),
                ContentColor = request.Theme.ContentColor.Trim(),
                PrimaryColor = request.Theme.PrimaryColor.Trim(),
                TextColor = request.Theme.TextColor.Trim(),
                TextMutedColor = request.Theme.TextMutedColor.Trim(),
                BorderColor = request.Theme.BorderColor.Trim(),
                HoverBlueColor = request.Theme.HoverBlueColor.Trim(),
                ActiveBlueColor = request.Theme.ActiveBlueColor.Trim(),
                HoverGrayColor = request.Theme.HoverGrayColor.Trim(),
                ErrorColor = request.Theme.ErrorColor.Trim(),
                ErrorBackgroundColor = request.Theme.ErrorBackgroundColor.Trim(),
                DangerColor = request.Theme.DangerColor.Trim(),
                SuccessColor = request.Theme.SuccessColor.Trim(),
                SuccessBackgroundColor = request.Theme.SuccessBackgroundColor.Trim()
            },
            GraphEmail = ToGraphEmailSettings(request.Email, tenant.Configuration.GraphEmail)
        };

        tenant.UpdateConfiguration(configuration);
        await dbContext.SaveChangesAsync(cancellationToken);
        tenantStore.InvalidateTenant(tenant.Id);

        return Results.Ok(configuration.ToAdminResponse(versionProvider.GetVersion()));
    }

    private static GraphEmailSettings? ToGraphEmailSettings(UpdateGraphEmailSettingsRequest? request, GraphEmailSettings? current)
    {
        if (request is null)
            return null;

        return new GraphEmailSettings
        {
            FromEmail = request.FromEmail.Trim(),
            FromName = request.FromName.Trim(),
            AzureTenantId = request.AzureTenantId.Trim(),
            ApplicationId = request.ApplicationId.Trim(),
            Secret = string.IsNullOrWhiteSpace(request.Secret) ? current?.Secret ?? string.Empty : request.Secret.Trim(),
            SaveSentItems = request.SaveSentItems
        };
    }

    private static IResult? ValidateRequest(UpdateTenantSettingsRequest request, GraphEmailSettings? currentEmail)
    {
        if (request.Oidc is null)
            return ValidationProblem("OIDC settings are required.");

        if (string.IsNullOrWhiteSpace(request.Oidc.MetadataUrl) || !Uri.TryCreate(request.Oidc.MetadataUrl, UriKind.Absolute, out Uri? metadataUrl))
            return ValidationProblem("OIDC metadata URL must be an absolute URL.");

        if (request.Oidc.RequireHttpsMetadata && metadataUrl.Scheme != Uri.UriSchemeHttps)
            return ValidationProblem("OIDC metadata URL must use HTTPS when HTTPS metadata is required.");

        if (string.IsNullOrWhiteSpace(request.Oidc.ClientId))
            return ValidationProblem("OIDC client ID is required.");

        if (request.Theme is null)
            return ValidationProblem("Theme settings are required.");

        string[] colors =
        [
            request.Theme.BackgroundColor,
            request.Theme.ContentColor,
            request.Theme.PrimaryColor,
            request.Theme.TextColor,
            request.Theme.TextMutedColor,
            request.Theme.BorderColor,
            request.Theme.HoverBlueColor,
            request.Theme.ActiveBlueColor,
            request.Theme.HoverGrayColor,
            request.Theme.ErrorColor,
            request.Theme.ErrorBackgroundColor,
            request.Theme.DangerColor,
            request.Theme.SuccessColor,
            request.Theme.SuccessBackgroundColor
        ];

        if (colors.Any(color => string.IsNullOrWhiteSpace(color) || !HexColorRegex.IsMatch(color.Trim())))
            return ValidationProblem("Theme colors must be hex colors.");

        if (request.Email is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.Email.FromEmail)
            || string.IsNullOrWhiteSpace(request.Email.FromName)
            || string.IsNullOrWhiteSpace(request.Email.AzureTenantId)
            || string.IsNullOrWhiteSpace(request.Email.ApplicationId))
        {
            return ValidationProblem("Email settings must include sender email, sender name, Azure tenant ID and application ID.");
        }

        if (string.IsNullOrWhiteSpace(request.Email.Secret) && string.IsNullOrWhiteSpace(currentEmail?.Secret))
            return ValidationProblem("Email settings require a secret when first configured.");

        return null;
    }

    private static IResult ValidationProblem(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid tenant settings", detail: detail);
}
