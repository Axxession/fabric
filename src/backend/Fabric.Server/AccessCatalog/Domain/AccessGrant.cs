using Fabric.Server.Core;

namespace Fabric.Server.AccessCatalog.Domain;

public sealed class AccessGrant
{
    private AccessGrant() { }

    public Guid Id { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid? AccessItemId { get; private set; }
    public Guid IdentityId { get; private set; }
    public AssignmentChannel AssignmentChannel { get; private set; }
    public AssignmentSourceKind SourceKind { get; private set; }
    public Guid SourceId { get; private set; }
    public Guid? ApprovalFlowId { get; private set; }
    public Guid? RequestScopeId { get; private set; }
    public Guid LocationId { get; private set; }
    public AccessDurationKind DurationKind { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public AccessGrantStatus Status { get; private set; }
    public Guid? ReplacedById { get; private set; }
    public GrantApprovalStatus ApprovalStatus { get; private set; }
    public GrantComplianceStatus ComplianceStatus { get; private set; }
    public DateTimeOffset? CompliantUntil { get; private set; }
    public DateTimeOffset? LastComplianceEvaluatedAt { get; private set; }
    public string ReasonText { get; private set; } = null!;
    public string? RevokedBy { get; private set; }
    public AccessGrantRevokeCause? RevokeCause { get; private set; }

    public static Result<AccessGrant, AccessCatalogErrors> Create(
        Guid packageId,
        Guid identityId,
        AssignmentChannel assignmentChannel,
        AssignmentSourceKind sourceKind,
        Guid sourceId,
        Guid? accessItemId,
        Guid? approvalFlowId,
        Guid? requestScopeId,
        Guid locationId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        GrantApprovalStatus approvalStatus,
        string reasonText)
    {
        if (durationKind == AccessDurationKind.Temporary && !validUntil.HasValue)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.InvalidValidityRange);

        if (durationKind == AccessDurationKind.Permanent && validUntil.HasValue)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.InvalidValidityRange);

        if (validUntil.HasValue && validUntil.Value <= validFrom)
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.InvalidValidityRange);

        if (string.IsNullOrWhiteSpace(reasonText))
            return Result.Failure<AccessGrant, AccessCatalogErrors>(AccessCatalogErrors.ReasonRequired);

        return Result.Success<AccessGrant, AccessCatalogErrors>(new AccessGrant
        {
            Id = Guid.NewGuid(),
            PackageId = packageId,
            IdentityId = identityId,
            AssignmentChannel = assignmentChannel,
            SourceKind = sourceKind,
            SourceId = sourceId,
            AccessItemId = accessItemId,
            ApprovalFlowId = approvalFlowId,
            RequestScopeId = requestScopeId,
            LocationId = locationId,
            DurationKind = durationKind,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = AccessGrantStatus.Active,
            ApprovalStatus = approvalStatus,
            ComplianceStatus = GrantComplianceStatus.NonCompliant,
            ReasonText = reasonText.Trim()
        });
    }

    public Result<AccessCatalogErrors> UpdateValidity(DateTimeOffset validFrom, DateTimeOffset? validUntil)
    {
        if (Status is AccessGrantStatus.Revoked or AccessGrantStatus.Replaced or AccessGrantStatus.Expired)
            return Result.Failure(AccessCatalogErrors.AccessGrantNotActive);

        if (DurationKind == AccessDurationKind.Temporary && !validUntil.HasValue)
            return Result.Failure(AccessCatalogErrors.InvalidValidityRange);

        if (DurationKind == AccessDurationKind.Permanent && validUntil.HasValue)
            return Result.Failure(AccessCatalogErrors.InvalidValidityRange);

        if (validUntil.HasValue && validUntil.Value <= validFrom)
            return Result.Failure(AccessCatalogErrors.InvalidValidityRange);

        ValidFrom = validFrom;
        ValidUntil = validUntil;
        return Result.Success<AccessCatalogErrors>();
    }

    public Result<AccessCatalogErrors> UpdateCompliance(GrantComplianceStatus complianceStatus, DateTimeOffset? compliantUntil, DateTimeOffset evaluatedAt)
    {
        if (Status is AccessGrantStatus.Revoked or AccessGrantStatus.Replaced or AccessGrantStatus.Expired)
            return Result.Failure(AccessCatalogErrors.AccessGrantNotActive);

        ComplianceStatus = complianceStatus;
        CompliantUntil = complianceStatus == GrantComplianceStatus.NonCompliant ? null : compliantUntil;
        LastComplianceEvaluatedAt = evaluatedAt;
        return Result.Success<AccessCatalogErrors>();
    }

    public Result<AccessCatalogErrors> Replace(Guid replacedById)
    {
        if (Status == AccessGrantStatus.Revoked)
            return Result.Failure(AccessCatalogErrors.AccessGrantAlreadyRevoked);

        if (Status == AccessGrantStatus.Replaced)
            return Result.Failure(AccessCatalogErrors.AccessGrantAlreadyReplaced);

        ReplacedById = replacedById;
        Status = AccessGrantStatus.Replaced;
        return Result.Success<AccessCatalogErrors>();
    }

    public Result<AccessCatalogErrors> Revoke(string? revokedBy, AccessGrantRevokeCause revokeCause)
    {
        if (Status == AccessGrantStatus.Revoked)
            return Result.Failure(AccessCatalogErrors.AccessGrantAlreadyRevoked);

        if (Status == AccessGrantStatus.Replaced)
            return Result.Failure(AccessCatalogErrors.AccessGrantAlreadyReplaced);

        Status = AccessGrantStatus.Revoked;
        RevokedBy = string.IsNullOrWhiteSpace(revokedBy) ? null : revokedBy.Trim();
        RevokeCause = revokeCause;
        return Result.Success<AccessCatalogErrors>();
    }
}
