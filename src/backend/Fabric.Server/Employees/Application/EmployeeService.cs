using Fabric.Server.Core;
using Fabric.Server.Employees.Contracts;
using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Sagas.EmployeeLifecycle;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fabric.Server.Employees.Application;

public sealed class EmployeeService(
    EmployeesDbContext db,
    IdentitiesDbContext identitiesDb,
    LocationsDbContext locationsDb,
    EmployeeLifecycleService lifecycleService,
    EmployeeLifecycleAutomationService employeeLifecycleAutomationService,
    TimeProvider timeProvider)
{
    public async Task<Result<Employee, EmployeeErrors>> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        Result<EmployeeErrors> validation = await ValidateEmployeeReferencesAsync(
            request.OrganizationUnitId,
            request.ManagerEmployeeId,
            request.EmployeeNumber,
            request.DirectoryId,
            excludeEmployeeId: null,
            cancellationToken);
        if (validation.IsFailure(out EmployeeErrors error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        Result<Identity, IdentityErrors> createIdentity = Identity.Create(
            request.FirstName,
            null,
            request.LastName,
            null,
            request.Email,
            null,
            now);
        if (createIdentity.IsFailure(out _))
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.IdentityCreationFailed);

        createIdentity.IsSuccess(out Identity identity);

        Result<Employee, EmployeeErrors> createEmployee = Employee.Create(
            identity.Id,
            request.FirstName,
            request.LastName,
            request.BirthDate,
            request.EmployeeNumber,
            request.DirectoryId,
            request.Email,
            request.OrganizationUnitId,
            request.ManagerEmployeeId,
            request.JobTitle,
            request.ContractStartDate,
            request.ContractEndDate,
            now);
        if (createEmployee.IsFailure(out error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        createEmployee.IsSuccess(out Employee employee);

        Result<EmployeeAffiliation, IdentityErrors> addAffiliation = identity.AddEmployeeAffiliation(employee.Id, now, null, now);
        if (addAffiliation.IsFailure(out _))
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.IdentityCreationFailed);

        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken);

        identitiesDb.Identities.Add(identity);
        await identitiesDb.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.EmployeeCreated, "Employee created", cancellationToken);
        await employeeLifecycleAutomationService.EnqueueAsync(employee.Id, "EmployeeCreated", cancellationToken);

        return Result.Success<Employee, EmployeeErrors>(employee);
    }

    public async Task<Result<Employee, EmployeeErrors>> UpdateEmployeeAsync(
        Guid employeeId,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeErrors> validation = await ValidateEmployeeReferencesAsync(
            request.OrganizationUnitId,
            request.ManagerEmployeeId,
            request.EmployeeNumber,
            request.DirectoryId,
            employee.Id,
            cancellationToken);
        if (validation.IsFailure(out EmployeeErrors error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        Result<EmployeeErrors> update = employee.Update(
            request.FirstName,
            request.LastName,
            request.BirthDate,
            request.EmployeeNumber,
            request.DirectoryId,
            request.Email,
            request.OrganizationUnitId,
            request.ManagerEmployeeId,
            request.JobTitle,
            request.ContractStartDate,
            request.ContractEndDate,
            timeProvider.GetUtcNow());
        if (update.IsFailure(out error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        Identity? identity = await identitiesDb.Identities
            .Include(item => item.EmployeeAffiliations)
            .SingleOrDefaultAsync(item => item.Id == employee.IdentityId, cancellationToken);
        if (identity is null)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.EmployeeIdentityNotFound);

        Result<IdentityErrors> updateIdentity = identity.UpdateProfile(
            request.FirstName,
            identity.MiddleName,
            request.LastName,
            identity.PreferredName,
            request.Email,
            identity.Phone,
            timeProvider.GetUtcNow());
        if (updateIdentity.IsFailure(out _))
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.IdentityUpdateFailed);

        await db.SaveChangesAsync(cancellationToken);

        await identitiesDb.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.EmployeeUpdated, "Employee updated", cancellationToken);
        await employeeLifecycleAutomationService.EnqueueAsync(employee.Id, "EmployeeUpdated", cancellationToken);

        return Result.Success<Employee, EmployeeErrors>(employee);
    }

    public async Task<Result<Employee, EmployeeErrors>> ArchiveEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeErrors> archive = employee.Archive(timeProvider.GetUtcNow());
        if (archive.IsFailure(out EmployeeErrors error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.EmployeeArchived, "Employee archived", cancellationToken);
        await employeeLifecycleAutomationService.EnqueueAsync(employee.Id, "EmployeeArchived", cancellationToken);
        return Result.Success<Employee, EmployeeErrors>(employee);
    }

    public async Task<Result<Employee, EmployeeErrors>> UnarchiveEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeErrors> unarchive = employee.Unarchive(timeProvider.GetUtcNow());
        if (unarchive.IsFailure(out EmployeeErrors error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.EmployeeUnarchived, "Employee unarchived", cancellationToken);
        await employeeLifecycleAutomationService.EnqueueAsync(employee.Id, "EmployeeUnarchived", cancellationToken);
        return Result.Success<Employee, EmployeeErrors>(employee);
    }

    public async Task<Result<Employee, EmployeeErrors>> ReplaceEmployeeWorkLocationsAsync(
        Guid employeeId,
        ReplaceEmployeeWorkLocationsRequest request,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.EmployeeNotFound);

        Guid[] locationIds = request.WorkLocations.Select(item => item.LocationId).Distinct().ToArray();
        int locationCount = await locationsDb.LocationLookups.CountAsync(item => locationIds.Contains(item.Id), cancellationToken);
        if (locationCount != locationIds.Length)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.LocationNotFound);

        Result<EmployeeErrors> replace = employee.ReplaceWorkLocations(
            request.WorkLocations.Select(item => (item.LocationId, item.IsPrimary)),
            timeProvider.GetUtcNow());
        if (replace.IsFailure(out EmployeeErrors error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await employeeLifecycleAutomationService.EnqueueAsync(employee.Id, "EmployeeWorkLocationsReplaced", cancellationToken);
        return Result.Success<Employee, EmployeeErrors>(employee);
    }

    public async Task<Result<Employee, EmployeeErrors>> ReplaceEmployeePersonasAsync(
        Guid employeeId,
        ReplaceEmployeePersonasRequest request,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.EmployeeNotFound);

        Guid[] personaIds = request.PersonaIds.Distinct().ToArray();
        List<Persona> personas = await db.Personas.Where(item => personaIds.Contains(item.Id)).ToListAsync(cancellationToken);
        if (personas.Count != personaIds.Length)
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.PersonaNotFound);

        if (personas.Any(persona => !persona.IsActive))
            return Result.Failure<Employee, EmployeeErrors>(EmployeeErrors.PersonaInactive);

        Result<EmployeeErrors> replace = employee.ReplacePersonas(personaIds, timeProvider.GetUtcNow());
        if (replace.IsFailure(out EmployeeErrors error))
            return Result.Failure<Employee, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await employeeLifecycleAutomationService.EnqueueAsync(employee.Id, "EmployeePersonasReplaced", cancellationToken);
        return Result.Success<Employee, EmployeeErrors>(employee);
    }

    public async Task<Result<EmployeeLeavePeriod, EmployeeErrors>> AddLeavePeriodAsync(
        Guid employeeId,
        CreateEmployeePeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeLeavePeriod, EmployeeErrors>(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeLeavePeriod, EmployeeErrors> add = employee.AddLeavePeriod(request.From, request.Until, request.Reason, timeProvider.GetUtcNow());
        if (add.IsFailure(out EmployeeErrors error))
            return Result.Failure<EmployeeLeavePeriod, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.LeavePeriodAdded, "Leave period added", cancellationToken);
        add.IsSuccess(out EmployeeLeavePeriod period);
        return Result.Success<EmployeeLeavePeriod, EmployeeErrors>(period);
    }

    public async Task<Result<EmployeeErrors>> UpdateLeavePeriodAsync(
        Guid employeeId,
        Guid periodId,
        UpdateEmployeePeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeErrors> update = employee.UpdateLeavePeriod(periodId, request.From, request.Until, request.Reason, timeProvider.GetUtcNow());
        if (update.IsFailure(out EmployeeErrors error))
            return Result.Failure(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.LeavePeriodUpdated, "Leave period updated", cancellationToken);
        return Result.Success<EmployeeErrors>();
    }

    public async Task<Result<EmployeeErrors>> RemoveLeavePeriodAsync(
        Guid employeeId,
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeErrors> remove = employee.RemoveLeavePeriod(periodId, timeProvider.GetUtcNow());
        if (remove.IsFailure(out EmployeeErrors error))
            return Result.Failure(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.LeavePeriodRemoved, "Leave period removed", cancellationToken);
        return Result.Success<EmployeeErrors>();
    }

    public async Task<Result<EmployeeSuspensionPeriod, EmployeeErrors>> AddSuspensionPeriodAsync(
        Guid employeeId,
        CreateEmployeePeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<EmployeeSuspensionPeriod, EmployeeErrors>(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeSuspensionPeriod, EmployeeErrors> add = employee.AddSuspensionPeriod(request.From, request.Until, request.Reason, timeProvider.GetUtcNow());
        if (add.IsFailure(out EmployeeErrors error))
            return Result.Failure<EmployeeSuspensionPeriod, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.SuspensionPeriodAdded, "Suspension period added", cancellationToken);
        add.IsSuccess(out EmployeeSuspensionPeriod period);
        return Result.Success<EmployeeSuspensionPeriod, EmployeeErrors>(period);
    }

    public async Task<Result<EmployeeErrors>> UpdateSuspensionPeriodAsync(
        Guid employeeId,
        Guid periodId,
        UpdateEmployeePeriodRequest request,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeErrors> update = employee.UpdateSuspensionPeriod(periodId, request.From, request.Until, request.Reason, timeProvider.GetUtcNow());
        if (update.IsFailure(out EmployeeErrors error))
            return Result.Failure(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.SuspensionPeriodUpdated, "Suspension period updated", cancellationToken);
        return Result.Success<EmployeeErrors>();
    }

    public async Task<Result<EmployeeErrors>> RemoveSuspensionPeriodAsync(
        Guid employeeId,
        Guid periodId,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await GetEmployeeAggregateAsync(employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure(EmployeeErrors.EmployeeNotFound);

        Result<EmployeeErrors> remove = employee.RemoveSuspensionPeriod(periodId, timeProvider.GetUtcNow());
        if (remove.IsFailure(out EmployeeErrors error))
            return Result.Failure(error);

        await db.SaveChangesAsync(cancellationToken);
        await lifecycleService.ReconcileNowAndRescheduleAsync(employee.Id, EmployeeLifecycleSource.SuspensionPeriodRemoved, "Suspension period removed", cancellationToken);
        return Result.Success<EmployeeErrors>();
    }

    public async Task<Result<OrganizationUnit, EmployeeErrors>> CreateOrganizationUnitAsync(
        CreateOrganizationUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        OrganizationUnit? parent = null;
        if (request.ParentId.HasValue)
        {
            parent = await db.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == request.ParentId.Value, cancellationToken);
            if (parent is null)
                return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitNotFound);

            if (!parent.IsActive)
                return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitInactive);
        }

        string? normalizedCode = NormalizeOptional(request.Code);
        if (normalizedCode is not null && await db.OrganizationUnits.AnyAsync(unit => unit.Code == normalizedCode, cancellationToken))
            return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitAlreadyExists);

        Result<OrganizationUnit, EmployeeErrors> create = OrganizationUnit.Create(request.Name, normalizedCode, request.Type, request.ParentId, now);
        if (create.IsFailure(out EmployeeErrors error))
            return Result.Failure<OrganizationUnit, EmployeeErrors>(error);

        create.IsSuccess(out OrganizationUnit unit);
        db.OrganizationUnits.Add(unit);
        await AddClosureRowsForNewUnitAsync(unit, parent?.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<OrganizationUnit, EmployeeErrors>(unit);
    }

    public async Task<Result<OrganizationUnit, EmployeeErrors>> UpdateOrganizationUnitAsync(
        Guid id,
        UpdateOrganizationUnitRequest request,
        CancellationToken cancellationToken = default)
    {
        OrganizationUnit? unit = await db.OrganizationUnits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (unit is null)
            return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitNotFound);

        string? normalizedCode = NormalizeOptional(request.Code);
        if (normalizedCode is not null
            && await db.OrganizationUnits.AnyAsync(item => item.Id != id && item.Code == normalizedCode, cancellationToken))
            return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitAlreadyExists);

        Result<EmployeeErrors> update = unit.Update(request.Name, normalizedCode, request.Type, timeProvider.GetUtcNow());
        if (update.IsFailure(out EmployeeErrors error))
            return Result.Failure<OrganizationUnit, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<OrganizationUnit, EmployeeErrors>(unit);
    }

    public async Task<Result<OrganizationUnit, EmployeeErrors>> MoveOrganizationUnitAsync(
        Guid id,
        Guid? parentId,
        CancellationToken cancellationToken = default)
    {
        OrganizationUnit? unit = await db.OrganizationUnits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (unit is null)
            return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitNotFound);

        if (parentId.HasValue)
        {
            OrganizationUnit? parent = await db.OrganizationUnits.SingleOrDefaultAsync(item => item.Id == parentId.Value, cancellationToken);
            if (parent is null)
                return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitNotFound);

            if (!parent.IsActive)
                return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitInactive);

            bool parentIsDescendant = await db.OrganizationUnitClosures
                .AnyAsync(closure => closure.AncestorId == id && closure.DescendantId == parentId.Value, cancellationToken);
            if (parentIsDescendant)
                return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitParentCycle);
        }

        Result<EmployeeErrors> move = unit.Move(parentId, timeProvider.GetUtcNow());
        if (move.IsFailure(out EmployeeErrors error))
            return Result.Failure<OrganizationUnit, EmployeeErrors>(error);

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await RebuildClosureRowsForMoveAsync(id, parentId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result.Success<OrganizationUnit, EmployeeErrors>(unit);
    }

    public async Task<Result<OrganizationUnit, EmployeeErrors>> SetOrganizationUnitActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        OrganizationUnit? unit = await db.OrganizationUnits.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (unit is null)
            return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (active)
        {
            unit.Activate(now);
        }
        else
        {
            bool hasActiveChildren = await db.OrganizationUnits.AnyAsync(item => item.ParentId == id && item.IsActive, cancellationToken);
            if (hasActiveChildren)
                return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitHasActiveChildren);

            Guid[] subtreeIds = await GetSubtreeIdsAsync(id, cancellationToken);
            List<Employee> subtreeEmployees = await db.Employees
                .Include(employee => employee.LeavePeriods)
                .Include(employee => employee.SuspensionPeriods)
                .Where(employee => subtreeIds.Contains(employee.OrganizationUnitId))
                .ToListAsync(cancellationToken);

            DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);
            bool hasActiveEmployees = subtreeEmployees.Any(employee =>
                EmployeeLifecycleCalculator.Calculate(employee, today) is not EmployeeStatus.Terminated and not EmployeeStatus.Archived);
            if (hasActiveEmployees)
                return Result.Failure<OrganizationUnit, EmployeeErrors>(EmployeeErrors.OrganizationUnitHasActiveEmployees);

            unit.Deactivate(now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<OrganizationUnit, EmployeeErrors>(unit);
    }

    public async Task<Result<Persona, EmployeeErrors>> CreatePersonaAsync(
        CreatePersonaRequest request,
        CancellationToken cancellationToken = default)
    {
        string normalizedName = request.Name.Trim();
        if (await db.Personas.AnyAsync(item => item.Name == normalizedName, cancellationToken))
            return Result.Failure<Persona, EmployeeErrors>(EmployeeErrors.PersonaAlreadyExists);

        Result<Persona, EmployeeErrors> create = Persona.Create(request.Name, timeProvider.GetUtcNow());
        if (create.IsFailure(out EmployeeErrors error))
            return Result.Failure<Persona, EmployeeErrors>(error);

        create.IsSuccess(out Persona persona);
        db.Personas.Add(persona);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Persona, EmployeeErrors>(persona);
    }

    public async Task<Result<Persona, EmployeeErrors>> UpdatePersonaAsync(
        Guid id,
        UpdatePersonaRequest request,
        CancellationToken cancellationToken = default)
    {
        Persona? persona = await db.Personas.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (persona is null)
            return Result.Failure<Persona, EmployeeErrors>(EmployeeErrors.PersonaNotFound);

        string normalizedName = request.Name.Trim();
        if (await db.Personas.AnyAsync(item => item.Id != id && item.Name == normalizedName, cancellationToken))
            return Result.Failure<Persona, EmployeeErrors>(EmployeeErrors.PersonaAlreadyExists);

        Result<EmployeeErrors> update = persona.Update(request.Name, timeProvider.GetUtcNow());
        if (update.IsFailure(out EmployeeErrors error))
            return Result.Failure<Persona, EmployeeErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Persona, EmployeeErrors>(persona);
    }

    public async Task<Result<Persona, EmployeeErrors>> SetPersonaActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        Persona? persona = await db.Personas.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (persona is null)
            return Result.Failure<Persona, EmployeeErrors>(EmployeeErrors.PersonaNotFound);

        if (!active)
        {
            bool assigned = await db.EmployeePersonas.AnyAsync(item => item.PersonaId == id, cancellationToken);
            if (assigned)
                return Result.Failure<Persona, EmployeeErrors>(EmployeeErrors.PersonaAssignedToEmployees);

            persona.Deactivate(timeProvider.GetUtcNow());
        }
        else
        {
            persona.Activate(timeProvider.GetUtcNow());
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Persona, EmployeeErrors>(persona);
    }

    private async Task<Employee?> GetEmployeeAggregateAsync(Guid employeeId, CancellationToken cancellationToken) =>
        await db.Employees
            .Include(employee => employee.WorkLocations)
            .Include(employee => employee.Personas)
            .Include(employee => employee.LeavePeriods)
            .Include(employee => employee.SuspensionPeriods)
            .SingleOrDefaultAsync(employee => employee.Id == employeeId, cancellationToken);

    private async Task<Result<EmployeeErrors>> ValidateEmployeeReferencesAsync(
        Guid organizationUnitId,
        Guid? managerEmployeeId,
        string? employeeNumber,
        string? directoryId,
        Guid? excludeEmployeeId,
        CancellationToken cancellationToken)
    {
        OrganizationUnit? organizationUnit = await db.OrganizationUnits.SingleOrDefaultAsync(unit => unit.Id == organizationUnitId, cancellationToken);
        if (organizationUnit is null)
            return Result.Failure(EmployeeErrors.OrganizationUnitNotFound);

        if (!organizationUnit.IsActive)
            return Result.Failure(EmployeeErrors.OrganizationUnitInactive);

        if (managerEmployeeId.HasValue && !await db.Employees.AnyAsync(employee => employee.Id == managerEmployeeId.Value, cancellationToken))
            return Result.Failure(EmployeeErrors.ManagerNotFound);

        string? normalizedEmployeeNumber = NormalizeOptional(employeeNumber);
        if (normalizedEmployeeNumber is not null
            && await db.Employees.AnyAsync(
                employee => employee.Id != excludeEmployeeId && employee.EmployeeNumber == normalizedEmployeeNumber,
                cancellationToken))
            return Result.Failure(EmployeeErrors.EmployeeNumberAlreadyExists);

        string? normalizedDirectoryId = NormalizeOptional(directoryId);
        if (normalizedDirectoryId is not null
            && await db.Employees.AnyAsync(
                employee => employee.Id != excludeEmployeeId && employee.DirectoryId == normalizedDirectoryId,
                cancellationToken))
            return Result.Failure(EmployeeErrors.DirectoryIdAlreadyExists);

        return Result.Success<EmployeeErrors>();
    }

    private async Task AddClosureRowsForNewUnitAsync(OrganizationUnit unit, Guid? parentId, CancellationToken cancellationToken)
    {
        db.OrganizationUnitClosures.Add(OrganizationUnitClosure.Create(unit.Id, unit.Id, 0));

        if (!parentId.HasValue)
            return;

        List<OrganizationUnitClosure> ancestors = await db.OrganizationUnitClosures
            .Where(closure => closure.DescendantId == parentId.Value)
            .ToListAsync(cancellationToken);

        foreach (OrganizationUnitClosure ancestor in ancestors)
        {
            db.OrganizationUnitClosures.Add(OrganizationUnitClosure.Create(
                ancestor.AncestorId,
                unit.Id,
                ancestor.Depth + 1));
        }
    }

    private async Task RebuildClosureRowsForMoveAsync(Guid unitId, Guid? parentId, CancellationToken cancellationToken)
    {
        List<OrganizationUnitClosure> subtree = await db.OrganizationUnitClosures
            .Where(closure => closure.AncestorId == unitId)
            .ToListAsync(cancellationToken);
        Guid[] subtreeIds = subtree.Select(closure => closure.DescendantId).ToArray();

        await db.OrganizationUnitClosures
            .Where(closure => subtreeIds.Contains(closure.DescendantId) && !subtreeIds.Contains(closure.AncestorId))
            .ExecuteDeleteAsync(cancellationToken);

        if (!parentId.HasValue)
            return;

        List<OrganizationUnitClosure> parentAncestors = await db.OrganizationUnitClosures
            .Where(closure => closure.DescendantId == parentId.Value)
            .ToListAsync(cancellationToken);

        foreach (OrganizationUnitClosure ancestor in parentAncestors)
        {
            foreach (OrganizationUnitClosure descendant in subtree)
            {
                db.OrganizationUnitClosures.Add(OrganizationUnitClosure.Create(
                    ancestor.AncestorId,
                    descendant.DescendantId,
                    ancestor.Depth + descendant.Depth + 1));
            }
        }
    }

    private async Task<Guid[]> GetSubtreeIdsAsync(Guid organizationUnitId, CancellationToken cancellationToken) =>
        await db.OrganizationUnitClosures
            .Where(closure => closure.AncestorId == organizationUnitId)
            .Select(closure => closure.DescendantId)
            .ToArrayAsync(cancellationToken);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
