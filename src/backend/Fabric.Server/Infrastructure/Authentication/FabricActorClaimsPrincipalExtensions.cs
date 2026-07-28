using System.Security.Claims;

namespace Fabric.Server.Infrastructure.Authentication;

public static class FabricActorClaimsPrincipalExtensions
{
    public const string EmployeeIdClaim = "employee_id";

    public static Guid? GetEmployeeId(this ClaimsPrincipal principal)
    {
        string? employeeId = principal.FindFirstValue(EmployeeIdClaim);
        return Guid.TryParse(employeeId, out Guid parsedEmployeeId) ? parsedEmployeeId : null;
    }
}
