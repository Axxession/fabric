namespace Fabric.Server.Employees.Domain;

public sealed class EmployeeWorkLocation
{
    private EmployeeWorkLocation() { }

    public Guid EmployeeId { get; internal set; }
    public Guid LocationId { get; internal set; }
    public bool IsPrimary { get; internal set; }

    internal static EmployeeWorkLocation Create(Guid employeeId, Guid locationId, bool isPrimary) =>
        new()
        {
            EmployeeId = employeeId,
            LocationId = locationId,
            IsPrimary = isPrimary,
        };
}
