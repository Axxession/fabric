namespace Fabric.Server.Employees.Domain;

public enum EmployeeLifecycleSource
{
    EmployeeCreated,
    EmployeeUpdated,
    EmployeeArchived,
    EmployeeUnarchived,
    LeavePeriodAdded,
    LeavePeriodUpdated,
    LeavePeriodRemoved,
    SuspensionPeriodAdded,
    SuspensionPeriodUpdated,
    SuspensionPeriodRemoved,
    ScheduledRecalculation,
}
