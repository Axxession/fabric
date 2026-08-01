namespace Fabric.Server.Employees.Domain;

public sealed class EmployeeLifecycleState
{
    public EmployeeLifecycleState() { }

    public Guid EmployeeId { get; set; }
    public EmployeeStatus CurrentStatus { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public DateTimeOffset LastEvaluatedAt { get; set; }
}
