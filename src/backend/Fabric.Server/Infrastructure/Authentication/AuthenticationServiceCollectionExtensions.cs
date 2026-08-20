using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Fabric.Server.Infrastructure.Tenancy;

namespace Fabric.Server.Infrastructure.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    private const string FabricBearerScheme = "FabricBearer";

    public static IServiceCollection AddFabricAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<ITenantOidcConfigurationStore, TenantOidcConfigurationStore>();
        services.AddSingleton<IPlatformOidcConfigurationStore, PlatformOidcConfigurationStore>();
        services.AddTransient<IClaimsTransformation, FabricClaimsTransformer>();

        services.AddAuthentication(FabricBearerScheme)
            .AddPolicyScheme(FabricBearerScheme, "Fabric bearer selector", options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Path.StartsWithSegments("/api/platform", StringComparison.OrdinalIgnoreCase)
                        ? PlatformBearerAuthenticationDefaults.AuthenticationScheme
                        : TenantBearerAuthenticationDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TenantBearerAuthenticationHandler>(
                TenantBearerAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, PlatformBearerAuthenticationHandler>(
                PlatformBearerAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, ReceptionKioskAuthenticationHandler>(
                ReceptionKioskAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, ReceptionDeskWorkstationAuthenticationHandler>(
                ReceptionDeskWorkstationAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, KioskAuthenticationHandler>(
                KioskAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, HardwareAgentAuthenticationHandler>(
                HardwareAgentAuthenticationDefaults.AuthenticationScheme,
                _ => { });

        var requireAuthPolicy = new AuthorizationPolicyBuilder(FabricBearerScheme)
            .RequireAuthenticatedUser()
            .Build();

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(requireAuthPolicy)
            .SetFallbackPolicy(requireAuthPolicy)
            .AddPolicy(FabricRoleDefaults.AdminPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.AdminRole);
            })
            .AddPolicy(FabricRoleDefaults.PlatformAdminPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(PlatformBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.PlatformAdminRole);
            })
            .AddPolicy(FabricRoleDefaults.HostPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.HostRole);
            })
            .AddPolicy(FabricRoleDefaults.ManagerPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.ManagerRole);
            })
            .AddPolicy(FabricRoleDefaults.SecurityOfficerPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.SecurityOfficerRole);
            })
            .AddPolicy(FabricRoleDefaults.AdminOrSecurityOfficerPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.AdminRole, FabricRoleDefaults.SecurityOfficerRole);
            })
            .AddPolicy(FabricRoleDefaults.ContractorEnrollmentPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.ContractorEnrollmentRole);
            })
            .AddPolicy(FabricRoleDefaults.ContractorPlanningPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.ContractorPlanningRole);
            })
            .AddPolicy(FabricRoleDefaults.ContractorEnrollmentOrPlanningPolicy, policy =>
            {
                policy.AuthenticationSchemes.Add(TenantBearerAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(FabricRoleDefaults.ContractorEnrollmentRole, FabricRoleDefaults.ContractorPlanningRole);
            })
            .AddPolicy(ReceptionKioskAuthenticationDefaults.Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(ReceptionKioskAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(ReceptionKioskAuthenticationDefaults.Role);
            })
            .AddPolicy(ReceptionDeskWorkstationAuthenticationDefaults.Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(ReceptionDeskWorkstationAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(ReceptionDeskWorkstationAuthenticationDefaults.Role);
            })
            .AddPolicy(KioskAuthenticationDefaults.Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(KioskAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(KioskAuthenticationDefaults.Role);
            })
            .AddPolicy(HardwareAgentAuthenticationDefaults.Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(HardwareAgentAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(HardwareAgentAuthenticationDefaults.Role);
            });

        return services;
    }
}
