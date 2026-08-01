namespace Fabric.Server.AccessCatalog.Domain;

public sealed class PackageRequestScope
{
    private PackageRequestScope() { }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid ApprovalFlowId { get; private set; }
    public Guid RequestedLocationId { get; private set; }

    public static PackageRequestScope Create(Guid requestId, Guid approvalFlowId, Guid requestedLocationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            ApprovalFlowId = approvalFlowId,
            RequestedLocationId = requestedLocationId
        };
}
