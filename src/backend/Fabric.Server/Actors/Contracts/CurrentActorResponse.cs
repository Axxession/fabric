namespace Fabric.Server.Actors.Contracts;

public sealed record CurrentActorResponse(
    Guid? IdentityId,
    Guid? EmployeeId,
    Guid? OrganizationUnitId,
    Guid? ManagerEmployeeId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? DirectoryId,
    bool IsEmployee,
    bool IsHost,
    bool IsManager,
    bool IsAdmin,
    bool IsSecurityOfficer,
    IReadOnlyList<string> Roles);
