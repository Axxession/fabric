using Fabric.Server.Core;

namespace Fabric.Server.AccessCatalog.Domain;

public sealed class ApprovalDefinition
{
    private ApprovalDefinition() { }

    public Guid Id { get; private set; }
    public Guid AccessItemId { get; private set; }
    public Guid? DestinationApprovalGroupId { get; private set; }
    public OrganizationalApprovalMode OrganizationalApprovalMode { get; private set; }
    public int OrganizationalApprovalLevels { get; private set; }

    public static Result<ApprovalDefinition, AccessCatalogErrors> Create(
        Guid accessItemId,
        Guid? destinationApprovalGroupId,
        OrganizationalApprovalMode organizationalApprovalMode,
        int organizationalApprovalLevels)
    {
        if (organizationalApprovalMode != OrganizationalApprovalMode.None && organizationalApprovalLevels <= 0)
            return Result.Failure<ApprovalDefinition, AccessCatalogErrors>(AccessCatalogErrors.InvalidOrganizationalApprovalLevels);

        return Result.Success<ApprovalDefinition, AccessCatalogErrors>(new ApprovalDefinition
        {
            Id = Guid.NewGuid(),
            AccessItemId = accessItemId,
            DestinationApprovalGroupId = destinationApprovalGroupId,
            OrganizationalApprovalMode = organizationalApprovalMode,
            OrganizationalApprovalLevels = organizationalApprovalMode == OrganizationalApprovalMode.None ? 0 : organizationalApprovalLevels
        });
    }

    public Result<AccessCatalogErrors> Update(
        Guid? destinationApprovalGroupId,
        OrganizationalApprovalMode organizationalApprovalMode,
        int organizationalApprovalLevels)
    {
        if (organizationalApprovalMode != OrganizationalApprovalMode.None && organizationalApprovalLevels <= 0)
            return Result.Failure(AccessCatalogErrors.InvalidOrganizationalApprovalLevels);

        DestinationApprovalGroupId = destinationApprovalGroupId;
        OrganizationalApprovalMode = organizationalApprovalMode;
        OrganizationalApprovalLevels = organizationalApprovalMode == OrganizationalApprovalMode.None ? 0 : organizationalApprovalLevels;
        return Result.Success<AccessCatalogErrors>();
    }
}
