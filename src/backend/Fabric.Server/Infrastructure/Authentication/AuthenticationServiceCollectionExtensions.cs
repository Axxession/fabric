using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Fabric.Server.Infrastructure.Authentication;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddFabricAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<ITenantOidcConfigurationStore, TenantOidcConfigurationStore>();
        services.AddTransient<IClaimsTransformation, FabricClaimsTransformer>();

        services.AddAuthentication(TenantBearerAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, TenantBearerAuthenticationHandler>(
                TenantBearerAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, ReceptionKioskAuthenticationHandler>(
                ReceptionKioskAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, KioskAuthenticationHandler>(
                KioskAuthenticationDefaults.AuthenticationScheme,
                _ => { })
            .AddScheme<AuthenticationSchemeOptions, HardwareAgentAuthenticationHandler>(
                HardwareAgentAuthenticationDefaults.AuthenticationScheme,
                _ => { });

        var requireAuthPolicy = new AuthorizationPolicyBuilder(TenantBearerAuthenticationDefaults.AuthenticationScheme)
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
            .AddPolicy(ReceptionKioskAuthenticationDefaults.Policy, policy =>
            {
                policy.AuthenticationSchemes.Add(ReceptionKioskAuthenticationDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(ReceptionKioskAuthenticationDefaults.Role);
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
