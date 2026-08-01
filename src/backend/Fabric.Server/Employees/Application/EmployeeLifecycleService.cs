using Fabric.Server.Employees.Domain;
using Fabric.Server.Employees.Persistence;
using Fabric.Server.Sagas.EmployeeLifecycle;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Employees.Application;

public sealed record EmployeeLifecycleRecalculationWorkItem(Guid RecalculationId, string TenantId, Guid EmployeeId, string Reason);

public sealed class EmployeeLifecycleService(
    EmployeesDbContext db,
    TimeProvider timeProvider,
    EmployeeLifecycleTrigger trigger,
    EmployeeLifecycleAutomationService employeeLifecycleAutomationService)
{
    public async Task ReconcileNowAndRescheduleAsync(
        Guid employeeId,
        EmployeeLifecycleSource source,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        Employee? employee = await db.Employees
            .Include(item => item.LeavePeriods)
            .Include(item => item.SuspensionPeriods)
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken);
        if (employee is null)
            return;

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);
        EmployeeStatus nextStatus = EmployeeLifecycleCalculator.Calculate(employee, today);

        EmployeeLifecycleState? state = await db.EmployeeLifecycleStates.SingleOrDefaultAsync(item => item.EmployeeId == employeeId, cancellationToken);
        if (state is null)
        {
            db.EmployeeLifecycleStates.Add(new EmployeeLifecycleState
            {
                EmployeeId = employeeId,
                CurrentStatus = nextStatus,
                EffectiveAt = now,
                LastEvaluatedAt = now,
            });
        }
        else
        {
            if (state.CurrentStatus != nextStatus)
            {
                db.EmployeeLifecycleEvents.Add(EmployeeLifecycleEvent.Create(
                    employee.Id,
                    employee.IdentityId,
                    state.CurrentStatus,
                    nextStatus,
                    now,
                    source,
                    reason,
                    now));

                state.CurrentStatus = nextStatus;
                state.EffectiveAt = now;
            }

            state.LastEvaluatedAt = now;
        }

        await RebuildFutureScheduleAsync(employee, now, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await employeeLifecycleAutomationService.EnqueueAsync(employee.Id, reason ?? source.ToString(), cancellationToken);
        trigger.Notify();
    }

    public async Task<DateTimeOffset?> GetNextScheduledForAsync(CancellationToken cancellationToken = default)
    {
        return await db.EmployeeLifecycleRecalculations
            .IgnoreQueryFilters()
            .Where(item => item.Status == EmployeeLifecycleRecalculationStatus.Pending)
            .OrderBy(item => item.ScheduledFor)
            .Select(item => (DateTimeOffset?)item.ScheduledFor)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLifecycleRecalculationWorkItem>> ClaimDueRecalculationsAsync(
        int batchSize,
        TimeSpan processingTimeout,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset staleBefore = now - processingTimeout;

        List<EmployeeLifecycleRecalculation> stale = await db.EmployeeLifecycleRecalculations
            .IgnoreQueryFilters()
            .Where(item => item.Status == EmployeeLifecycleRecalculationStatus.Processing
                && item.ProcessingStartedAt.HasValue
                && item.ProcessingStartedAt.Value < staleBefore)
            .ToListAsync(cancellationToken);

        foreach (EmployeeLifecycleRecalculation item in stale)
        {
            item.Status = EmployeeLifecycleRecalculationStatus.Pending;
            item.ProcessingStartedAt = null;
        }

        List<EmployeeLifecycleRecalculation> due = await db.EmployeeLifecycleRecalculations
            .IgnoreQueryFilters()
            .Where(item => item.Status == EmployeeLifecycleRecalculationStatus.Pending && item.ScheduledFor <= now)
            .OrderBy(item => item.ScheduledFor)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (EmployeeLifecycleRecalculation item in due)
        {
            item.Status = EmployeeLifecycleRecalculationStatus.Processing;
            item.ProcessingStartedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);

        return due
            .Select(item => new EmployeeLifecycleRecalculationWorkItem(
                item.Id,
                db.Entry(item).Property<string>("TenantId").CurrentValue!,
                item.EmployeeId,
                item.Reason))
            .ToArray();
    }

    public async Task MarkCompletedAsync(Guid recalculationId, CancellationToken cancellationToken = default)
    {
        EmployeeLifecycleRecalculation? item = await db.EmployeeLifecycleRecalculations
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(row => row.Id == recalculationId, cancellationToken);
        if (item is null)
            return;

        item.Status = EmployeeLifecycleRecalculationStatus.Completed;
        item.CompletedAt = timeProvider.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetToPendingAsync(Guid recalculationId, CancellationToken cancellationToken = default)
    {
        EmployeeLifecycleRecalculation? item = await db.EmployeeLifecycleRecalculations
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(row => row.Id == recalculationId, cancellationToken);
        if (item is null)
            return;

        item.Status = EmployeeLifecycleRecalculationStatus.Pending;
        item.ProcessingStartedAt = null;
        await db.SaveChangesAsync(cancellationToken);
        trigger.Notify();
    }

    private async Task RebuildFutureScheduleAsync(Employee employee, DateTimeOffset now, CancellationToken cancellationToken)
    {
        List<EmployeeLifecycleRecalculation> pending = await db.EmployeeLifecycleRecalculations
            .Where(item => item.EmployeeId == employee.Id && item.Status == EmployeeLifecycleRecalculationStatus.Pending)
            .ToListAsync(cancellationToken);
        db.EmployeeLifecycleRecalculations.RemoveRange(pending);

        foreach (DateTimeOffset scheduledFor in BuildFutureInstants(employee).Distinct().Where(item => item > now))
            db.EmployeeLifecycleRecalculations.Add(EmployeeLifecycleRecalculation.Create(employee.Id, scheduledFor, "BoundaryReached"));
    }

    private static IEnumerable<DateTimeOffset> BuildFutureInstants(Employee employee)
    {
        if (employee.ContractStartDate.HasValue)
            yield return ToUtcStartOfDay(employee.ContractStartDate.Value);

        if (employee.ContractEndDate.HasValue)
            yield return ToUtcStartOfDay(employee.ContractEndDate.Value.AddDays(1));

        foreach (EmployeeLeavePeriod period in employee.LeavePeriods)
        {
            yield return ToUtcStartOfDay(period.From);
            yield return ToUtcStartOfDay(period.Until.AddDays(1));
        }

        foreach (EmployeeSuspensionPeriod period in employee.SuspensionPeriods)
        {
            yield return ToUtcStartOfDay(period.From);
            yield return ToUtcStartOfDay(period.Until.AddDays(1));
        }
    }

    private static DateTimeOffset ToUtcStartOfDay(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
}
