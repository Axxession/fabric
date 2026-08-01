using Fabric.Server.Core;

namespace Fabric.Server.Employees.Domain;

public sealed class Persona
{
    private Persona() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Persona, EmployeeErrors> Create(string name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Persona, EmployeeErrors>(EmployeeErrors.PersonaNameRequired);

        return Result.Success<Persona, EmployeeErrors>(new Persona
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<EmployeeErrors> Update(string name, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(EmployeeErrors.PersonaNameRequired);

        Name = name.Trim();
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
}
