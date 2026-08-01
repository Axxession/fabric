using Fabric.Server.Employees.Domain;

namespace Fabric.Server.Employees.Application;

public static class EmployeeLifecycleCalculator
{
    public static EmployeeStatus Calculate(Employee employee, DateOnly today)
    {
        if (employee.ArchivedAt.HasValue)
            return EmployeeStatus.Archived;

        if (employee.SuspensionPeriods.Any(period => IsActive(period.From, period.Until, today)))
            return EmployeeStatus.Suspended;

        if (employee.LeavePeriods.Any(period => IsActive(period.From, period.Until, today)))
            return EmployeeStatus.Leave;

        if (employee.ContractStartDate.HasValue && today < employee.ContractStartDate.Value)
            return EmployeeStatus.PreHire;

        if (employee.ContractEndDate.HasValue && employee.ContractEndDate.Value < today)
            return EmployeeStatus.Terminated;

        return EmployeeStatus.Active;
    }

    public static bool IsActive(DateOnly from, DateOnly until, DateOnly today) =>
        from <= today && today <= until;
}
