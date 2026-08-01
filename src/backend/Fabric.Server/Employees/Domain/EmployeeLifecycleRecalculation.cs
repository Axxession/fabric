namespace Fabric.Server.Employees.Domain;

public sealed class EmployeeLifecycleRecalculation
{
    private EmployeeLifecycleRecalculation() { }

    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public string Reason { get; set; } = null!;
    public EmployeeLifecycleRecalculationStatus Status { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public static EmployeeLifecycleRecalculation Create(Guid employeeId, DateTimeOffset scheduledFor, string reason) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            ScheduledFor = scheduledFor,
            Reason = reason,
            Status = EmployeeLifecycleRecalculationStatus.Pending,
        };
}
