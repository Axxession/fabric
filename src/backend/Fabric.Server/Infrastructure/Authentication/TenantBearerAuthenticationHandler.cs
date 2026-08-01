using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Fabric.Server.Infrastructure.Authentication;

public sealed class TenantBearerAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITenantContext tenantContext,
    ITenantOidcConfigurationStore configurationStore)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string BearerPrefix = "Bearer ";
    private const string RoleClaim = "role";
    private const string RolesClaim = "roles";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string authorizationHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return AuthenticateResult.NoResult();

        if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Authorization header must use the Bearer scheme.");

        string token = authorizationHeader[BearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return AuthenticateResult.Fail("Bearer token is required.");

        OidcSettings oidcSettings = tenantContext.Configuration.Oidc;
        TokenValidationResult validationResult = await ValidateTokenAsync(token, oidcSettings, refreshMetadata: false);

        if (!validationResult.IsValid && validationResult.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            configurationStore.RequestRefresh(tenantContext.TenantId, oidcSettings);
            validationResult = await ValidateTokenAsync(token, oidcSettings, refreshMetadata: true);
        }

        if (!validationResult.IsValid)
            return AuthenticateResult.Fail(validationResult.Exception ?? new SecurityTokenValidationException("Bearer token is invalid."));

        if (validationResult.ClaimsIdentity is null)
            return AuthenticateResult.Fail("Bearer token did not produce an identity.");

        ClaimsIdentity normalizedIdentity = NormalizeRoleClaims(validationResult.ClaimsIdentity);
        var principal = new ClaimsPrincipal(normalizedIdentity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        return base.HandleChallengeAsync(properties);
    }

    private async Task<TokenValidationResult> ValidateTokenAsync(
        string token,
        OidcSettings oidcSettings,
        bool refreshMetadata)
    {
        OpenIdConnectConfiguration configuration = await configurationStore.GetConfigurationAsync(
            tenantContext.TenantId,
            oidcSettings,
            Context.RequestAborted);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration.Issuer,
            ValidateAudience = false, //TODO: Look into why we should validate this and should we make it a seperate config what the audince is?
            ValidAudience = oidcSettings.ClientId,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = "role"
        };

        var tokenHandler = new JsonWebTokenHandler
        {
            MapInboundClaims = false
        };

        return await tokenHandler.ValidateTokenAsync(token, validationParameters);
    }

    private static ClaimsIdentity NormalizeRoleClaims(ClaimsIdentity identity)
    {
        Claim[] claims = identity.Claims.ToArray();
        var normalizedIdentity = new ClaimsIdentity(
            claims.Where(claim => !IsRoleClaim(claim.Type)),
            identity.AuthenticationType,
            identity.NameClaimType,
            RoleClaim);

        foreach (string role in claims
                     .Where(claim => IsRoleClaim(claim.Type))
                     .SelectMany(claim => ExpandRoleClaimValues(claim.Value))
                     .Distinct(StringComparer.Ordinal))
        {
            normalizedIdentity.AddClaim(new Claim(RoleClaim, role));
        }

        return normalizedIdentity;
    }

    private static bool IsRoleClaim(string claimType) =>
        string.Equals(claimType, RoleClaim, StringComparison.OrdinalIgnoreCase)
        || string.Equals(claimType, RolesClaim, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> ExpandRoleClaimValues(string claimValue)
    {
        string? trimmedValue = Normalize(claimValue);
        if (trimmedValue is null)
            return [];

        if (!trimmedValue.StartsWith("[", StringComparison.Ordinal))
        {
            string? normalizedScalarValue = NormalizeRoleValue(trimmedValue);
            return normalizedScalarValue is null ? [] : [normalizedScalarValue];
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(trimmedValue);
            if (document.RootElement.ValueKind is not JsonValueKind.Array)
                return [];

            return document.RootElement
                .EnumerateArray()
                .Where(element => element.ValueKind is JsonValueKind.String)
                .Select(element => NormalizeRoleValue(element.GetString()))
                .Where(value => value is not null)
                .Select(value => value!)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? NormalizeRoleValue(string? value)
    {
        string? trimmedValue = Normalize(value);
        if (trimmedValue is null)
            return null;

        var builder = new StringBuilder(trimmedValue.Length);
        bool previousWasSeparator = false;

        for (int index = 0; index < trimmedValue.Length; index++)
        {
            char character = trimmedValue[index];
            if (char.IsLetterOrDigit(character))
            {
                if (char.IsUpper(character)
                    && builder.Length > 0
                    && !previousWasSeparator
                    && (char.IsLower(trimmedValue[index - 1]) || index + 1 < trimmedValue.Length && char.IsLower(trimmedValue[index + 1])))
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (builder.Length > 0 && !previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        if (builder.Length > 0 && builder[^1] is '-')
            builder.Length--;

        return Normalize(builder.ToString());
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
