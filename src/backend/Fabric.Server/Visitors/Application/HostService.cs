using Fabric.Server.Core;
using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Infrastructure.Tenancy;
using Fabric.Server.Tenants.Domain;
using Fabric.Server.Tenants.Persistence;
using Fabric.Server.Visitors.Contracts;
using Fabric.Server.Visitors.Domain;
using Fabric.Server.Visitors.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Visitors.Application;

public sealed class HostService(
    VisitorsDbContext visitorsDb,
    EmployeesDbContext employeesDb,
    TenantsDbContext tenantsDb,
    ITenantContextAccessor tenantContext,
    ITenantStore tenantStore)
{
    public async Task<HostSettingsResponse> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        new(await GetAssignmentModeAsync(cancellationToken));

    public async Task<HostSettingsResponse> UpdateSettingsAsync(HostAssignmentMode assignmentMode, CancellationToken cancellationToken = default)
    {
        int affectedRows = await tenantsDb.Tenants
            .Where(item => item.Id == tenantContext.TenantId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(
                    item => item.Configuration.Host.AssignmentMode,
                    _ => assignmentMode),
                cancellationToken);

        if (affectedRows == 0)
            throw new InvalidOperationException($"Tenant '{tenantContext.TenantId}' was not found while updating host settings.");

        tenantStore.InvalidateTenant(tenantContext.TenantId);
        return await GetSettingsAsync(cancellationToken);
    }

    public async Task<Page<HostResponse>> ListHostsAsync(ListHostsRequest request, CancellationToken cancellationToken = default)
    {
        HostAssignmentMode assignmentMode = await GetAssignmentModeAsync(cancellationToken);
        IQueryable<Employee> query = employeesDb.Employees.AsNoTracking().Where(item => item.ArchivedAt == null);
        if (assignmentMode is HostAssignmentMode.AllowList)
        {
            Guid[] allowListEmployeeIds = await visitorsDb.HostAssignments.AsNoTracking()
                .Select(item => item.EmployeeId)
                .ToArrayAsync(cancellationToken);
            query = query.Where(item => allowListEmployeeIds.Contains(item.Id));
        }

        query = ApplyQueryFilter(query, request.Query);

        IPaged<Employee> page = await query
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ThenBy(employee => employee.Id)
            .GetPageAsync(request.Page, request.PageSize, cancellationToken);

        HashSet<Guid> allowListedEmployeeIds = await GetAllowListedEmployeeIdsAsync(page.Items.Select(item => item.Id).ToArray(), cancellationToken);
        return page.Map(employee => ToResponse(employee, allowListedEmployeeIds.Contains(employee.Id)));
    }

    public async Task<HostResponse?> GetHostByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        IQueryable<Employee> query = employeesDb.Employees.AsNoTracking()
            .Where(item => item.ArchivedAt == null && item.Id == employeeId);
        if (await GetAssignmentModeAsync(cancellationToken) is HostAssignmentMode.AllowList)
        {
            bool hostIsAllowListed = await visitorsDb.HostAssignments.AsNoTracking().AnyAsync(item => item.EmployeeId == employeeId, cancellationToken);
            if (!hostIsAllowListed)
                return null;
        }

        Employee? employee = await query.SingleOrDefaultAsync(cancellationToken);
        if (employee is null)
            return null;

        bool isAllowListed = await visitorsDb.HostAssignments.AsNoTracking().AnyAsync(item => item.EmployeeId == employeeId, cancellationToken);
        return ToResponse(employee, isAllowListed);
    }

    public async Task<bool> IsEmployeeHostAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        bool employeeExists = await employeesDb.Employees.AsNoTracking()
            .AnyAsync(item => item.Id == employeeId && item.ArchivedAt == null, cancellationToken);
        if (!employeeExists)
            return false;

        HostAssignmentMode assignmentMode = await GetAssignmentModeAsync(cancellationToken);
        return assignmentMode switch
        {
            HostAssignmentMode.AllEmployees => true,
            HostAssignmentMode.AllowList => await visitorsDb.HostAssignments.AsNoTracking().AnyAsync(item => item.EmployeeId == employeeId, cancellationToken),
            _ => false
        };
    }

    public async Task<Result<HostResponse, HostErrors>> AddHostAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (await GetAssignmentModeAsync(cancellationToken) is not HostAssignmentMode.AllowList)
            return Result.Failure<HostResponse, HostErrors>(HostErrors.AssignmentModeDoesNotSupportAllowList);

        Employee? employee = await employeesDb.Employees.SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null)
            return Result.Failure<HostResponse, HostErrors>(HostErrors.EmployeeNotFound);

        if (employee.ArchivedAt.HasValue)
            return Result.Failure<HostResponse, HostErrors>(HostErrors.EmployeeArchived);

        HostAssignment? existing = await visitorsDb.HostAssignments.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (existing is null)
        {
            visitorsDb.HostAssignments.Add(HostAssignment.Create(employeeId));
            await visitorsDb.SaveChangesAsync(cancellationToken);
        }

        return Result.Success<HostResponse, HostErrors>(ToResponse(employee, true));
    }

    public async Task<Result<HostErrors>> RemoveHostAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        if (await GetAssignmentModeAsync(cancellationToken) is not HostAssignmentMode.AllowList)
            return Result.Failure(HostErrors.AssignmentModeDoesNotSupportAllowList);

        HostAssignment? hostAssignment = await visitorsDb.HostAssignments.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (hostAssignment is null)
            return Result.Failure(HostErrors.HostNotFound);

        visitorsDb.HostAssignments.Remove(hostAssignment);
        await visitorsDb.SaveChangesAsync(cancellationToken);
        return Result.Success<HostErrors>();
    }

    public async Task<Dictionary<Guid, HostResponse>> GetHostMapAsync(Guid[] employeeIds, CancellationToken cancellationToken = default)
    {
        if (employeeIds.Length == 0)
            return [];

        List<Employee> employees = await employeesDb.Employees.AsNoTracking()
            .Where(item => employeeIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        HashSet<Guid> allowListedEmployeeIds = await GetAllowListedEmployeeIdsAsync(employeeIds, cancellationToken);
        return employees.ToDictionary(item => item.Id, item => ToResponse(item, allowListedEmployeeIds.Contains(item.Id)));
    }

    private static IQueryable<Employee> ApplyQueryFilter(IQueryable<Employee> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        string filter = $"%{search.Trim()}%";
        return query.Where(employee =>
            EF.Functions.ILike(employee.FirstName, filter)
            || EF.Functions.ILike(employee.LastName, filter)
            || EF.Functions.ILike(employee.FirstName + " " + employee.LastName, filter)
            || employee.Email != null && EF.Functions.ILike(employee.Email, filter)
            || employee.EmployeeNumber != null && EF.Functions.ILike(employee.EmployeeNumber, filter)
            || employee.JobTitle != null && EF.Functions.ILike(employee.JobTitle, filter));
    }

    private async Task<HashSet<Guid>> GetAllowListedEmployeeIdsAsync(Guid[] employeeIds, CancellationToken cancellationToken)
    {
        if (employeeIds.Length == 0)
            return [];

        Guid[] ids = await visitorsDb.HostAssignments.AsNoTracking()
            .Where(item => employeeIds.Contains(item.EmployeeId))
            .Select(item => item.EmployeeId)
            .ToArrayAsync(cancellationToken);
        return ids.ToHashSet();
    }

    private async Task<HostAssignmentMode> GetAssignmentModeAsync(CancellationToken cancellationToken)
    {
        return await tenantsDb.Tenants.AsNoTracking()
            .Where(item => item.Id == tenantContext.TenantId)
            .Select(item => item.Configuration.Host.AssignmentMode)
            .SingleAsync(cancellationToken);
    }

    private static HostResponse ToResponse(Employee employee, bool isAllowListed) =>
        new(employee.Id, employee.FirstName, employee.LastName, employee.Email, isAllowListed);
}
