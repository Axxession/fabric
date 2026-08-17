using Fabric.Server.AccessCatalog.Application;
using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.AccessCatalog.Persistence;
using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Sagas.EmployeeLifecycle;

public sealed class EmployeeLifecycleAutomationService(
    SagasDbContext db,
    EmployeesDbContext employeesDb,
    IdentitiesDbContext identitiesDb,
    AccessCatalogDbContext accessCatalogDb,
    AccessGrantService accessGrantService,
    AccessControlDbContext accessControlDb,
    PACSSubjectProvisioningService pacsSubjectProvisioningService,
    EmployeeLifecycleAutomationTrigger trigger,
    TimeProvider timeProvider)
{
    public async Task<EmployeeLifecycleAutomationSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        EmployeeLifecycleAutomationSettings? settings = await db.EmployeeLifecycleAutomationSettings.SingleOrDefaultAsync(cancellationToken);
        return settings ?? EmployeeLifecycleAutomationSettings.Default;
    }

    public async Task<EmployeeLifecycleAutomationSettings> UpdateSettingsAsync(bool isEnabled, bool disableEmployeeOnLeave, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        EmployeeLifecycleAutomationSettings? settings = await db.EmployeeLifecycleAutomationSettings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new EmployeeLifecycleAutomationSettings { Id = Guid.NewGuid() };
            db.EmployeeLifecycleAutomationSettings.Add(settings);
        }

        bool wasEnabled = settings.IsEnabled;
        settings.IsEnabled = isEnabled;
        settings.DisableEmployeeOnLeave = disableEmployeeOnLeave;

        if (wasEnabled && !isEnabled)
            settings.DisabledAt = now;
        else if (!wasEnabled && isEnabled)
        {
            settings.ReenabledAt = now;
            settings.LastFullReconciledAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task EnqueueAsync(Guid employeeId, string reason, CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        EmployeeAccessAutomationReconciliation? existing = await db.EmployeeAccessAutomationReconciliations.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (existing is null)
            db.EmployeeAccessAutomationReconciliations.Add(EmployeeAccessAutomationReconciliation.Create(employeeId, reason, now, now));
        else
            existing.RescheduleNow(reason, now);

        await db.SaveChangesAsync(cancellationToken);
        trigger.Notify();
    }

    public async Task<IReadOnlyList<EmployeeAccessAutomationWorkItem>> GetDueWorkItemsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        return await db.EmployeeAccessAutomationReconciliations
            .IgnoreQueryFilters()
            .Where(item => item.ScheduledFor <= now)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => new EmployeeAccessAutomationWorkItem(
                EF.Property<string>(item, Infrastructure.Tenancy.TenantDbContext.TenantIdPropertyName),
                item.EmployeeId,
                item.Reason))
            .ToListAsync(cancellationToken);
    }

    public async Task ReconcileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        EmployeeAccessAutomationReconciliation? reconciliation = await db.EmployeeAccessAutomationReconciliations.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (reconciliation is null)
            return;

        try
        {
            EmployeeLifecycleAutomationSettings settings = await GetSettingsAsync(cancellationToken);
            if (!settings.IsEnabled)
            {
                db.EmployeeAccessAutomationReconciliations.Remove(reconciliation);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            Employee? employee = await employeesDb.Employees
                .Include(item => item.WorkLocations)
                .Include(item => item.Personas)
                .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
            if (employee is null)
            {
                db.EmployeeAccessAutomationReconciliations.Remove(reconciliation);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            EmployeeLifecycleState? lifecycleState = await employeesDb.EmployeeLifecycleStates.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
            EmployeeStatus currentStatus = lifecycleState?.CurrentStatus ?? EmployeeStatus.Active;

            await ReconcileAccessGrantsAsync(employee, cancellationToken);
            await ReconcilePacsSubjectsAsync(employee, currentStatus, settings.DisableEmployeeOnLeave, cancellationToken);

            db.EmployeeAccessAutomationReconciliations.Remove(reconciliation);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();
            reconciliation.MarkFailed(ex.Message, GetRetryAt(reconciliation.AttemptCount + 1, now), now);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<Result<OrganizationalUnitPackageRule, string>> CreateOuRuleAsync(Guid organizationUnitId, Guid packageId, CancellationToken cancellationToken = default)
    {
        if (!await employeesDb.OrganizationUnits.AnyAsync(item => item.Id == organizationUnitId, cancellationToken))
            return Result.Failure<OrganizationalUnitPackageRule, string>("Organization unit not found.");
        if (!await accessCatalogDb.Packages.AnyAsync(item => item.Id == packageId, cancellationToken))
            return Result.Failure<OrganizationalUnitPackageRule, string>("Package not found.");
        bool exists = await db.OrganizationalUnitPackageRules.AnyAsync(item => item.OrganizationUnitId == organizationUnitId && item.PackageId == packageId, cancellationToken);
        if (exists)
            return Result.Failure<OrganizationalUnitPackageRule, string>("Rule already exists.");

        OrganizationalUnitPackageRule rule = new() { Id = Guid.NewGuid(), OrganizationUnitId = organizationUnitId, PackageId = packageId, IsEnabled = true };
        db.OrganizationalUnitPackageRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<OrganizationalUnitPackageRule, string>(rule);
    }

    public async Task<Result<PersonaPackageRule, string>> CreatePersonaRuleAsync(Guid personaId, Guid packageId, CancellationToken cancellationToken = default)
    {
        if (!await employeesDb.Personas.AnyAsync(item => item.Id == personaId, cancellationToken))
            return Result.Failure<PersonaPackageRule, string>("Persona not found.");
        if (!await accessCatalogDb.Packages.AnyAsync(item => item.Id == packageId, cancellationToken))
            return Result.Failure<PersonaPackageRule, string>("Package not found.");
        bool exists = await db.PersonaPackageRules.AnyAsync(item => item.PersonaId == personaId && item.PackageId == packageId, cancellationToken);
        if (exists)
            return Result.Failure<PersonaPackageRule, string>("Rule already exists.");

        PersonaPackageRule rule = new() { Id = Guid.NewGuid(), PersonaId = personaId, PackageId = packageId, IsEnabled = true };
        db.PersonaPackageRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<PersonaPackageRule, string>(rule);
    }

    public async Task<bool> DeleteOuRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        OrganizationalUnitPackageRule? rule = await db.OrganizationalUnitPackageRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return false;
        db.OrganizationalUnitPackageRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeletePersonaRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        PersonaPackageRule? rule = await db.PersonaPackageRules.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (rule is null)
            return false;
        db.PersonaPackageRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ToggleOuRuleAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        OrganizationalUnitPackageRule? rule = await db.OrganizationalUnitPackageRules.SingleAsync(item => item.Id == id, cancellationToken);
        rule.IsEnabled = isEnabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task TogglePersonaRuleAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default)
    {
        PersonaPackageRule? rule = await db.PersonaPackageRules.SingleAsync(item => item.Id == id, cancellationToken);
        rule.IsEnabled = isEnabled;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ReconcileAccessGrantsAsync(Employee employee, CancellationToken cancellationToken)
    {
        Guid[] locationIds = employee.WorkLocations.Select(item => item.LocationId).Distinct().ToArray();

        List<DesiredGrant> desired = [];
        OrganizationalUnitPackageRule[] ouRules = await db.OrganizationalUnitPackageRules
            .Where(item => item.OrganizationUnitId == employee.OrganizationUnitId && item.IsEnabled)
            .ToArrayAsync(cancellationToken);
        desired.AddRange(ouRules.SelectMany(rule => locationIds.Select(locationId => new DesiredGrant(rule.PackageId, AssignmentSourceKind.OrganizationalUnit, rule.OrganizationUnitId, locationId))));

        Guid[] personaIds = employee.Personas.Select(item => item.PersonaId).ToArray();
        PersonaPackageRule[] personaRules = await db.PersonaPackageRules
            .Where(item => personaIds.Contains(item.PersonaId) && item.IsEnabled)
            .ToArrayAsync(cancellationToken);
        desired.AddRange(personaRules.SelectMany(rule => locationIds.Select(locationId => new DesiredGrant(rule.PackageId, AssignmentSourceKind.Persona, rule.PersonaId, locationId))));

        AccessGrant[] existing = await accessCatalogDb.AccessGrants
            .Where(item => item.IdentityId == employee.IdentityId)
            .Where(item => item.AssignmentChannel == AssignmentChannel.AutomaticConfiguration)
            .Where(item => item.SourceKind == AssignmentSourceKind.OrganizationalUnit || item.SourceKind == AssignmentSourceKind.Persona)
            .ToArrayAsync(cancellationToken);

        foreach (DesiredGrant desiredGrant in desired)
        {
            AccessGrant? match = existing.SingleOrDefault(item =>
                item.PackageId == desiredGrant.PackageId &&
                item.SourceKind == desiredGrant.SourceKind &&
                item.SourceId == desiredGrant.SourceId &&
                item.Status == AccessGrantStatus.Active &&
                item.LocationId == desiredGrant.LocationId);

            if (match is not null)
                continue;

            Result<IReadOnlyList<AccessGrant>, AccessCatalogErrors> result = await accessGrantService.CreateAsync(
                desiredGrant.PackageId,
                employee.IdentityId,
                desiredGrant.LocationId,
                AssignmentChannel.AutomaticConfiguration,
                desiredGrant.SourceKind,
                desiredGrant.SourceId,
                AccessDurationKind.Permanent,
                timeProvider.GetUtcNow(),
                null,
                $"Automatic employee access from {desiredGrant.SourceKind}.",
                cancellationToken);

            if (result.IsFailure(out AccessCatalogErrors error))
                throw new InvalidOperationException($"Failed to create automatic access grant: {error}.");
        }

        foreach (AccessGrant grant in existing.Where(item => item.Status == AccessGrantStatus.Active))
        {
            bool stillDesired = desired.Any(desiredGrant =>
                desiredGrant.PackageId == grant.PackageId &&
                desiredGrant.SourceKind == grant.SourceKind &&
                desiredGrant.SourceId == grant.SourceId &&
                desiredGrant.LocationId == grant.LocationId);

            if (stillDesired)
                continue;

            Result<AccessGrant, AccessCatalogErrors> revoke = await accessGrantService.RevokeAsync(grant.Id, AccessGrantRevokeCause.EmployeeLifecycleAutomation, "Employee lifecycle automation", cancellationToken);
            if (revoke.IsFailure(out AccessCatalogErrors error))
                throw new InvalidOperationException($"Failed to revoke automatic access grant: {error}.");
        }
    }

    private async Task ReconcilePacsSubjectsAsync(Employee employee, EmployeeStatus status, bool disableEmployeeOnLeave, CancellationToken cancellationToken)
    {
        PACSSubject[] subjects = await accessControlDb.PACSSubjects
            .Where(item => item.IdentityId == employee.IdentityId)
            .ToArrayAsync(cancellationToken);

        if (subjects.Length == 0)
            return;

        Identity? identity = await identitiesDb.Identities.SingleOrDefaultAsync(item => item.Id == employee.IdentityId, cancellationToken);
        string firstName = identity?.FirstName ?? employee.FirstName;
        string lastName = identity?.LastName ?? employee.LastName;
        string? email = identity?.Email ?? employee.Email;

        PACSSubjectState? desiredState = status switch
        {
            EmployeeStatus.Active => PACSSubjectState.Active,
            EmployeeStatus.Leave when disableEmployeeOnLeave => PACSSubjectState.Blocked,
            EmployeeStatus.Leave => null,
            EmployeeStatus.Terminated => PACSSubjectState.Archived,
            EmployeeStatus.Archived => PACSSubjectState.Archived,
            _ => PACSSubjectState.Blocked
        };

        if (!desiredState.HasValue)
            return;

        PACSSubjectProvisioningReason reason = status switch
        {
            EmployeeStatus.Leave => PACSSubjectProvisioningReason.EmployeeLeave,
            EmployeeStatus.Suspended => PACSSubjectProvisioningReason.EmployeeSuspension,
            EmployeeStatus.Active => PACSSubjectProvisioningReason.EmployeeLifecycleRestored,
            _ => PACSSubjectProvisioningReason.ArchiveRequested
        };

        foreach (PACSSubject subject in subjects)
        {
            _ = await pacsSubjectProvisioningService.UpsertAsync(
                employee.IdentityId,
                subject.AccessControlSystemId,
                desiredState.Value,
                firstName,
                lastName,
                email,
                reason,
                PACSSubjectProvisioningSourceKind.EmployeeLifecycleSaga,
                employee.Id,
                cancellationToken);
        }
    }

    private static DateTimeOffset GetRetryAt(int attemptCount, DateTimeOffset now)
    {
        TimeSpan delay = attemptCount switch
        {
            <= 1 => TimeSpan.FromMinutes(1),
            2 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(15)
        };

        return now.Add(delay);
    }

    private sealed record DesiredGrant(Guid PackageId, AssignmentSourceKind SourceKind, Guid SourceId, Guid LocationId);
}
