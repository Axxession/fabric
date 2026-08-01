using Fabric.Server.CredentialManagement.Domain;

namespace Fabric.Server.CredentialManagement.Contracts;

public sealed record CredentialTypeResponse(
    Guid Id,
    string Name,
    CredentialTechnology Technology,
    CredentialAllocationMode AllocationMode,
    CredentialRecyclePolicy RecyclePolicy,
    TimeSpan RecycleGracePeriod,
    bool RequiresConfirmedPacsRevocation,
    int UsedCount,
    int AvailableCount,
    int? NearLimitThreshold,
    string? IdentifierPrefix,
    string? IdentifierSuffix,
    int? IdentifierNumberLength,
    CredentialIdentifierPaddingDirection? IdentifierPaddingDirection,
    string? IdentifierPaddingCharacter,
    CredentialCapacityState CapacityState,
    CredentialTypeStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    CredentialRangeResponse[] Ranges);

public sealed record CredentialRangeResponse(
    Guid Id,
    Guid CredentialTypeId,
    long RangeStart,
    long RangeStop,
    bool IsActive);

public sealed record CredentialResponse(
    Guid Id,
    Guid CredentialTypeId,
    string Identifier,
    string FormattedIdentifier,
    Guid IdentityId,
    CredentialDurationKind DurationKind,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidUntil,
    CredentialStatus Status,
    CredentialPurpose Purpose,
    CredentialSourceKind SourceKind,
    Guid? SourceId,
    Guid? RequestedByIdentityId,
    string ReasonText,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
