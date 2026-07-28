using System.Security.Claims;
using Fabric.Server.Actors.Application;
using Fabric.Server.Actors.Contracts;
using Fabric.Server.Core;
using Microsoft.AspNetCore.Authentication;

namespace Fabric.Server.Infrastructure.Authentication;

public sealed class FabricClaimsTransformer(CurrentActorService currentActorService) : IClaimsTransformation
{
    private const string RoleClaim = "role";
    private const string PermissionsClaim = "permissions";
    private const string PermissionsValue = "*";
    private const string TransformationMarkerClaim = "fabric_claims_transformed";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        if (principal.HasClaim(TransformationMarkerClaim, "true"))
            return principal;

        identity.AddClaim(new Claim(TransformationMarkerClaim, "true"));
        AddClaimIfMissing(identity, PermissionsClaim, PermissionsValue);

        Result<CurrentActorResponse, ActorErrors> result = await currentActorService.GetCurrentActorAsync(principal);
        if (result.IsFailure(out _))
            return principal;

        result.IsSuccess(out CurrentActorResponse? actor);
        if (actor?.EmployeeId is not Guid employeeId)
            return principal;

        AddClaimIfMissing(identity, FabricActorClaimsPrincipalExtensions.EmployeeIdClaim, employeeId.ToString());

        foreach (string role in actor.Roles)
        {
            AddClaimIfMissing(identity, RoleClaim, role);
        }

        return principal;
    }

    private static void AddClaimIfMissing(ClaimsIdentity identity, string claimType, string claimValue)
    {
        if (identity.HasClaim(claimType, claimValue))
            return;

        identity.AddClaim(new Claim(claimType, claimValue));
    }
}
