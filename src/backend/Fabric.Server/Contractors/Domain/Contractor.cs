using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Domain;

public sealed class Contractor
{
    private Contractor() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Email { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Contractor, ContractorErrors> Create(
        Guid companyId,
        string firstName,
        string lastName,
        string? email,
        DateTimeOffset now)
    {
        Result<ContractorErrors> validation = Validate(firstName, lastName);
        if (validation.IsFailure(out ContractorErrors error))
            return Result.Failure<Contractor, ContractorErrors>(error);

        return Result.Success<Contractor, ContractorErrors>(new Contractor
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = NormalizeOptional(email),
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<ContractorErrors> Update(Guid companyId, string firstName, string lastName, string? email, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(ContractorErrors.ContractorAlreadyArchived);

        Result<ContractorErrors> validation = Validate(firstName, lastName);
        if (validation.IsFailure(out ContractorErrors error))
            return Result.Failure(error);

        CompanyId = companyId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = NormalizeOptional(email);
        UpdatedAt = now;
        return Result.Success<ContractorErrors>();
    }

    public Result<ContractorErrors> Archive(DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(ContractorErrors.ContractorAlreadyArchived);

        ArchivedAt = now;
        UpdatedAt = now;
        return Result.Success<ContractorErrors>();
    }

    public Result<ContractorErrors> Unarchive(DateTimeOffset now)
    {
        if (!ArchivedAt.HasValue)
            return Result.Failure(ContractorErrors.ContractorNotArchived);

        ArchivedAt = null;
        UpdatedAt = now;
        return Result.Success<ContractorErrors>();
    }

    private static Result<ContractorErrors> Validate(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure(ContractorErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure(ContractorErrors.LastNameRequired);

        return Result.Success<ContractorErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
