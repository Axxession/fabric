using Fabric.Server.Core;

namespace Fabric.Server.Employees.Domain;

public sealed class Employee
{
    private readonly List<EmployeeLeavePeriod> _leavePeriods = [];
    private readonly List<EmployeePersona> _personas = [];
    private readonly List<EmployeeSuspensionPeriod> _suspensionPeriods = [];
    private readonly List<EmployeeWorkLocation> _workLocations = [];

    private Employee() { }

    public Guid Id { get; private set; }
    public Guid IdentityId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public DateOnly? BirthDate { get; private set; }
    public string? EmployeeNumber { get; private set; }
    public string? DirectoryId { get; private set; }
    public string? Email { get; private set; }
    public Guid OrganizationUnitId { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public string? JobTitle { get; private set; }
    public DateOnly? ContractStartDate { get; private set; }
    public DateOnly? ContractEndDate { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<EmployeeWorkLocation> WorkLocations => _workLocations;
    public IReadOnlyCollection<EmployeePersona> Personas => _personas;
    public IReadOnlyCollection<EmployeeLeavePeriod> LeavePeriods => _leavePeriods;
    public IReadOnlyCollection<EmployeeSuspensionPeriod> SuspensionPeriods => _suspensionPeriods;

    public static Result<Employee, EmployeeErrors> Create(
        Guid identityId,
        string firstName,
        string lastName,
        DateOnly? birthDate,
        string? employeeNumber,
        string? directoryId,
        string? email,
        Guid organizationUnitId,
        Guid? managerEmployeeId,
        string? jobTitle,
        DateOnly? contractStartDate,
        DateOnly? contractEndDate,
        DateTimeOffset now)
    {
        Result<EmployeeErrors> validation = Validate(
            firstName,
            lastName,
            managerEmployeeId,
            employeeId: null,
            contractStartDate,
            contractEndDate);
        if (validation.IsFailure(out EmployeeErrors error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        return Result.Success<Employee, EmployeeErrors>(new Employee
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            BirthDate = birthDate,
            EmployeeNumber = NormalizeOptional(employeeNumber),
            DirectoryId = NormalizeOptional(directoryId),
            Email = NormalizeOptional(email),
            OrganizationUnitId = organizationUnitId,
            ManagerEmployeeId = managerEmployeeId,
            JobTitle = NormalizeOptional(jobTitle),
            ContractStartDate = contractStartDate,
            ContractEndDate = contractEndDate,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<EmployeeErrors> Update(
        string firstName,
        string lastName,
        DateOnly? birthDate,
        string? employeeNumber,
        string? directoryId,
        string? email,
        Guid organizationUnitId,
        Guid? managerEmployeeId,
        string? jobTitle,
        DateOnly? contractStartDate,
        DateOnly? contractEndDate,
        DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        Result<EmployeeErrors> validation = Validate(
            firstName,
            lastName,
            managerEmployeeId,
            Id,
            contractStartDate,
            contractEndDate);
        if (validation.IsFailure(out EmployeeErrors error))
            return Result.Failure(error);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        BirthDate = birthDate;
        EmployeeNumber = NormalizeOptional(employeeNumber);
        DirectoryId = NormalizeOptional(directoryId);
        Email = NormalizeOptional(email);
        OrganizationUnitId = organizationUnitId;
        ManagerEmployeeId = managerEmployeeId;
        JobTitle = NormalizeOptional(jobTitle);
        ContractStartDate = contractStartDate;
        ContractEndDate = contractEndDate;
        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeErrors> ReplaceWorkLocations(IEnumerable<(Guid LocationId, bool IsPrimary)> workLocations, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        (Guid LocationId, bool IsPrimary)[] normalized = workLocations.DistinctBy(item => item.LocationId).ToArray();
        int primaryCount = normalized.Count(item => item.IsPrimary);

        if (normalized.Length > 0 && primaryCount == 0)
            return Result.Failure(EmployeeErrors.EmployeeWorkLocationPrimaryRequired);

        if (primaryCount > 1)
            return Result.Failure(EmployeeErrors.EmployeeWorkLocationPrimaryConflict);

        _workLocations.Clear();
        foreach ((Guid locationId, bool isPrimary) in normalized)
            _workLocations.Add(EmployeeWorkLocation.Create(Id, locationId, isPrimary));

        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeErrors> ReplacePersonas(IEnumerable<Guid> personaIds, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        Guid[] normalized = personaIds.Distinct().ToArray();
        _personas.Clear();
        foreach (Guid personaId in normalized)
            _personas.Add(EmployeePersona.Create(Id, personaId));

        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeLeavePeriod, EmployeeErrors> AddLeavePeriod(DateOnly from, DateOnly until, string? reason, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure<EmployeeLeavePeriod, EmployeeErrors>(EmployeeErrors.EmployeeAlreadyArchived);

        Result<EmployeeLeavePeriod, EmployeeErrors> create = EmployeeLeavePeriod.Create(Id, from, until, reason);
        if (create.IsFailure(out EmployeeErrors error))
            return Result.Failure<EmployeeLeavePeriod, EmployeeErrors>(error);

        create.IsSuccess(out EmployeeLeavePeriod period);
        _leavePeriods.Add(period);
        UpdatedAt = now;
        return Result.Success<EmployeeLeavePeriod, EmployeeErrors>(period);
    }

    public Result<EmployeeErrors> UpdateLeavePeriod(Guid periodId, DateOnly from, DateOnly until, string? reason, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        EmployeeLeavePeriod? period = _leavePeriods.SingleOrDefault(item => item.Id == periodId);
        if (period is null)
            return Result.Failure(EmployeeErrors.LeavePeriodNotFound);

        Result<EmployeeErrors> update = period.Update(from, until, reason);
        if (update.IsFailure(out EmployeeErrors error))
            return Result.Failure(error);

        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeErrors> RemoveLeavePeriod(Guid periodId, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        EmployeeLeavePeriod? period = _leavePeriods.SingleOrDefault(item => item.Id == periodId);
        if (period is null)
            return Result.Failure(EmployeeErrors.LeavePeriodNotFound);

        _leavePeriods.Remove(period);
        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeSuspensionPeriod, EmployeeErrors> AddSuspensionPeriod(DateOnly from, DateOnly until, string? reason, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure<EmployeeSuspensionPeriod, EmployeeErrors>(EmployeeErrors.EmployeeAlreadyArchived);

        Result<EmployeeSuspensionPeriod, EmployeeErrors> create = EmployeeSuspensionPeriod.Create(Id, from, until, reason);
        if (create.IsFailure(out EmployeeErrors error))
            return Result.Failure<EmployeeSuspensionPeriod, EmployeeErrors>(error);

        create.IsSuccess(out EmployeeSuspensionPeriod period);
        _suspensionPeriods.Add(period);
        UpdatedAt = now;
        return Result.Success<EmployeeSuspensionPeriod, EmployeeErrors>(period);
    }

    public Result<EmployeeErrors> UpdateSuspensionPeriod(Guid periodId, DateOnly from, DateOnly until, string? reason, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        EmployeeSuspensionPeriod? period = _suspensionPeriods.SingleOrDefault(item => item.Id == periodId);
        if (period is null)
            return Result.Failure(EmployeeErrors.SuspensionPeriodNotFound);

        Result<EmployeeErrors> update = period.Update(from, until, reason);
        if (update.IsFailure(out EmployeeErrors error))
            return Result.Failure(error);

        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeErrors> RemoveSuspensionPeriod(Guid periodId, DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        EmployeeSuspensionPeriod? period = _suspensionPeriods.SingleOrDefault(item => item.Id == periodId);
        if (period is null)
            return Result.Failure(EmployeeErrors.SuspensionPeriodNotFound);

        _suspensionPeriods.Remove(period);
        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeErrors> Archive(DateTimeOffset now)
    {
        if (ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeAlreadyArchived);

        ArchivedAt = now;
        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    public Result<EmployeeErrors> Unarchive(DateTimeOffset now)
    {
        if (!ArchivedAt.HasValue)
            return Result.Failure(EmployeeErrors.EmployeeNotArchived);

        ArchivedAt = null;
        UpdatedAt = now;
        return Result.Success<EmployeeErrors>();
    }

    private static Result<EmployeeErrors> Validate(
        string firstName,
        string lastName,
        Guid? managerEmployeeId,
        Guid? employeeId,
        DateOnly? contractStartDate,
        DateOnly? contractEndDate)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Result.Failure(EmployeeErrors.EmployeeNameRequired);

        if (employeeId.HasValue && managerEmployeeId == employeeId)
            return Result.Failure(EmployeeErrors.ManagerCannotBeSelf);

        if (contractStartDate.HasValue && contractEndDate.HasValue && contractEndDate.Value < contractStartDate.Value)
            return Result.Failure(EmployeeErrors.ContractDateRangeInvalid);

        return Result.Success<EmployeeErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
