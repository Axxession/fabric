using System.Security.Claims;
using Fabric.Server.Actors.Contracts;
using Fabric.Server.Core;
using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Infrastructure.Authentication;
using Fabric.Server.Visitors.Application;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Actors.Application;

public sealed class CurrentActorService(
    EmployeesDbContext employeesDb,
    IdentitiesDbContext identitiesDb,
    HostService hostService,
    ILogger<CurrentActorService> logger)
{
    public async Task<Result<CurrentActorResponse, ActorErrors>> GetCurrentActorAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        string? objectId = principal.GetObjectId();
        string? subjectId = principal.GetSubjectId();
        string? email = principal.GetEmail();
        string[] roles = principal.GetActorRoles();

        Employee[] candidates = await GetCandidateEmployeesAsync(objectId, subjectId, email, cancellationToken);
        Result<Employee?, ActorErrors> employeeResolution = ResolveEmployee(candidates, objectId, subjectId, email);
        if (employeeResolution.IsFailure(out ActorErrors error))
            return Result.Failure<CurrentActorResponse, ActorErrors>(error);

        employeeResolution.IsSuccess(out Employee? employee);

        bool isManager = employee is not null
            && await employeesDb.Employees.AsNoTracking().AnyAsync(item => item.ManagerEmployeeId == employee.Id, cancellationToken);
        bool isHost = employee is not null && await hostService.IsEmployeeHostAsync(employee.Id, cancellationToken);

        Identity? identity = employee is null
            ? null
            : await identitiesDb.Identities.AsNoTracking().SingleOrDefaultAsync(item => item.Id == employee.IdentityId, cancellationToken);

        string[] hostRoles = isHost ? [FabricRoleDefaults.HostRole] : Array.Empty<string>();
        string[] managerRoles = isManager ? [FabricRoleDefaults.ManagerRole] : Array.Empty<string>();

        string[] effectiveRoles = roles
            .Concat(hostRoles)
            .Concat(managerRoles)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        return Result.Success<CurrentActorResponse, ActorErrors>(new CurrentActorResponse(
            identity?.Id ?? employee?.IdentityId,
            employee?.Id,
            employee?.OrganizationUnitId,
            employee?.ManagerEmployeeId,
            identity?.DisplayName ?? principal.GetDisplayName() ?? BuildDisplayName(employee?.FirstName, employee?.LastName, principal.GetEmail()),
            identity?.FirstName ?? employee?.FirstName ?? principal.GetFirstName(),
            identity?.LastName ?? employee?.LastName ?? principal.GetLastName(),
            identity?.Email ?? employee?.Email ?? email,
            employee?.DirectoryId,
            employee is not null,
            isHost,
            isManager,
            effectiveRoles.Contains(FabricRoleDefaults.AdminRole, StringComparer.Ordinal),
            effectiveRoles.Contains(FabricRoleDefaults.SecurityOfficerRole, StringComparer.Ordinal),
            effectiveRoles));
    }

    private async Task<Employee[]> GetCandidateEmployeesAsync(
        string? objectId,
        string? subjectId,
        string? email,
        CancellationToken cancellationToken)
    {
        if (objectId is null && subjectId is null && email is null)
            return [];

        bool hasObjectId = objectId is not null;
        bool hasSubjectId = subjectId is not null;
        bool hasEmail = email is not null;

        return await employeesDb.Employees
            .AsNoTracking()
            .Where(item =>
                hasObjectId && item.DirectoryId == objectId
                || hasSubjectId && item.DirectoryId == subjectId
                || hasEmail && item.Email == email)
            .ToArrayAsync(cancellationToken);
    }

    private Result<Employee?, ActorErrors> ResolveEmployee(Employee[] candidates, string? objectId, string? subjectId, string? email)
    {
        Employee[] objectIdMatches = objectId is null ? [] : candidates.Where(item => item.DirectoryId == objectId).ToArray();
        Result<Employee?, ActorErrors> objectIdResolution = ResolveSingleMatch(objectIdMatches, "oid", objectId);
        if (objectIdResolution.IsFailure(out ActorErrors objectIdError))
            return Result.Failure<Employee?, ActorErrors>(objectIdError);

        objectIdResolution.IsSuccess(out Employee? objectIdMatch);
        if (objectIdMatch is not null)
            return Result.Success<Employee?, ActorErrors>(objectIdMatch);

        Employee[] subjectIdMatches = subjectId is null ? [] : candidates.Where(item => item.DirectoryId == subjectId).ToArray();
        Result<Employee?, ActorErrors> subjectIdResolution = ResolveSingleMatch(subjectIdMatches, "sub", subjectId);
        if (subjectIdResolution.IsFailure(out ActorErrors subjectIdError))
            return Result.Failure<Employee?, ActorErrors>(subjectIdError);

        subjectIdResolution.IsSuccess(out Employee? subjectIdMatch);
        if (subjectIdMatch is not null)
            return Result.Success<Employee?, ActorErrors>(subjectIdMatch);

        Employee[] emailMatches = email is null ? [] : candidates.Where(item => item.Email == email).ToArray();
        return ResolveSingleMatch(emailMatches, "email", email);
    }

    private Result<Employee?, ActorErrors> ResolveSingleMatch(Employee[] matches, string claimType, string? claimValue)
    {
        if (matches.Length == 0)
            return Result.Success<Employee?, ActorErrors>(null);

        if (matches.Length == 1)
            return Result.Success<Employee?, ActorErrors>(matches[0]);

        logger.AmbiguousEmployeeMatch(claimType, claimValue, matches.Length);
        return Result.Failure<Employee?, ActorErrors>(ActorErrors.AmbiguousEmployeeMatch);
    }

    private static string? BuildDisplayName(string? firstName, string? lastName, string? email)
    {
        if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
            return $"{firstName} {lastName}";

        return email;
    }
}

internal static partial class CurrentActorLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Ambiguous employee match for actor claim {ClaimType}={ClaimValue}. Matched {MatchCount} employees.")]
    public static partial void AmbiguousEmployeeMatch(this ILogger logger, string claimType, string? claimValue, int matchCount);
}
