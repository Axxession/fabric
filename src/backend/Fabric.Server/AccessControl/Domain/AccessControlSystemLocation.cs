namespace Fabric.Server.AccessControl.Domain;

public sealed class AccessControlSystemLocation
{
    private AccessControlSystemLocation() { }

    public Guid Id { get; private set; }
    public Guid AccessControlSystemId { get; private set; }
    public Guid LocationId { get; private set; }

    public static AccessControlSystemLocation Create(Guid accessControlSystemId, Guid locationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccessControlSystemId = accessControlSystemId,
            LocationId = locationId
        };
}
