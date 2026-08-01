using Fabric.Server.Core;

namespace Fabric.Server.Identities.Domain;

public sealed class Identity
{
    private Identity() { }

    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; } = null!;
    public string? PreferredName { get; private set; }
    public string DisplayName { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public IdentityStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public List<EmployeeAffiliation> EmployeeAffiliations { get; private set; } = [];
    public List<ContractorAffiliation> ContractorAffiliations { get; private set; } = [];
    public List<VisitorAffiliation> VisitorAffiliations { get; private set; } = [];

    public static Result<Identity, IdentityErrors> Create(
        string firstName,
        string? middleName,
        string lastName,
        string? preferredName,
        string? email,
        string? phone,
        DateTimeOffset now)
    {
        Result<IdentityErrors> validation = ValidateNames(firstName, lastName);
        if (validation.IsFailure(out IdentityErrors error))
            return Result.Failure<Identity, IdentityErrors>(error);

        return Result.Success<Identity, IdentityErrors>(new Identity
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            MiddleName = NormalizeOptional(middleName),
            LastName = lastName.Trim(),
            PreferredName = NormalizeOptional(preferredName),
            DisplayName = BuildDisplayName(firstName, preferredName, lastName),
            Email = NormalizeOptional(email),
            Phone = NormalizeOptional(phone),
            Status = IdentityStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<IdentityErrors> UpdateProfile(
        string firstName,
        string? middleName,
        string lastName,
        string? preferredName,
        string? email,
        string? phone,
        DateTimeOffset updatedAt)
    {
        Result<IdentityErrors> validation = ValidateNames(firstName, lastName);
        if (validation.IsFailure(out IdentityErrors error))
            return Result.Failure(error);

        FirstName = firstName.Trim();
        MiddleName = NormalizeOptional(middleName);
        LastName = lastName.Trim();
        PreferredName = NormalizeOptional(preferredName);
        DisplayName = BuildDisplayName(firstName, preferredName, lastName);
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        UpdatedAt = updatedAt;
        return Result.Success<IdentityErrors>();
    }

    public Result<VisitorAffiliation, IdentityErrors> AddVisitorAffiliation(Guid visitorId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveUntil, DateTimeOffset updatedAt)
    {
        VisitorAffiliation? existing = VisitorAffiliations.SingleOrDefault(affiliation => affiliation.VisitorId == visitorId);
        if (existing is not null)
            return Result.Failure<VisitorAffiliation, IdentityErrors>(IdentityErrors.VisitorAffiliationAlreadyExists);

        Result<VisitorAffiliation, IdentityErrors> create = VisitorAffiliation.Create(visitorId, effectiveFrom, effectiveUntil);
        if (create.IsFailure(out IdentityErrors error))
            return Result.Failure<VisitorAffiliation, IdentityErrors>(error);

        create.IsSuccess(out VisitorAffiliation affiliation);
        VisitorAffiliations.Add(affiliation);
        UpdatedAt = updatedAt;
        return Result.Success<VisitorAffiliation, IdentityErrors>(affiliation);
    }

    public Result<EmployeeAffiliation, IdentityErrors> AddEmployeeAffiliation(Guid employeeId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveUntil, DateTimeOffset updatedAt)
    {
        EmployeeAffiliation? existing = EmployeeAffiliations.SingleOrDefault(affiliation => affiliation.EmployeeId == employeeId);
        if (existing is not null)
            return Result.Failure<EmployeeAffiliation, IdentityErrors>(IdentityErrors.EmployeeAffiliationAlreadyExists);

        Result<EmployeeAffiliation, IdentityErrors> create = EmployeeAffiliation.Create(employeeId, effectiveFrom, effectiveUntil);
        if (create.IsFailure(out IdentityErrors error))
            return Result.Failure<EmployeeAffiliation, IdentityErrors>(error);

        create.IsSuccess(out EmployeeAffiliation affiliation);
        EmployeeAffiliations.Add(affiliation);
        UpdatedAt = updatedAt;
        return Result.Success<EmployeeAffiliation, IdentityErrors>(affiliation);
    }

    public Result<ContractorAffiliation, IdentityErrors> AddContractorAffiliation(Guid contractorId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveUntil, DateTimeOffset updatedAt)
    {
        ContractorAffiliation? existing = ContractorAffiliations.SingleOrDefault(affiliation => affiliation.ContractorId == contractorId);
        if (existing is not null)
            return Result.Failure<ContractorAffiliation, IdentityErrors>(IdentityErrors.ContractorAffiliationAlreadyExists);

        Result<ContractorAffiliation, IdentityErrors> create = ContractorAffiliation.Create(contractorId, effectiveFrom, effectiveUntil);
        if (create.IsFailure(out IdentityErrors error))
            return Result.Failure<ContractorAffiliation, IdentityErrors>(error);

        create.IsSuccess(out ContractorAffiliation affiliation);
        ContractorAffiliations.Add(affiliation);
        UpdatedAt = updatedAt;
        return Result.Success<ContractorAffiliation, IdentityErrors>(affiliation);
    }

    public Result<IdentityErrors> EndVisitorAffiliation(Guid visitorId, DateTimeOffset effectiveUntil, DateTimeOffset updatedAt)
    {
        VisitorAffiliation? affiliation = VisitorAffiliations.SingleOrDefault(item => item.VisitorId == visitorId);
        if (affiliation is null)
            return Result.Failure(IdentityErrors.IdentityNotFound);

        Result<IdentityErrors> result = affiliation.End(effectiveUntil);
        if (result.IsSuccess(out _))
            UpdatedAt = updatedAt;

        return result;
    }

    private static Result<IdentityErrors> ValidateNames(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure(IdentityErrors.FirstNameRequired);

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure(IdentityErrors.LastNameRequired);

        return Result.Success<IdentityErrors>();
    }

    private static string BuildDisplayName(string firstName, string? preferredName, string lastName)
    {
        string givenName = string.IsNullOrWhiteSpace(preferredName) ? firstName.Trim() : preferredName.Trim();
        return $"{givenName} {lastName.Trim()}";
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
