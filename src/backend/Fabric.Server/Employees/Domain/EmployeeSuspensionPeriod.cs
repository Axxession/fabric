using Fabric.Server.Core;

namespace Fabric.Server.Employees.Domain;

public sealed class EmployeeSuspensionPeriod
{
    private EmployeeSuspensionPeriod() { }

    public Guid Id { get; internal set; }
    public Guid EmployeeId { get; internal set; }
    public DateOnly From { get; internal set; }
    public DateOnly Until { get; internal set; }
    public string? Reason { get; internal set; }

    internal static Result<EmployeeSuspensionPeriod, EmployeeErrors> Create(Guid employeeId, DateOnly from, DateOnly until, string? reason)
    {
        if (until < from)
            return Result.Failure<EmployeeSuspensionPeriod, EmployeeErrors>(EmployeeErrors.EmployeePeriodDateRangeInvalid);

        return Result.Success<EmployeeSuspensionPeriod, EmployeeErrors>(new EmployeeSuspensionPeriod
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            From = from,
            Until = until,
            Reason = NormalizeOptional(reason),
        });
    }

    internal Result<EmployeeErrors> Update(DateOnly from, DateOnly until, string? reason)
    {
        if (until < from)
            return Result.Failure(EmployeeErrors.EmployeePeriodDateRangeInvalid);

        From = from;
        Until = until;
        Reason = NormalizeOptional(reason);
        return Result.Success<EmployeeErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
