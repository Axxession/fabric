using Fabric.Server.CredentialManagement.Domain;

namespace Fabric.Server.CredentialManagement.Contracts;

public static class CredentialManagementMappings
{
    public static CredentialTypeResponse ToResponse(
        this CredentialType credentialType,
        int usedCount,
        int availableCount,
        CredentialRangeResponse[]? ranges = null)
    {
        CredentialCapacityState capacityState = availableCount == 0
            ? CredentialCapacityState.Limit
            : credentialType.NearLimitThreshold.HasValue && availableCount <= credentialType.NearLimitThreshold.Value
                ? CredentialCapacityState.NearLimit
                : CredentialCapacityState.Healthy;

        return new CredentialTypeResponse(
            credentialType.Id,
            credentialType.Name,
            credentialType.Technology,
            credentialType.AllocationMode,
            credentialType.RecyclePolicy,
            credentialType.RecycleGracePeriod,
            credentialType.RequiresConfirmedPacsRevocation,
            usedCount,
            availableCount,
            credentialType.NearLimitThreshold,
            credentialType.IdentifierPrefix,
            credentialType.IdentifierSuffix,
            credentialType.IdentifierNumberLength,
            credentialType.IdentifierPaddingDirection,
            credentialType.IdentifierPaddingCharacter,
            capacityState,
            credentialType.Status,
            credentialType.CreatedAt,
            credentialType.UpdatedAt,
            ranges ?? credentialType.Ranges.Select(ToResponse).ToArray());
    }

    public static CredentialRangeResponse ToResponse(this CredentialRange range) =>
        new(range.Id, range.CredentialTypeId, range.RangeStart, range.RangeStop, range.IsActive);

    public static CredentialResponse ToResponse(this Credential credential, CredentialType? credentialType = null) =>
        new(
            credential.Id,
            credential.CredentialTypeId,
            credential.Identifier,
            credentialType?.FormatIdentifier(credential.Identifier) ?? credential.Identifier,
            credential.IdentityId,
            credential.DurationKind,
            credential.ValidFrom,
            credential.ValidUntil,
            credential.Status,
            credential.Purpose,
            credential.SourceKind,
            credential.SourceId,
            credential.RequestedByIdentityId,
            credential.ReasonText,
            credential.CreatedAt,
            credential.UpdatedAt);
}
