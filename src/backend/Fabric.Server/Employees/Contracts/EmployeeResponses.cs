using Fabric.Server.Employees.Application;
using Fabric.Server.Employees.Domain;

namespace Fabric.Server.Employees.Contracts;

public sealed record OrganizationUnitSummaryResponse(Guid Id, string Name, string? Code, string Type, Guid? ParentId, bool IsActive);

public sealed record PersonaSummaryResponse(Guid Id, string Name, bool IsActive);

public sealed record EmployeeWorkLocationResponse(Guid LocationId, bool IsPrimary);

public sealed record EmployeeLeavePeriodResponse(Guid Id, DateOnly From, DateOnly Until, string? Reason);

public sealed record EmployeeSuspensionPeriodResponse(Guid Id, DateOnly From, DateOnly Until, string? Reason);

public sealed record EmployeeResponse(
    Guid Id,
    Guid IdentityId,
    string FirstName,
    string LastName,
    DateOnly? BirthDate,
    string? EmployeeNumber,
    string? DirectoryId,
    string? Email,
    OrganizationUnitSummaryResponse OrganizationUnit,
    Guid? ManagerEmployeeId,
    string? JobTitle,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate,
    DateTimeOffset? ArchivedAt,
    EmployeeStatus Status,
    bool HasActiveLeave,
    bool HasActiveSuspension,
    IReadOnlyList<PersonaSummaryResponse> Personas,
    IReadOnlyList<EmployeeWorkLocationResponse> WorkLocations,
    IReadOnlyList<EmployeeLeavePeriodResponse> LeavePeriods,
    IReadOnlyList<EmployeeSuspensionPeriodResponse> SuspensionPeriods,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrganizationUnitResponse(
    Guid Id,
    string Name,
    string? Code,
    string Type,
    Guid? ParentId,
    bool IsActive,
    int? Depth,
    int? ChildCount,
    int? EmployeeCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PersonaResponse(Guid Id, string Name, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public static class EmployeeMapper
{
    public static OrganizationUnitSummaryResponse ToSummary(this OrganizationUnit unit) =>
        new(unit.Id, unit.Name, unit.Code, unit.Type, unit.ParentId, unit.IsActive);

    public static OrganizationUnitResponse ToResponse(this OrganizationUnit unit, int? depth = null, int? childCount = null, int? employeeCount = null) =>
        new(unit.Id, unit.Name, unit.Code, unit.Type, unit.ParentId, unit.IsActive, depth, childCount, employeeCount, unit.CreatedAt, unit.UpdatedAt);

    public static PersonaSummaryResponse ToSummary(this Persona persona) =>
        new(persona.Id, persona.Name, persona.IsActive);

    public static PersonaResponse ToResponse(this Persona persona) =>
        new(persona.Id, persona.Name, persona.IsActive, persona.CreatedAt, persona.UpdatedAt);

    public static EmployeeLeavePeriodResponse ToResponse(this EmployeeLeavePeriod period) =>
        new(period.Id, period.From, period.Until, period.Reason);

    public static EmployeeSuspensionPeriodResponse ToResponse(this EmployeeSuspensionPeriod period) =>
        new(period.Id, period.From, period.Until, period.Reason);

    public static EmployeeResponse ToResponse(
        this Employee employee,
        OrganizationUnit organizationUnit,
        IReadOnlyDictionary<Guid, Persona> personas,
        DateOnly today) =>
        new(
            employee.Id,
            employee.IdentityId,
            employee.FirstName,
            employee.LastName,
            employee.BirthDate,
            employee.EmployeeNumber,
            employee.DirectoryId,
            employee.Email,
            organizationUnit.ToSummary(),
            employee.ManagerEmployeeId,
            employee.JobTitle,
            employee.ContractStartDate,
            employee.ContractEndDate,
            employee.ArchivedAt,
            EmployeeLifecycleCalculator.Calculate(employee, today),
            employee.LeavePeriods.Any(period => EmployeeLifecycleCalculator.IsActive(period.From, period.Until, today)),
            employee.SuspensionPeriods.Any(period => EmployeeLifecycleCalculator.IsActive(period.From, period.Until, today)),
            employee.Personas
                .Select(link => personas.GetValueOrDefault(link.PersonaId))
                .Where(persona => persona is not null)
                .Select(persona => persona!.ToSummary())
                .OrderBy(persona => persona.Name)
                .ToArray(),
            employee.WorkLocations
                .OrderByDescending(location => location.IsPrimary)
                .ThenBy(location => location.LocationId)
                .Select(location => new EmployeeWorkLocationResponse(location.LocationId, location.IsPrimary))
                .ToArray(),
            employee.LeavePeriods
                .OrderBy(period => period.From)
                .Select(period => period.ToResponse())
                .ToArray(),
            employee.SuspensionPeriods
                .OrderBy(period => period.From)
                .Select(period => period.ToResponse())
                .ToArray(),
            employee.CreatedAt,
            employee.UpdatedAt);
}
