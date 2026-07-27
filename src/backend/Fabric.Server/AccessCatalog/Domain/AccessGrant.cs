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
    public AccessDurationKind DurationKind { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public AccessGrantStatus Status { get; private set; }
    public string ReasonText { get; private set; } = null!;

    public static Result<AccessGrant, AccessCatalogErrors> Create(
        Guid packageId,
        Guid identityId,
        AssignmentChannel assignmentChannel,
        AssignmentSourceKind sourceKind,
        Guid sourceId,
        Guid? accessItemId,
        Guid? approvalFlowId,
        Guid? requestScopeId,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
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
            DurationKind = durationKind,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = AccessGrantStatus.Active,
            ReasonText = reasonText.Trim()
        });
    }

    public Result<AccessCatalogErrors> Revoke()
    {
        if (Status == AccessGrantStatus.Revoked)
            return Result.Failure(AccessCatalogErrors.AccessGrantAlreadyRevoked);

        Status = AccessGrantStatus.Revoked;
        return Result.Success<AccessCatalogErrors>();
    }
}
