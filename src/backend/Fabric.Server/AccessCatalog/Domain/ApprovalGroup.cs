namespace Fabric.Server.AccessCatalog.Domain;

public sealed class ApprovalGroup
{
    private ApprovalGroup() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public ApprovalGroupStatus Status { get; private set; }

    public static ApprovalGroup Create(string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Status = ApprovalGroupStatus.Active
        };

    public void Update(string name, ApprovalGroupStatus status)
    {
        Name = name.Trim();
        Status = status;
    }
}
