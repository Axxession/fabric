using System.Security.Claims;

namespace Fabric.Server.Infrastructure.Storage;

public sealed record StorageClaimSnapshot(string? Oid, string? Email, string? DisplayName)
{
    public static StorageClaimSnapshot FromPrincipal(ClaimsPrincipal principal)
    {
        string? oid = ReadClaim(principal, "oid");
        string? email = ReadClaim(principal, ClaimTypes.Email) ?? ReadClaim(principal, "email") ?? ReadClaim(principal, "preferred_username");
        string? displayName = ReadClaim(principal, ClaimTypes.Name) ?? ReadClaim(principal, "name") ?? principal.Identity?.Name;
        return new StorageClaimSnapshot(Normalize(oid), Normalize(email), Normalize(displayName));
    }

    private static string? ReadClaim(ClaimsPrincipal principal, string claimType) => principal.FindFirstValue(claimType);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
