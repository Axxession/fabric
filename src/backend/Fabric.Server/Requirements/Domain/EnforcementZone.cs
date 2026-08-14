using Fabric.Server.Core;

namespace Fabric.Server.Requirements.Domain;

public sealed class EnforcementZone
{
    private EnforcementZone() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<EnforcementZone, EnforcementZoneErrors> Create(
        string code,
        string name,
        string? description,
        DateTimeOffset now)
    {
        Result<EnforcementZoneErrors> validation = Validate(code, name);
        if (validation.IsFailure(out EnforcementZoneErrors error))
            return Result.Failure<EnforcementZone, EnforcementZoneErrors>(error);

        return Result.Success<EnforcementZone, EnforcementZoneErrors>(new EnforcementZone
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Description = NormalizeOptional(description),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public Result<EnforcementZoneErrors> Update(string code, string name, string? description, DateTimeOffset now)
    {
        Result<EnforcementZoneErrors> validation = Validate(code, name);
        if (validation.IsFailure(out EnforcementZoneErrors error))
            return Result.Failure(error);

        Code = code.Trim();
        Name = name.Trim();
        Description = NormalizeOptional(description);
        UpdatedAt = now;
        return Result.Success<EnforcementZoneErrors>();
    }

    public Result<EnforcementZoneErrors> Activate(DateTimeOffset now)
    {
        if (IsActive)
            return Result.Failure(EnforcementZoneErrors.EnforcementZoneAlreadyActive);

        IsActive = true;
        UpdatedAt = now;
        return Result.Success<EnforcementZoneErrors>();
    }

    public Result<EnforcementZoneErrors> Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
            return Result.Failure(EnforcementZoneErrors.EnforcementZoneAlreadyInactive);

        IsActive = false;
        UpdatedAt = now;
        return Result.Success<EnforcementZoneErrors>();
    }

    private static Result<EnforcementZoneErrors> Validate(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(EnforcementZoneErrors.CodeRequired);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(EnforcementZoneErrors.NameRequired);

        return Result.Success<EnforcementZoneErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
