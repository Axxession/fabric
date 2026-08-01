namespace Fabric.Server.Employees.Domain;

public sealed class EmployeePersona
{
    private EmployeePersona() { }

    public Guid EmployeeId { get; internal set; }
    public Guid PersonaId { get; internal set; }

    internal static EmployeePersona Create(Guid employeeId, Guid personaId) =>
        new()
        {
            EmployeeId = employeeId,
            PersonaId = personaId,
        };
}
