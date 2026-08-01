using Fabric.Server.Core;

namespace Fabric.Server.Employees.Domain;

public sealed class OrganizationUnit
{
    private OrganizationUnit() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Code { get; private set; }
    public string Type { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<OrganizationUnit, EmployeeErrors> Create(
        string name,
        string? code,
        string type,
        Guid? parentId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitNotFound);

        return Result.Success<OrganizationUnit, EmployeeErrors>(new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Code = NormalizeOptional(code),
            Type = type.Trim(),
            ParentId = parentId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<EmployeeErrors> Update(string name, string? code, string type, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
            return Result.Failure(EmployeeErrors.OrganizationUnitNotFound);

        Name = name.Trim();
        Code = NormalizeOptional(code);
        Type = type.Trim();
        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeErrors> Move(Guid? parentId, DateTimeOffset now)
    {
        if (parentId == Id)
            return Result.Failure(EmployeeErrors.OrganizationUnitParentCycle);

        ParentId = parentId;
        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
