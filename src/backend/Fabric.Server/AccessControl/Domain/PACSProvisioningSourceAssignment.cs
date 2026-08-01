namespace Fabric.Server.AccessControl.Domain;

public sealed class PACSProvisioningSourceAssignment
{
    private PACSProvisioningSourceAssignment() { }

    public Guid PACSProvisioningId { get; private set; }
    public Guid PACSAssignmentId { get; private set; }

    public static PACSProvisioningSourceAssignment Create(Guid pacsProvisioningId, Guid pacsAssignmentId) =>
        new()
        {
            PACSProvisioningId = pacsProvisioningId,
            PACSAssignmentId = pacsAssignmentId
        };
}
