namespace Fabric.Server.AccessControl.Domain;

public sealed class AccessItem
{
    private AccessItem() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public AccessItemStatus Status { get; private set; }

    public static AccessItem Create(string name, string? description) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Status = AccessItemStatus.Active
        };

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetStatus(AccessItemStatus status) => Status = status;
}
