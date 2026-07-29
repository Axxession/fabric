using Fabric.Server.Core;

namespace Fabric.Server.CredentialManagement.Domain;

public sealed class Credential
{
    private Credential() { }

    public Guid Id { get; private set; }
    public Guid CredentialTypeId { get; private set; }
    public string Identifier { get; private set; } = null!;
    public Guid IdentityId { get; private set; }
    public CredentialDurationKind DurationKind { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public CredentialStatus Status { get; private set; }
    public CredentialPurpose Purpose { get; private set; }
    public CredentialSourceKind SourceKind { get; private set; }
    public Guid? SourceId { get; private set; }
    public Guid? RequestedByIdentityId { get; private set; }
    public string ReasonText { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsExpired(DateTimeOffset now) =>
        ValidUntil.HasValue && ValidUntil.Value <= now;

    public static Result<Credential, CredentialManagementErrors> Create(
        Guid credentialTypeId,
        string identifier,
        Guid identityId,
        CredentialDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        CredentialPurpose purpose,
        CredentialSourceKind sourceKind,
        Guid? sourceId,
        Guid? requestedByIdentityId,
        string reasonText,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierRequired);

        if (string.IsNullOrWhiteSpace(reasonText))
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.ReasonRequired);

        if (durationKind == CredentialDurationKind.Temporary && !validUntil.HasValue)
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.TemporaryCredentialRequiresValidUntil);

        if (durationKind == CredentialDurationKind.Permanent && validUntil.HasValue)
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.PermanentCredentialMustNotHaveValidUntil);

        if (validUntil.HasValue && validUntil.Value <= validFrom)
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.ValidUntilMustBeAfterValidFrom);

        return Result.Success<Credential, CredentialManagementErrors>(new Credential
        {
            Id = Guid.NewGuid(),
            CredentialTypeId = credentialTypeId,
            Identifier = identifier.Trim(),
            IdentityId = identityId,
            DurationKind = durationKind,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            Status = validFrom <= now ? CredentialStatus.Active : CredentialStatus.Issued,
            Purpose = purpose,
            SourceKind = sourceKind,
            SourceId = sourceId,
            RequestedByIdentityId = requestedByIdentityId,
            ReasonText = reasonText.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public void MarkExpired(DateTimeOffset now)
    {
        Status = CredentialStatus.Expired;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = CredentialStatus.Archived;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        Status = CredentialStatus.Revoked;
        UpdatedAt = now;
    }
}
