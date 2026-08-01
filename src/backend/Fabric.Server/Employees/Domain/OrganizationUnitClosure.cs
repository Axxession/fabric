namespace Fabric.Server.Employees.Domain;

public sealed class OrganizationUnitClosure
{
    private OrganizationUnitClosure() { }

    public Guid AncestorId { get; private set; }
    public Guid DescendantId { get; private set; }
    public int Depth { get; private set; }

    public static OrganizationUnitClosure Create(Guid ancestorId, Guid descendantId, int depth) =>
        new()
        {
            AncestorId = ancestorId,
            DescendantId = descendantId,
            Depth = depth,
        };
}
