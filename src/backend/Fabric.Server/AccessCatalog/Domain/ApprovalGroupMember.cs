namespace Fabric.Server.AccessCatalog.Domain;

public sealed class ApprovalGroupMember
{
    private ApprovalGroupMember() { }

    public Guid Id { get; private set; }
    public Guid ApprovalGroupId { get; private set; }
    public Guid IdentityId { get; private set; }
    public Guid ResponsibleLocationId { get; private set; }

    public static ApprovalGroupMember Create(Guid approvalGroupId, Guid identityId, Guid responsibleLocationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ApprovalGroupId = approvalGroupId,
            IdentityId = identityId,
            ResponsibleLocationId = responsibleLocationId
        };
}
