using Fabric.Server.Core;
using Fabric.Server.Employees.Application;
using Fabric.Server.Employees.Contracts;
using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Employees.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder employees = app.MapGroup("/api/employees/employees");

        employees.MapGet("", ListEmployees)
            .WithSummary("List employees")
            .Produces<Page<EmployeeResponse>>();
        employees.MapGet("/{id:guid}", GetEmployee)
            .WithSummary("Get employee")
            .Produces<EmployeeResponse>()
            .Produces(StatusCodes.Status404NotFound);
        employees.MapPost("", CreateEmployee)
            .WithSummary("Create employee")
            .Produces<EmployeeResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
        employees.MapPut("/{id:guid}", UpdateEmployee)
            .WithSummary("Update employee")
            .Produces<EmployeeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        employees.MapPost("/{id:guid}/archive", ArchiveEmployee)
            .WithSummary("Archive employee")
            .Produces<EmployeeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        employees.MapPost("/{id:guid}/unarchive", UnarchiveEmployee)
            .WithSummary("Unarchive employee")
            .Produces<EmployeeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        employees.MapGet("/{id:guid}/work-locations", ListEmployeeWorkLocations)
            .WithSummary("List employee work locations")
            .Produces<EmployeeWorkLocationResponse[]>();
        employees.MapPut("/{id:guid}/work-locations", ReplaceEmployeeWorkLocations)
            .WithSummary("Replace employee work locations")
            .Produces<EmployeeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        employees.MapGet("/{id:guid}/personas", ListEmployeePersonas)
            .WithSummary("List employee personas")
            .Produces<PersonaSummaryResponse[]>();
        employees.MapPut("/{id:guid}/personas", ReplaceEmployeePersonas)
            .WithSummary("Replace employee personas")
            .Produces<EmployeeResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        employees.MapGet("/{id:guid}/leave-periods", ListLeavePeriods)
            .WithSummary("List employee leave periods")
            .Produces<EmployeeLeavePeriodResponse[]>();
        employees.MapPost("/{id:guid}/leave-periods", AddLeavePeriod)
            .WithSummary("Add employee leave period")
            .Produces<EmployeeLeavePeriodResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        employees.MapPut("/{id:guid}/leave-periods/{periodId:guid}", UpdateLeavePeriod)
            .WithSummary("Update employee leave period")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        employees.MapDelete("/{id:guid}/leave-periods/{periodId:guid}", DeleteLeavePeriod)
            .WithSummary("Delete employee leave period")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        employees.MapGet("/{id:guid}/suspension-periods", ListSuspensionPeriods)
            .WithSummary("List employee suspension periods")
            .Produces<EmployeeSuspensionPeriodResponse[]>();
        employees.MapPost("/{id:guid}/suspension-periods", AddSuspensionPeriod)
            .WithSummary("Add employee suspension period")
            .Produces<EmployeeSuspensionPeriodResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        employees.MapPut("/{id:guid}/suspension-periods/{periodId:guid}", UpdateSuspensionPeriod)
            .WithSummary("Update employee suspension period")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        employees.MapDelete("/{id:guid}/suspension-periods/{periodId:guid}", DeleteSuspensionPeriod)
            .WithSummary("Delete employee suspension period")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        RouteGroupBuilder organizationUnits = app.MapGroup("/api/employees/organization-units");

        organizationUnits.MapGet("", ListOrganizationUnits)
            .WithSummary("List organization units")
            .Produces<Page<OrganizationUnitResponse>>();
        organizationUnits.MapGet("/{id:guid}", GetOrganizationUnit)
            .WithSummary("Get organization unit")
            .Produces<OrganizationUnitResponse>()
            .Produces(StatusCodes.Status404NotFound);
        organizationUnits.MapGet("/{id:guid}/ancestors", ListAncestors)
            .WithSummary("List organization unit ancestors")
            .Produces<OrganizationUnitResponse[]>();
        organizationUnits.MapGet("/{id:guid}/descendants", ListDescendants)
            .WithSummary("List organization unit descendants")
            .Produces<OrganizationUnitResponse[]>();
        organizationUnits.MapGet("/{id:guid}/employees", ListSubtreeEmployees)
            .WithSummary("List organization unit subtree employees")
            .Produces<Page<EmployeeResponse>>();
        organizationUnits.MapPost("", CreateOrganizationUnit)
            .WithSummary("Create organization unit")
            .Produces<OrganizationUnitResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        organizationUnits.MapPut("/{id:guid}", UpdateOrganizationUnit)
            .WithSummary("Update organization unit")
            .Produces<OrganizationUnitResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        organizationUnits.MapPost("/{id:guid}/move", MoveOrganizationUnit)
            .WithSummary("Move organization unit")
            .Produces<OrganizationUnitResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        organizationUnits.MapPost("/{id:guid}/activate", ActivateOrganizationUnit)
            .WithSummary("Activate organization unit")
            .Produces<OrganizationUnitResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        organizationUnits.MapPost("/{id:guid}/deactivate", DeactivateOrganizationUnit)
            .WithSummary("Deactivate organization unit")
            .Produces<OrganizationUnitResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        RouteGroupBuilder personas = app.MapGroup("/api/employees/personas");

        personas.MapGet("", ListPersonas)
            .WithSummary("List personas")
            .Produces<Page<PersonaResponse>>();
        personas.MapGet("/{id:guid}", GetPersona)
            .WithSummary("Get persona")
            .Produces<PersonaResponse>()
            .Produces(StatusCodes.Status404NotFound);
        personas.MapGet("/{id:guid}/employees", ListPersonaEmployees)
            .WithSummary("List persona employees")
            .Produces<Page<EmployeeResponse>>()
            .Produces(StatusCodes.Status404NotFound);
        personas.MapPost("", CreatePersona)
            .WithSummary("Create persona")
            .Produces<PersonaResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        personas.MapPut("/{id:guid}", UpdatePersona)
            .WithSummary("Update persona")
            .Produces<PersonaResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        personas.MapPost("/{id:guid}/activate", ActivatePersona)
            .WithSummary("Activate persona")
            .Produces<PersonaResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        personas.MapPost("/{id:guid}/deactivate", DeactivatePersona)
            .WithSummary("Deactivate persona")
            .Produces<PersonaResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListEmployees(
        [AsParameters] ListEmployeesRequest request,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        List<Employee> employees = await ListEmployeesAsync(db.Employees, request, db, timeProvider, cancellationToken);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeePageAsync(employees, request.Page, request.PageSize, db, today, cancellationToken));
    }

    private static async Task<IResult> GetEmployee(
        Guid id,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await db.Employees
            .AsNoTracking()
            .Include(item => item.WorkLocations)
            .Include(item => item.Personas)
            .Include(item => item.LeavePeriods)
            .Include(item => item.SuspensionPeriods)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (employee is null)
            return Results.NotFound();

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeeAsync(employee, db, today, cancellationToken));
    }

    private static async Task<IResult> CreateEmployee(
        [FromBody] CreateEmployeeRequest request,
        EmployeeService employeeService,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        Result<Employee, EmployeeErrors> result = await employeeService.CreateEmployeeAsync(request, cancellationToken);
        if (result.IsFailure(out EmployeeErrors error))
            return ToResult(MapError(error));

        result.IsSuccess(out Employee employee);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        EmployeeResponse response = await MapEmployeeAsync(employee, db, today, cancellationToken);
        return Results.Created($"/api/employees/employees/{employee.Id}", response);
    }

    private static async Task<IResult> UpdateEmployee(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        EmployeeService employeeService,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        Result<Employee, EmployeeErrors> result = await employeeService.UpdateEmployeeAsync(id, request, cancellationToken);
        if (result.IsFailure(out EmployeeErrors error))
            return ToResult(MapError(error));

        result.IsSuccess(out Employee employee);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeeAsync(employee, db, today, cancellationToken));
    }

    private static async Task<IResult> ArchiveEmployee(
        Guid id,
        EmployeeService employeeService,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        Result<Employee, EmployeeErrors> result = await employeeService.ArchiveEmployeeAsync(id, cancellationToken);
        if (result.IsFailure(out EmployeeErrors error))
            return ToResult(MapError(error));

        result.IsSuccess(out Employee employee);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeeAsync(employee, db, today, cancellationToken));
    }

    private static async Task<IResult> UnarchiveEmployee(
        Guid id,
        EmployeeService employeeService,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        Result<Employee, EmployeeErrors> result = await employeeService.UnarchiveEmployeeAsync(id, cancellationToken);
        if (result.IsFailure(out EmployeeErrors error))
            return ToResult(MapError(error));

        result.IsSuccess(out Employee employee);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeeAsync(employee, db, today, cancellationToken));
    }

    private static async Task<IResult> ListEmployeeWorkLocations(
        Guid id,
        EmployeesDbContext db,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await db.Employees
            .AsNoTracking()
            .Include(item => item.WorkLocations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (employee is null)
            return Results.NotFound();

        return Results.Ok(employee.WorkLocations
            .OrderByDescending(location => location.IsPrimary)
            .ThenBy(location => location.LocationId)
            .Select(location => new EmployeeWorkLocationResponse(location.LocationId, location.IsPrimary))
            .ToArray());
    }

    private static async Task<IResult> ReplaceEmployeeWorkLocations(
        Guid id,
        [FromBody] ReplaceEmployeeWorkLocationsRequest request,
        EmployeeService employeeService,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        Result<Employee, EmployeeErrors> result = await employeeService.ReplaceEmployeeWorkLocationsAsync(id, request, cancellationToken);
        if (result.IsFailure(out EmployeeErrors error))
            return ToResult(MapError(error));

        result.IsSuccess(out Employee employee);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeeAsync(employee, db, today, cancellationToken));
    }

    private static async Task<IResult> ListEmployeePersonas(
        Guid id,
        EmployeesDbContext db,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await db.Employees
            .AsNoTracking()
            .Include(item => item.Personas)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (employee is null)
            return Results.NotFound();

        Guid[] personaIds = employee.Personas.Select(item => item.PersonaId).ToArray();
        Dictionary<Guid, Persona> personas = await db.Personas
            .AsNoTracking()
            .Where(persona => personaIds.Contains(persona.Id))
            .ToDictionaryAsync(persona => persona.Id, cancellationToken);

        return Results.Ok(employee.Personas
            .Select(link => personas.GetValueOrDefault(link.PersonaId))
            .Where(persona => persona is not null)
            .Select(persona => persona!.ToSummary())
            .OrderBy(persona => persona.Name)
            .ToArray());
    }

    private static async Task<IResult> ReplaceEmployeePersonas(
        Guid id,
        [FromBody] ReplaceEmployeePersonasRequest request,
        EmployeeService employeeService,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        Result<Employee, EmployeeErrors> result = await employeeService.ReplaceEmployeePersonasAsync(id, request, cancellationToken);
        if (result.IsFailure(out EmployeeErrors error))
            return ToResult(MapError(error));

        result.IsSuccess(out Employee employee);
        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeeAsync(employee, db, today, cancellationToken));
    }

    private static async Task<IResult> ListLeavePeriods(Guid id, EmployeesDbContext db, CancellationToken cancellationToken = default)
    {
        Employee? employee = await db.Employees
            .AsNoTracking()
            .Include(item => item.LeavePeriods)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (employee is null)
            return Results.NotFound();

        return Results.Ok(employee.LeavePeriods.OrderBy(period => period.From).Select(period => period.ToResponse()).ToArray());
    }

    private static async Task<IResult> AddLeavePeriod(
        Guid id,
        [FromBody] CreateEmployeePeriodRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<EmployeeLeavePeriod, EmployeeErrors> result = await employeeService.AddLeavePeriodAsync(id, request, cancellationToken);
        return result.Match<IResult>(
            period => Results.Created($"/api/employees/employees/{id}/leave-periods/{period.Id}", period.ToResponse()),
            error => ToResult(MapError(error)));
    }

    private static async Task<IResult> UpdateLeavePeriod(
        Guid id,
        Guid periodId,
        [FromBody] UpdateEmployeePeriodRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<EmployeeErrors> result = await employeeService.UpdateLeavePeriodAsync(id, periodId, request, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> DeleteLeavePeriod(
        Guid id,
        Guid periodId,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<EmployeeErrors> result = await employeeService.RemoveLeavePeriodAsync(id, periodId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> ListSuspensionPeriods(Guid id, EmployeesDbContext db, CancellationToken cancellationToken = default)
    {
        Employee? employee = await db.Employees
            .AsNoTracking()
            .Include(item => item.SuspensionPeriods)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (employee is null)
            return Results.NotFound();

        return Results.Ok(employee.SuspensionPeriods.OrderBy(period => period.From).Select(period => period.ToResponse()).ToArray());
    }

    private static async Task<IResult> AddSuspensionPeriod(
        Guid id,
        [FromBody] CreateEmployeePeriodRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<EmployeeSuspensionPeriod, EmployeeErrors> result = await employeeService.AddSuspensionPeriodAsync(id, request, cancellationToken);
        return result.Match<IResult>(
            period => Results.Created($"/api/employees/employees/{id}/suspension-periods/{period.Id}", period.ToResponse()),
            error => ToResult(MapError(error)));
    }

    private static async Task<IResult> UpdateSuspensionPeriod(
        Guid id,
        Guid periodId,
        [FromBody] UpdateEmployeePeriodRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<EmployeeErrors> result = await employeeService.UpdateSuspensionPeriodAsync(id, periodId, request, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> DeleteSuspensionPeriod(
        Guid id,
        Guid periodId,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<EmployeeErrors> result = await employeeService.RemoveSuspensionPeriodAsync(id, periodId, cancellationToken);
        return result.AsResponse(MapError);
    }

    private static async Task<IResult> ListOrganizationUnits(
        [AsParameters] ListOrganizationUnitsRequest request,
        EmployeesDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<OrganizationUnit> query = db.OrganizationUnits.AsNoTracking();

        if (request.ParentId.HasValue)
            query = query.Where(unit => unit.ParentId == request.ParentId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(unit => unit.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(unit =>
                EF.Functions.ILike(unit.Name, filter)
                || unit.Code != null && EF.Functions.ILike(unit.Code, filter)
                || EF.Functions.ILike(unit.Type, filter));
        }

        IPaged<OrganizationUnit> page = await query
            .OrderBy(unit => unit.Name)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        return Results.Ok(page.Map(unit => unit.ToResponse()));
    }

    private static async Task<IResult> GetOrganizationUnit(
        Guid id,
        EmployeesDbContext db,
        CancellationToken cancellationToken = default)
    {
        OrganizationUnit? unit = await db.OrganizationUnits.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (unit is null)
            return Results.NotFound();

        int depth = await GetUnitDepthAsync(id, db, cancellationToken);
        int childCount = await db.OrganizationUnits.CountAsync(item => item.ParentId == id, cancellationToken);
        int employeeCount = await db.Employees.CountAsync(item => item.OrganizationUnitId == id, cancellationToken);
        return Results.Ok(unit.ToResponse(depth, childCount, employeeCount));
    }

    private static async Task<IResult> ListAncestors(
        Guid id,
        EmployeesDbContext db,
        CancellationToken cancellationToken = default)
    {
        List<OrganizationUnitClosure> closures = await db.OrganizationUnitClosures
            .AsNoTracking()
            .Where(closure => closure.DescendantId == id && closure.Depth > 0)
            .OrderByDescending(closure => closure.Depth)
            .ToListAsync(cancellationToken);
        Guid[] ids = closures.Select(closure => closure.AncestorId).ToArray();
        Dictionary<Guid, int> depths = closures.ToDictionary(closure => closure.AncestorId, closure => closure.Depth);
        List<OrganizationUnit> units = await db.OrganizationUnits.AsNoTracking().Where(unit => ids.Contains(unit.Id)).ToListAsync(cancellationToken);
        OrganizationUnitResponse[] response = units
            .OrderBy(unit => depths[unit.Id])
            .Select(unit => unit.ToResponse(depths[unit.Id]))
            .ToArray();
        return Results.Ok(response);
    }

    private static async Task<IResult> ListDescendants(
        Guid id,
        EmployeesDbContext db,
        CancellationToken cancellationToken = default)
    {
        List<OrganizationUnitClosure> closures = await db.OrganizationUnitClosures
            .AsNoTracking()
            .Where(closure => closure.AncestorId == id && closure.Depth > 0)
            .OrderBy(closure => closure.Depth)
            .ToListAsync(cancellationToken);
        Guid[] ids = closures.Select(closure => closure.DescendantId).ToArray();
        Dictionary<Guid, int> depths = closures.ToDictionary(closure => closure.DescendantId, closure => closure.Depth);
        List<OrganizationUnit> units = await db.OrganizationUnits.AsNoTracking().Where(unit => ids.Contains(unit.Id)).ToListAsync(cancellationToken);
        OrganizationUnitResponse[] response = units
            .OrderBy(unit => depths[unit.Id])
            .ThenBy(unit => unit.Name)
            .Select(unit => unit.ToResponse(depths[unit.Id]))
            .ToArray();
        return Results.Ok(response);
    }

    private static async Task<IResult> ListSubtreeEmployees(
        Guid id,
        [AsParameters] ListEmployeesRequest request,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        request.OrganizationUnitId = id;
        request.IncludeDescendants = true;
        return await ListEmployees(request, db, timeProvider, cancellationToken);
    }

    private static async Task<IResult> CreateOrganizationUnit(
        [FromBody] CreateOrganizationUnitRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<OrganizationUnit, EmployeeErrors> result = await employeeService.CreateOrganizationUnitAsync(request, cancellationToken);
        return result.Match<IResult>(
            unit => Results.Created($"/api/employees/organization-units/{unit.Id}", unit.ToResponse()),
            error => ToResult(MapError(error)));
    }

    private static async Task<IResult> UpdateOrganizationUnit(
        Guid id,
        [FromBody] UpdateOrganizationUnitRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<OrganizationUnit, EmployeeErrors> result = await employeeService.UpdateOrganizationUnitAsync(id, request, cancellationToken);
        return result.Map(unit => unit.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> MoveOrganizationUnit(
        Guid id,
        [FromBody] MoveOrganizationUnitRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<OrganizationUnit, EmployeeErrors> result = await employeeService.MoveOrganizationUnitAsync(id, request.ParentId, cancellationToken);
        return result.Map(unit => unit.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ActivateOrganizationUnit(
        Guid id,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<OrganizationUnit, EmployeeErrors> result = await employeeService.SetOrganizationUnitActiveAsync(id, true, cancellationToken);
        return result.Map(unit => unit.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> DeactivateOrganizationUnit(
        Guid id,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<OrganizationUnit, EmployeeErrors> result = await employeeService.SetOrganizationUnitActiveAsync(id, false, cancellationToken);
        return result.Map(unit => unit.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListPersonas(
        [AsParameters] ListPersonasRequest request,
        EmployeesDbContext db,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Persona> query = db.Personas.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(persona => persona.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(persona => EF.Functions.ILike(persona.Name, filter));
        }

        IPaged<Persona> page = await query.OrderBy(persona => persona.Name).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(persona => persona.ToResponse()));
    }

    private static async Task<IResult> GetPersona(Guid id, EmployeesDbContext db, CancellationToken cancellationToken = default)
    {
        Persona? persona = await db.Personas.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return persona is null ? Results.NotFound() : Results.Ok(persona.ToResponse());
    }

    private static async Task<IResult> ListPersonaEmployees(
        Guid id,
        [AsParameters] ListEmployeesRequest request,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        bool personaExists = await db.Personas.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);
        if (!personaExists)
            return Results.NotFound();

        List<Employee> employees = await ListEmployeesAsync(
            db.Employees.Where(employee => employee.Personas.Any(link => link.PersonaId == id)),
            request,
            db,
            timeProvider,
            cancellationToken);

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return Results.Ok(await MapEmployeePageAsync(employees, request.Page, request.PageSize, db, today, cancellationToken));
    }

    private static async Task<IResult> CreatePersona(
        [FromBody] CreatePersonaRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<Persona, EmployeeErrors> result = await employeeService.CreatePersonaAsync(request, cancellationToken);
        return result.Match<IResult>(
            persona => Results.Created($"/api/employees/personas/{persona.Id}", persona.ToResponse()),
            error => ToResult(MapError(error)));
    }

    private static async Task<IResult> UpdatePersona(
        Guid id,
        [FromBody] UpdatePersonaRequest request,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<Persona, EmployeeErrors> result = await employeeService.UpdatePersonaAsync(id, request, cancellationToken);
        return result.Map(persona => persona.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ActivatePersona(
        Guid id,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<Persona, EmployeeErrors> result = await employeeService.SetPersonaActiveAsync(id, true, cancellationToken);
        return result.Map(persona => persona.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> DeactivatePersona(
        Guid id,
        EmployeeService employeeService,
        CancellationToken cancellationToken = default)
    {
        Result<Persona, EmployeeErrors> result = await employeeService.SetPersonaActiveAsync(id, false, cancellationToken);
        return result.Map(persona => persona.ToResponse()).AsResponse(MapError);
    }

    private static async Task<Page<EmployeeResponse>> MapEmployeePageAsync(
        IReadOnlyList<Employee> employees,
        int page,
        int pageSize,
        EmployeesDbContext db,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        int totalItems = employees.Count;
        List<Employee> pageItems = employees
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToList();

        Guid[] organizationUnitIds = pageItems.Select(employee => employee.OrganizationUnitId).Distinct().ToArray();
        Guid[] personaIds = pageItems.SelectMany(employee => employee.Personas).Select(link => link.PersonaId).Distinct().ToArray();

        Dictionary<Guid, OrganizationUnit> organizationUnits = await db.OrganizationUnits.AsNoTracking()
            .Where(unit => organizationUnitIds.Contains(unit.Id))
            .ToDictionaryAsync(unit => unit.Id, cancellationToken);
        Dictionary<Guid, Persona> personas = await db.Personas.AsNoTracking()
            .Where(persona => personaIds.Contains(persona.Id))
            .ToDictionaryAsync(persona => persona.Id, cancellationToken);

        int totalPages = pageSize == 0 ? 0 : (int)Math.Ceiling((double)totalItems / pageSize);
        return new Page<EmployeeResponse>
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            IsLastPage = totalPages == 0 || (page + 1) >= totalPages,
            Items = pageItems
                .Select(employee => employee.ToResponse(organizationUnits[employee.OrganizationUnitId], personas, today))
                .ToList(),
        };
    }

    private static async Task<EmployeeResponse> MapEmployeeAsync(
        Employee employee,
        EmployeesDbContext db,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        Employee? aggregate = await db.Employees
            .AsNoTracking()
            .Include(item => item.WorkLocations)
            .Include(item => item.Personas)
            .Include(item => item.LeavePeriods)
            .Include(item => item.SuspensionPeriods)
            .SingleAsync(item => item.Id == employee.Id, cancellationToken);
        OrganizationUnit organizationUnit = await db.OrganizationUnits.AsNoTracking()
            .SingleAsync(unit => unit.Id == aggregate.OrganizationUnitId, cancellationToken);
        Guid[] personaIds = aggregate.Personas.Select(link => link.PersonaId).Distinct().ToArray();
        Dictionary<Guid, Persona> personas = await db.Personas.AsNoTracking()
            .Where(persona => personaIds.Contains(persona.Id))
            .ToDictionaryAsync(persona => persona.Id, cancellationToken);
        return aggregate.ToResponse(organizationUnit, personas, today);
    }

    private static async Task<List<Employee>> ListEmployeesAsync(
        IQueryable<Employee> source,
        ListEmployeesRequest request,
        EmployeesDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        IQueryable<Employee> query = source
            .AsNoTracking()
            .Include(employee => employee.WorkLocations)
            .Include(employee => employee.Personas)
            .Include(employee => employee.LeavePeriods)
            .Include(employee => employee.SuspensionPeriods);

        if (request.ManagerEmployeeId.HasValue)
        {
            if (request.IncludeIndirectReports == true)
            {
                Guid[] reportIds = await GetReportIdsAsync(request.ManagerEmployeeId.Value, db, cancellationToken);
                query = query.Where(employee => reportIds.Contains(employee.Id));
            }
            else
            {
                query = query.Where(employee => employee.ManagerEmployeeId == request.ManagerEmployeeId.Value);
            }
        }

        if (request.OrganizationUnitId.HasValue)
        {
            if (request.IncludeDescendants)
            {
                Guid[] unitIds = await db.OrganizationUnitClosures
                    .Where(closure => closure.AncestorId == request.OrganizationUnitId.Value)
                    .Select(closure => closure.DescendantId)
                    .ToArrayAsync(cancellationToken);
                query = query.Where(employee => unitIds.Contains(employee.OrganizationUnitId));
            }
            else
            {
                query = query.Where(employee => employee.OrganizationUnitId == request.OrganizationUnitId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(employee =>
                EF.Functions.ILike(employee.FirstName, filter)
                || EF.Functions.ILike(employee.LastName, filter)
                || EF.Functions.ILike(employee.FirstName + " " + employee.LastName, filter)
                || employee.Email != null && EF.Functions.ILike(employee.Email, filter)
                || employee.EmployeeNumber != null && EF.Functions.ILike(employee.EmployeeNumber, filter)
                || employee.DirectoryId != null && EF.Functions.ILike(employee.DirectoryId, filter)
                || employee.JobTitle != null && EF.Functions.ILike(employee.JobTitle, filter));
        }

        List<Employee> employees = await query
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ThenBy(employee => employee.Id)
            .ToListAsync(cancellationToken);

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (request.Status is { Length: > 0 })
            employees = employees.Where(employee => request.Status.Contains(EmployeeLifecycleCalculator.Calculate(employee, today))).ToList();

        return employees;
    }

    private static async Task<Guid[]> GetReportIdsAsync(Guid managerEmployeeId, EmployeesDbContext db, CancellationToken cancellationToken)
    {
        var reportingLines = await db.Employees
            .AsNoTracking()
            .Select(employee => new { employee.Id, employee.ManagerEmployeeId })
            .ToArrayAsync(cancellationToken);

        var directReportsByManagerId = reportingLines
            .Where(item => item.ManagerEmployeeId.HasValue)
            .GroupBy(item => item.ManagerEmployeeId!.Value, item => item.Id)
            .ToDictionary(group => group.Key, group => group.ToArray());

        HashSet<Guid> reportIds = [];
        Queue<Guid> pendingManagerIds = new([managerEmployeeId]);

        while (pendingManagerIds.Count > 0)
        {
            Guid currentManagerId = pendingManagerIds.Dequeue();
            if (!directReportsByManagerId.TryGetValue(currentManagerId, out Guid[]? directReports))
                continue;

            foreach (Guid reportId in directReports)
            {
                if (!reportIds.Add(reportId))
                    continue;

                pendingManagerIds.Enqueue(reportId);
            }
        }

        return [.. reportIds];
    }

    private static async Task<int> GetUnitDepthAsync(Guid id, EmployeesDbContext db, CancellationToken cancellationToken)
    {
        int? depth = await db.OrganizationUnitClosures
            .Where(closure => closure.DescendantId == id)
            .MaxAsync(closure => (int?)closure.Depth, cancellationToken);
        return depth ?? 0;
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(EmployeeErrors error) =>
        error switch
        {
            EmployeeErrors.EmployeeNotFound => Problem(StatusCodes.Status404NotFound, "Employee not found."),
            EmployeeErrors.EmployeeIdentityNotFound => Problem(StatusCodes.Status404NotFound, "Employee identity not found."),
            EmployeeErrors.OrganizationUnitNotFound => Problem(StatusCodes.Status404NotFound, "Organization unit not found."),
            EmployeeErrors.ManagerNotFound => Problem(StatusCodes.Status404NotFound, "Manager employee not found."),
            EmployeeErrors.PersonaNotFound => Problem(StatusCodes.Status404NotFound, "Persona not found."),
            EmployeeErrors.LeavePeriodNotFound => Problem(StatusCodes.Status404NotFound, "Leave period not found."),
            EmployeeErrors.SuspensionPeriodNotFound => Problem(StatusCodes.Status404NotFound, "Suspension period not found."),
            EmployeeErrors.LocationNotFound => Problem(StatusCodes.Status404NotFound, "Location not found."),
            EmployeeErrors.OrganizationUnitInactive => Problem(StatusCodes.Status409Conflict, "Organization unit is inactive."),
            EmployeeErrors.OrganizationUnitAlreadyExists => Problem(StatusCodes.Status409Conflict, "Organization unit already exists."),
            EmployeeErrors.OrganizationUnitHasActiveChildren => Problem(StatusCodes.Status409Conflict, "Organization unit has active children."),
            EmployeeErrors.OrganizationUnitHasActiveEmployees => Problem(StatusCodes.Status409Conflict, "Organization unit has active employees."),
            EmployeeErrors.OrganizationUnitParentCycle => Problem(StatusCodes.Status409Conflict, "Organization unit move would create a cycle."),
            EmployeeErrors.EmployeeNumberAlreadyExists => Problem(StatusCodes.Status409Conflict, "Employee number already exists."),
            EmployeeErrors.DirectoryIdAlreadyExists => Problem(StatusCodes.Status409Conflict, "Directory id already exists."),
            EmployeeErrors.EmployeeAlreadyArchived => Problem(StatusCodes.Status409Conflict, "Employee is already archived."),
            EmployeeErrors.EmployeeNotArchived => Problem(StatusCodes.Status409Conflict, "Employee is not archived."),
            EmployeeErrors.PersonaAlreadyExists => Problem(StatusCodes.Status409Conflict, "Persona already exists."),
            EmployeeErrors.PersonaInactive => Problem(StatusCodes.Status409Conflict, "Persona is inactive."),
            EmployeeErrors.PersonaAssignedToEmployees => Problem(StatusCodes.Status409Conflict, "Persona is assigned to employees."),
            EmployeeErrors.ManagerCannotBeSelf => Problem(StatusCodes.Status400BadRequest, "Manager employee cannot be the employee itself."),
            EmployeeErrors.EmployeeNameRequired => Problem(StatusCodes.Status400BadRequest, "First name and last name are required."),
            EmployeeErrors.ContractDateRangeInvalid => Problem(StatusCodes.Status400BadRequest, "Contract end date must be on or after contract start date."),
            EmployeeErrors.EmployeePeriodDateRangeInvalid => Problem(StatusCodes.Status400BadRequest, "Period end date must be on or after period start date."),
            EmployeeErrors.EmployeeWorkLocationPrimaryRequired => Problem(StatusCodes.Status400BadRequest, "A primary work location is required when work locations are provided."),
            EmployeeErrors.EmployeeWorkLocationPrimaryConflict => Problem(StatusCodes.Status400BadRequest, "Only one primary work location is allowed."),
            EmployeeErrors.PersonaNameRequired => Problem(StatusCodes.Status400BadRequest, "Persona name is required."),
            EmployeeErrors.IdentityCreationFailed => Problem(StatusCodes.Status400BadRequest, "Identity could not be created."),
            EmployeeErrors.IdentityUpdateFailed => Problem(StatusCodes.Status400BadRequest, "Identity could not be updated."),
            _ => Problem(StatusCodes.Status400BadRequest, "Employee request is invalid."),
        };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });

    private static IResult ToResult((int statusCode, ProblemDetails? problemDetails) error) =>
        error.problemDetails is null
            ? Results.StatusCode(error.statusCode)
            : Results.Json(error.problemDetails, statusCode: error.statusCode);
}
