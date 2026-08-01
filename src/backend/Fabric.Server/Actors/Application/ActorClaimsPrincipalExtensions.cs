using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Fabric.Server.Actors.Application;

internal static class ActorClaimsPrincipalExtensions
{
    private const string ObjectIdClaim = "oid";
    private const string RoleClaim = "role";
    private const string GivenNameClaim = "given_name";
    private const string FamilyNameClaim = "family_name";

    public static string? GetObjectId(this ClaimsPrincipal principal) => Normalize(principal.FindFirstValue(ObjectIdClaim));

    public static string? GetSubjectId(this ClaimsPrincipal principal) => Normalize(principal.FindFirstValue(JwtRegisteredClaimNames.Sub));

    public static string? GetEmail(this ClaimsPrincipal principal) => Normalize(principal.FindFirstValue(JwtRegisteredClaimNames.Email));

    public static string? GetDisplayName(this ClaimsPrincipal principal) => Normalize(principal.Identity?.Name);

    public static string? GetFirstName(this ClaimsPrincipal principal) => Normalize(principal.FindFirstValue(GivenNameClaim));

    public static string? GetLastName(this ClaimsPrincipal principal) => Normalize(principal.FindFirstValue(FamilyNameClaim));

    public static string[] GetActorRoles(this ClaimsPrincipal principal) => principal.FindAll(RoleClaim)
        .Select(claim => NormalizeRole(claim.Value))
        .Where(value => value is not null)
        .Select(value => value!)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeRole(string? value)
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
}
