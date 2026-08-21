using Fabric.Server.AccessCatalog.Domain;
using Fabric.Server.Core;
using Fabric.Server.Requirements.Contracts;
using Fabric.Server.Requirements.Domain;
using Fabric.Server.Sagas.AccessGrantProvisioning;

namespace Fabric.Server.AccessCatalog.Contracts;

public sealed record ListCatalogsRequest : BaseListRequest
{
    public string? Name { get; set; }
}

public sealed record ListApprovalGroupsRequest : BaseListRequest
{
    public string? Name { get; set; }
}

public sealed record ListPackagesRequest : BaseListRequest
{
    public string? Name { get; set; }
}

public sealed record ListAccessGrantsRequest : BaseListRequest
{
    public Guid? IdentityId { get; set; }
    public Guid? PackageId { get; set; }
    public AccessGrantStatus? Status { get; set; }
    public AssignmentSourceKind? SourceKind { get; set; }
    public Guid? SourceId { get; set; }
}

public sealed record AssignmentContextRequest(AssignmentSourceKind SourceKind, Guid SourceId);

public sealed record GrantComplianceSummaryResponse(
    AssignmentSourceKind SourceKind,
    Guid SourceId,
    GrantComplianceStatus? ComplianceStatus,
    DateTimeOffset? CompliantUntil,
    int GrantCount);

public sealed record ContextAssignedPackageGrantResponse(
    Guid GrantId,
    Guid AccessItemId,
    string AccessItemName,
    AccessGrantStatus Status,
    GrantApprovalStatus ApprovalStatus,
    GrantComplianceStatus ComplianceStatus,
    GrantProvisioningStatus ProvisioningStatus,
    DateTimeOffset? CompliantUntil,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    string? RevokedBy,
    AccessGrantRevokeCause? RevokeCause);

public sealed record ContextAssignedPackageResponse(
    Guid PackageId,
    string PackageName,
    ContextAssignedPackageGrantResponse[] Grants);

public sealed record ContextAssignedPackagesResponse(
    AssignmentSourceKind SourceKind,
    Guid SourceId,
    ContextAssignedPackageResponse[] Packages);

public sealed record GrantComplianceDetailResponse(
    AssignmentSourceKind SourceKind,
    Guid SourceId,
    GrantComplianceStatus? ComplianceStatus,
    DateTimeOffset? CompliantUntil,
    RequirementComplianceResponse[] Requirements);

public sealed record ListPackageRequestsRequest : BaseListRequest
{
    public Guid? RequesterIdentityId { get; set; }
    public Guid? BeneficiaryIdentityId { get; set; }
    public PackageRequestStatus? Status { get; set; }
}

public sealed record ListApprovalRequirementsRequest : BaseListRequest
{
    public Guid? RequestId { get; set; }
    public Guid? RequiredApproverIdentityId { get; set; }
    public Guid? ApprovalGroupId { get; set; }
    public ApprovalStatus? Status { get; set; }
}

public sealed record ListApprovalInboxRequest : BaseListRequest;

public sealed record CreateCatalogRequest(string Name, string? Description);
public sealed record UpdateCatalogRequest(string Name, string? Description, CatalogStatus Status);
public sealed record LinkCatalogPackageRequest(Guid PackageId, bool IsRequestable);
public sealed record CreateApprovalGroupRequest(string Name);
public sealed record UpdateApprovalGroupRequest(string Name, ApprovalGroupStatus Status);
public sealed record CreateApprovalGroupMemberRequest(Guid IdentityId, Guid ResponsibleLocationId);

public sealed record CreatePackageRequest(string Name, string? Description);
public sealed record UpdatePackageRequest(string Name, string? Description, PackageStatus Status);
public sealed record AddPackageAccessItemRequest(Guid AccessItemId);
public sealed record CreateApprovalDefinitionRequest(Guid AccessItemId, Guid? DestinationApprovalGroupId, OrganizationalApprovalMode OrganizationalApprovalMode, int OrganizationalApprovalLevels);
public sealed record UpdateApprovalDefinitionRequest(Guid? DestinationApprovalGroupId, OrganizationalApprovalMode OrganizationalApprovalMode, int OrganizationalApprovalLevels);
public sealed record PreviewPackageRequestApprovalsRequest(Guid PackageId, Guid BeneficiaryIdentityId, Guid[] LocationIds, AccessDurationKind DurationKind, DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil);
public sealed record CreatePackageRequestRequest(Guid PackageId, Guid RequesterIdentityId, Guid BeneficiaryIdentityId, Guid[] LocationIds, string RequestReason, AccessDurationKind DurationKind, DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil);
public sealed record CreateApprovalDecisionRequest(Guid ApproverIdentityId, ApprovalDecisionKind DecisionKind, string? Note);

public sealed record CreateAccessGrantRequest(
    Guid PackageId,
    Guid IdentityId,
    Guid LocationId,
    AssignmentChannel AssignmentChannel,
    AssignmentSourceKind SourceKind,
    Guid SourceId,
    AccessDurationKind DurationKind,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    string ReasonText);

public sealed record CatalogResponse(Guid Id, string Name, string? Description, CatalogStatus Status);
public sealed record CatalogPackageResponse(Guid CatalogId, Guid PackageId, bool IsRequestable);
public sealed record PackageResponse(Guid Id, string Name, string? Description, PackageStatus Status);
public sealed record PackageAccessItemResponse(Guid PackageId, Guid AccessItemId);
public sealed record ApprovalGroupResponse(Guid Id, string Name, ApprovalGroupStatus Status);
public sealed record ApprovalGroupMemberResponse(Guid Id, Guid ApprovalGroupId, Guid IdentityId, Guid ResponsibleLocationId);
public sealed record ApprovalDefinitionResponse(Guid Id, Guid AccessItemId, Guid? DestinationApprovalGroupId, OrganizationalApprovalMode OrganizationalApprovalMode, int OrganizationalApprovalLevels);
public sealed record ApprovalRequirementResponse(Guid Id, Guid ApprovalFlowId, Guid RequestId, Guid AccessItemId, Guid LocationId, ApprovalRequirementType Type, ApprovalDecisionRole Role, Guid? ApprovalGroupId, Guid? RequiredApproverIdentityId, ApprovalStatus Status, string? SystemApprovalReason, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
public sealed record ApprovalRequirementPreviewApprovalGroupResponse(Guid Id, string Name);
public sealed record ApprovalRequirementPreviewApproverIdentityResponse(Guid Id, string DisplayName, string? Email);
public sealed record ApprovalRequirementPreviewResponse(Guid LocationId, ApprovalRequirementType Type, ApprovalDecisionRole Role, ApprovalRequirementPreviewApprovalGroupResponse? ApprovalGroup, ApprovalRequirementPreviewApproverIdentityResponse? ApproverIdentity);
public sealed record ApprovalRequirementsPreviewAccessItemResponse(Guid AccessItemId, string Name, string? Description, bool IsComplianceRequired, ApprovalRequirementPreviewResponse[] Requirements);
public sealed record ContextComplianceRequirementResponse(Guid RequirementDefinitionId, string Code, string Name, bool IsBlocking, RequirementResultStatus Status, string Reason, DateTimeOffset? ValidUntil);
public sealed record ContextComplianceLocationResponse(Guid LocationId, string LocationLabel, ContextComplianceStatus Status, DateTimeOffset? CompliantUntil, ContextComplianceRequirementResponse[] Requirements);
public sealed record PackageRequestPreviewResponse(ApprovalRequirementsPreviewAccessItemResponse[] Approvals, ContextComplianceLocationResponse[] ContextCompliance);
public sealed record ApprovalDecisionResponse(Guid Id, Guid RequestId, Guid ApprovalRequirementId, Guid ApproverIdentityId, ApprovalDecisionRole Role, ApprovalDecisionKind DecisionKind, string? Note, DateTimeOffset DecidedAt);
public sealed record PackageRequestResponse(Guid Id, Guid PackageId, Guid RequesterIdentityId, Guid BeneficiaryIdentityId, string RequestReason, PackageRequestStatus Status, PackageRequestSubStatus? SubStatus, AccessDurationKind DurationKind, DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, DateTimeOffset? DecidedAt, Guid[] LocationIds);
public sealed record PackageRequestDetailLocationResponse(Guid Id, string Label, Guid SiteId, string SiteName);
public sealed record ApprovalInboxItemResponse(Guid ApprovalRequirementId, Guid ApprovalFlowId, Guid RequestId, Guid PackageId, string PackageName, Guid BeneficiaryIdentityId, string BeneficiaryDisplayName, Guid RequesterIdentityId, string RequesterDisplayName, Guid AccessItemId, string AccessItemName, Guid SiteId, string SiteName, string[] RequestedLocationLabels, ApprovalRequirementType Type, ApprovalDecisionRole Role, string? ApprovalGroupName, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, ApprovalStatus Status);
public sealed record PackageRequestDetailGrantResponse(Guid Id, Guid AccessItemId, string AccessItemName, Guid LocationId, string LocationLabel, AccessGrantStatus Status, GrantApprovalStatus ApprovalStatus, GrantComplianceStatus ComplianceStatus, GrantProvisioningStatus ProvisioningStatus, DateTimeOffset? CompliantUntil, DateTimeOffset ValidFrom, DateTimeOffset? ValidUntil);
public sealed record PackageRequestDetailDecisionResponse(Guid Id, string ApproverDisplayName, ApprovalDecisionRole Role, ApprovalDecisionKind DecisionKind, string? Note, DateTimeOffset DecidedAt);
public sealed record PackageRequestDetailRequirementResponse(Guid Id, ApprovalRequirementType Type, ApprovalDecisionRole Role, Guid? ApprovalGroupId, string? ApprovalGroupName, Guid? RequiredApproverIdentityId, string? RequiredApproverDisplayName, ApprovalStatus Status, string? SystemApprovalReason, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, PackageRequestDetailDecisionResponse[] Decisions);
public sealed record PackageRequestDetailFlowResponse(Guid ApprovalFlowId, Guid AccessItemId, string AccessItemName, string? AccessItemDescription, Guid SiteId, string SiteName, ApprovalFlowStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt, PackageRequestDetailLocationResponse[] RequestedLocations, PackageRequestDetailRequirementResponse[] Requirements, PackageRequestDetailGrantResponse[] Grants);
public sealed record PackageRequestDetailResponse(PackageRequestResponse Request, PackageResponse Package, PackageRequestDetailLocationResponse[] RequestedLocations, PackageRequestDetailFlowResponse[] Flows, PackageRequestDetailGrantResponse[] Grants);
public sealed record GrantRequirementResponse(Guid Id, Guid RequirementDefinitionId, string SourcePolicyKind, Guid SourcePolicyId, bool IsBlocking, DateTimeOffset DerivedAt);
public sealed record GrantRequirementResultResponse(Guid Id, Guid RequirementDefinitionId, RequirementResultStatus Status, RequirementEvidenceKind? EvidenceKind, string? EvidenceReference, string Reason, DateTimeOffset? ValidUntil, DateTimeOffset LastEvaluatedAt);
public sealed record RecalculateGrantRequirementsResponse(int GrantsProcessed, bool FutureOnly);
public sealed record AccessGrantMaterializationOutcomeResponse(
    Guid Id,
    Guid AccessItemId,
    Guid LocationId,
    AccessGrantMaterializationOutcomeStatus Status,
    string? FailureReason);

public sealed record CreateAccessGrantResponse(AccessGrantResponse[] Grants);

public sealed record AccessGrantResponse(
    Guid Id,
    Guid PackageId,
    Guid AccessItemId,
    Guid IdentityId,
    AssignmentChannel AssignmentChannel,
    AssignmentSourceKind SourceKind,
    Guid SourceId,
    Guid? ApprovalFlowId,
    Guid? RequestScopeId,
    Guid LocationId,
    AccessDurationKind DurationKind,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    AccessGrantStatus Status,
    Guid? ReplacedById,
    GrantApprovalStatus ApprovalStatus,
    GrantComplianceStatus ComplianceStatus,
    GrantProvisioningStatus ProvisioningStatus,
    DateTimeOffset? CompliantUntil,
    DateTimeOffset? LastComplianceEvaluatedAt,
    string ReasonText,
    string? RevokedBy,
    AccessGrantRevokeCause? RevokeCause,
    GrantRequirementResponse[] Requirements,
    GrantRequirementResultResponse[] RequirementResults,
    AccessGrantMaterializationOutcomeResponse[] MaterializationOutcomes);

public static class AccessCatalogMapper
{
    public static CatalogResponse ToResponse(this Catalog catalog) =>
        new(catalog.Id, catalog.Name, catalog.Description, catalog.Status);

    public static CatalogPackageResponse ToResponse(this CatalogPackage link) =>
        new(link.CatalogId, link.PackageId, link.IsRequestable);

    public static PackageResponse ToResponse(this Package package) =>
        new(package.Id, package.Name, package.Description, package.Status);

    public static PackageAccessItemResponse ToResponse(this PackageAccessItem link) =>
        new(link.PackageId, link.AccessItemId);

    public static ApprovalGroupResponse ToResponse(this ApprovalGroup group) =>
        new(group.Id, group.Name, group.Status);

    public static ApprovalGroupMemberResponse ToResponse(this ApprovalGroupMember member) =>
        new(member.Id, member.ApprovalGroupId, member.IdentityId, member.ResponsibleLocationId);

    public static ApprovalDefinitionResponse ToResponse(this ApprovalDefinition definition) =>
        new(definition.Id, definition.AccessItemId, definition.DestinationApprovalGroupId, definition.OrganizationalApprovalMode, definition.OrganizationalApprovalLevels);

    public static ApprovalRequirementResponse ToResponse(this ApprovalRequirement requirement) =>
        new(requirement.Id, requirement.ApprovalFlowId, requirement.RequestId, requirement.AccessItemId, requirement.LocationId, requirement.Type, requirement.Role, requirement.ApprovalGroupId, requirement.RequiredApproverIdentityId, requirement.Status, requirement.SystemApprovalReason, requirement.CreatedAt, requirement.CompletedAt);

    public static ApprovalDecisionResponse ToResponse(this ApprovalDecision decision) =>
        new(decision.Id, decision.RequestId, decision.ApprovalRequirementId, decision.ApproverIdentityId, decision.Role, decision.DecisionKind, decision.Note, decision.DecidedAt);

    public static PackageRequestResponse ToResponse(this PackageRequest request, Guid[] locationIds) =>
        new(request.Id, request.PackageId, request.RequesterIdentityId, request.BeneficiaryIdentityId, request.RequestReason, request.Status, request.SubStatus, request.DurationKind, request.ValidFrom, request.ValidUntil, request.CreatedAt, request.ExpiresAt, request.DecidedAt, locationIds);

    public static AccessGrantMaterializationOutcomeResponse ToResponse(this AccessGrantMaterializationOutcome outcome) =>
        new(
            outcome.Id,
            outcome.AccessItemId,
            outcome.LocationId,
            outcome.Status,
            outcome.FailureReason);

    public static GrantRequirementResponse ToResponse(this GrantRequirement requirement) =>
        new(requirement.Id, requirement.RequirementDefinitionId, requirement.SourcePolicyKind, requirement.SourcePolicyId, requirement.IsBlocking, requirement.DerivedAt);

    public static GrantRequirementResultResponse ToResponse(this GrantRequirementResult result) =>
        new(result.Id, result.RequirementDefinitionId, result.Status, result.EvidenceKind, result.EvidenceReference, result.Reason, result.ValidUntil, result.LastEvaluatedAt);

    public static AccessGrantResponse ToResponse(this AccessGrant grant, GrantProvisioningStatus provisioningStatus, GrantRequirementResponse[] requirements, GrantRequirementResultResponse[] requirementResults, AccessGrantMaterializationOutcomeResponse[] materializationOutcomes) =>
        new(
            grant.Id,
            grant.PackageId,
            grant.AccessItemId ?? Guid.Empty,
            grant.IdentityId,
            grant.AssignmentChannel,
            grant.SourceKind,
            grant.SourceId,
            grant.ApprovalFlowId,
            grant.RequestScopeId,
            grant.LocationId,
            grant.DurationKind,
            grant.ValidFrom,
            grant.ValidUntil,
            grant.Status,
            grant.ReplacedById,
            grant.ApprovalStatus,
            grant.ComplianceStatus,
            provisioningStatus,
            grant.CompliantUntil,
            grant.LastComplianceEvaluatedAt,
            grant.ReasonText,
            grant.RevokedBy,
            grant.RevokeCause,
            requirements,
            requirementResults,
            materializationOutcomes);
}
