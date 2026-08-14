using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Domain;

public sealed class Company
{
    private Company() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? CompanyNumber { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Company, CompanyErrors> Create(string code, string name, string? companyNumber, DateTimeOffset now)
    {
        Result<CompanyErrors> validation = Validate(code, name);
        if (validation.IsFailure(out CompanyErrors error))
            return Result.Failure<Company, CompanyErrors>(error);

        return Result.Success<Company, CompanyErrors>(new Company
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            CompanyNumber = NormalizeOptional(companyNumber),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<CompanyErrors> Update(string code, string name, string? companyNumber, DateTimeOffset now)
    {
        Result<CompanyErrors> validation = Validate(code, name);
        if (validation.IsFailure(out CompanyErrors error))
            return Result.Failure(error);

        Code = code.Trim();
        Name = name.Trim();
        CompanyNumber = NormalizeOptional(companyNumber);
        UpdatedAt = now;
        return Result.Success<CompanyErrors>();
    }

    public Result<CompanyErrors> Activate(DateTimeOffset now)
    {
        if (IsActive)
            return Result.Failure(CompanyErrors.CompanyAlreadyActive);

        IsActive = true;
        UpdatedAt = now;
        return Result.Success<CompanyErrors>();
    }

    public Result<CompanyErrors> Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
            return Result.Failure(CompanyErrors.CompanyAlreadyInactive);

        IsActive = false;
        UpdatedAt = now;
        return Result.Success<CompanyErrors>();
    }

    private static Result<CompanyErrors> Validate(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(CompanyErrors.CodeRequired);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(CompanyErrors.NameRequired);

        return Result.Success<CompanyErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
