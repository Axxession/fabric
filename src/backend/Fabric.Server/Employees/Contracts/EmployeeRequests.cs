using Fabric.Server.Core;
using Fabric.Server.Employees.Domain;

namespace Fabric.Server.Employees.Contracts;

public sealed record ListEmployeesRequest : BaseListRequest
{
    public string? Query { get; set; }
    public EmployeeStatus[]? Status { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public bool? IncludeIndirectReports { get; set; }
    public bool IncludeDescendants { get; set; }
}

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    DateOnly? BirthDate,
    string? EmployeeNumber,
    string? DirectoryId,
    string? Email,
    Guid OrganizationUnitId,
    Guid? ManagerEmployeeId,
    string? JobTitle,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate);

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    DateOnly? BirthDate,
    string? EmployeeNumber,
    string? DirectoryId,
    string? Email,
    Guid OrganizationUnitId,
    Guid? ManagerEmployeeId,
    string? JobTitle,
    DateOnly? ContractStartDate,
    DateOnly? ContractEndDate);

public sealed record EmployeeWorkLocationRequest(Guid LocationId, bool IsPrimary);

public sealed record ReplaceEmployeeWorkLocationsRequest(IReadOnlyList<EmployeeWorkLocationRequest> WorkLocations);

public sealed record ReplaceEmployeePersonasRequest(IReadOnlyList<Guid> PersonaIds);

public sealed record CreateEmployeePeriodRequest(DateOnly From, DateOnly Until, string? Reason);

public sealed record UpdateEmployeePeriodRequest(DateOnly From, DateOnly Until, string? Reason);
