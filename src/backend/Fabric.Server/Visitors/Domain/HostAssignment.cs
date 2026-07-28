namespace Fabric.Server.Visitors.Domain;

public sealed class HostAssignment
{
    private HostAssignment() { }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }

    public static HostAssignment Create(Guid employeeId) =>
        new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId
        };
}
