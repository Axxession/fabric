namespace Fabric.Server.AccessCatalog.Domain;

public enum CatalogStatus
{
    Active,
    Inactive
}

public enum PackageStatus
{
    Active,
    Inactive
}

public enum AccessGrantStatus
{
    Active,
    Revoked
}

public enum AccessDurationKind
{
    Permanent,
    Temporary
}

public enum AssignmentChannel
{
    CatalogRequest,
    AutomaticConfiguration,
    Manual
}

public enum AssignmentSourceKind
{
    CatalogRequest,
    OrganizationalUnit,
    Persona,
    VisitorLocation,
    Manual
}

public enum PackageRequestStatus
{
    InProgress,
    Completed
}

public enum PackageRequestSubStatus
{
    Approved,
    PartiallyApproved,
    Rejected,
    Expired
}

public enum ApprovalFlowStatus
{
    InProgress,
    Approved,
    Rejected,
    SystemApproved,
    Expired
}

public enum ApprovalGroupStatus
{
    Active,
    Inactive
}

public enum ApprovalRequirementType
{
    Destination,
    Organizational
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    SystemApproved
}

public enum ApprovalDecisionKind
{
    Approve,
    Reject
}

public enum ApprovalDecisionRole
{
    FacilityManager,
    L1,
    L2,
    L3
}

public enum OrganizationalApprovalMode
{
    None,
    ManagerChain
}
