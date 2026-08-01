using Fabric.Server.Core;
using Fabric.Server.CredentialManagement.Domain;

namespace Fabric.Server.CredentialManagement.Contracts;

public sealed record ListCredentialTypesRequest : BaseListRequest
{
    public string? Query { get; set; }
    public CredentialTechnology? Technology { get; set; }
    public CredentialTypeStatus? Status { get; set; }
}

public sealed record ListCredentialsRequest : BaseListRequest
{
    public Guid? CredentialTypeId { get; set; }
    public Guid? IdentityId { get; set; }
    public CredentialStatus? Status { get; set; }
}

public sealed record CreateCredentialTypeRequest(
    string Name,
    CredentialTechnology Technology,
    CredentialAllocationMode AllocationMode,
    CredentialRecyclePolicy RecyclePolicy,
    TimeSpan RecycleGracePeriod,
    bool RequiresConfirmedPacsRevocation,
    int? NearLimitThreshold,
    string? IdentifierPrefix,
    string? IdentifierSuffix,
    int? IdentifierNumberLength,
    CredentialIdentifierPaddingDirection? IdentifierPaddingDirection,
    string? IdentifierPaddingCharacter);

public sealed record UpdateCredentialTypeRequest(
    string Name,
    CredentialTechnology Technology,
    CredentialAllocationMode AllocationMode,
    CredentialRecyclePolicy RecyclePolicy,
    TimeSpan RecycleGracePeriod,
    bool RequiresConfirmedPacsRevocation,
    int? NearLimitThreshold,
    string? IdentifierPrefix,
    string? IdentifierSuffix,
    int? IdentifierNumberLength,
    CredentialIdentifierPaddingDirection? IdentifierPaddingDirection,
    string? IdentifierPaddingCharacter);

public sealed record CreateCredentialRangeRequest(
    long RangeStart,
    long RangeStop,
    bool IsActive);

public sealed record UpdateCredentialRangeRequest(
    long RangeStart,
    long RangeStop,
    bool IsActive);

public sealed record IssueCredentialRequest(
    Guid CredentialTypeId,
    string? Identifier,
    Guid IdentityId,
    CredentialDurationKind DurationKind,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    CredentialPurpose Purpose,
    CredentialSourceKind SourceKind,
    Guid? SourceId,
    Guid? RequestedByIdentityId,
    string ReasonText,
    Guid[] LocationIds);
