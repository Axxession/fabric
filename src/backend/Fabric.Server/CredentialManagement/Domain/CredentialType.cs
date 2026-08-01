using Fabric.Server.Core;

namespace Fabric.Server.CredentialManagement.Domain;

public sealed class CredentialType
{
    private readonly List<CredentialRange> _ranges = [];

    private CredentialType() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public CredentialTechnology Technology { get; private set; }
    public CredentialAllocationMode AllocationMode { get; private set; }
    public CredentialRecyclePolicy RecyclePolicy { get; private set; }
    public TimeSpan RecycleGracePeriod { get; private set; }
    public bool RequiresConfirmedPacsRevocation { get; private set; }
    public int? NearLimitThreshold { get; private set; }
    public string? IdentifierPrefix { get; private set; }
    public string? IdentifierSuffix { get; private set; }
    public int? IdentifierNumberLength { get; private set; }
    public CredentialIdentifierPaddingDirection? IdentifierPaddingDirection { get; private set; }
    public string? IdentifierPaddingCharacter { get; private set; }
    public CredentialTypeStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<CredentialRange> Ranges => _ranges;

    public static Result<CredentialType, CredentialManagementErrors> Create(
        string name,
        CredentialTechnology technology,
        CredentialAllocationMode allocationMode,
        CredentialRecyclePolicy recyclePolicy,
        TimeSpan recycleGracePeriod,
        bool requiresConfirmedPacsRevocation,
        int? nearLimitThreshold,
        string? identifierPrefix,
        string? identifierSuffix,
        int? identifierNumberLength,
        CredentialIdentifierPaddingDirection? identifierPaddingDirection,
        string? identifierPaddingCharacter,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || nearLimitThreshold is < 0)
            return Result.Failure<CredentialType, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeInvalid);

        Result<CredentialManagementErrors> recycleValidation = ValidateRecycleSettings(allocationMode, recyclePolicy, recycleGracePeriod);
        if (recycleValidation.IsFailure(out CredentialManagementErrors recycleError))
            return Result.Failure<CredentialType, CredentialManagementErrors>(recycleError);

        Result<CredentialManagementErrors> identifierValidation = ValidateIdentifierFormatting(
            technology,
            identifierPrefix,
            identifierSuffix,
            identifierNumberLength,
            identifierPaddingDirection,
            identifierPaddingCharacter);
        if (identifierValidation.IsFailure(out CredentialManagementErrors identifierError))
            return Result.Failure<CredentialType, CredentialManagementErrors>(identifierError);

        return Result.Success<CredentialType, CredentialManagementErrors>(new CredentialType
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Technology = technology,
            AllocationMode = allocationMode,
            RecyclePolicy = recyclePolicy,
            RecycleGracePeriod = recycleGracePeriod,
            RequiresConfirmedPacsRevocation = requiresConfirmedPacsRevocation,
            NearLimitThreshold = nearLimitThreshold,
            IdentifierPrefix = NormalizeOptional(identifierPrefix),
            IdentifierSuffix = NormalizeOptional(identifierSuffix),
            IdentifierNumberLength = identifierNumberLength,
            IdentifierPaddingDirection = identifierPaddingDirection,
            IdentifierPaddingCharacter = NormalizeOptional(identifierPaddingCharacter),
            Status = CredentialTypeStatus.Active,
            CreatedAt = now,
            UpdatedAt = now
        });
    }

    public Result<CredentialManagementErrors> Update(
        string name,
        CredentialTechnology technology,
        CredentialAllocationMode allocationMode,
        CredentialRecyclePolicy recyclePolicy,
        TimeSpan recycleGracePeriod,
        bool requiresConfirmedPacsRevocation,
        int? nearLimitThreshold,
        string? identifierPrefix,
        string? identifierSuffix,
        int? identifierNumberLength,
        CredentialIdentifierPaddingDirection? identifierPaddingDirection,
        string? identifierPaddingCharacter,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name) || nearLimitThreshold is < 0)
            return Result.Failure(CredentialManagementErrors.CredentialTypeInvalid);

        Result<CredentialManagementErrors> recycleValidation = ValidateRecycleSettings(allocationMode, recyclePolicy, recycleGracePeriod);
        if (recycleValidation.IsFailure(out CredentialManagementErrors recycleError))
            return Result.Failure(recycleError);

        Result<CredentialManagementErrors> identifierValidation = ValidateIdentifierFormatting(
            technology,
            identifierPrefix,
            identifierSuffix,
            identifierNumberLength,
            identifierPaddingDirection,
            identifierPaddingCharacter);
        if (identifierValidation.IsFailure(out CredentialManagementErrors identifierError))
            return Result.Failure(identifierError);

        Name = name.Trim();
        Technology = technology;
        AllocationMode = allocationMode;
        RecyclePolicy = recyclePolicy;
        RecycleGracePeriod = recycleGracePeriod;
        RequiresConfirmedPacsRevocation = requiresConfirmedPacsRevocation;
        NearLimitThreshold = nearLimitThreshold;
        IdentifierPrefix = NormalizeOptional(identifierPrefix);
        IdentifierSuffix = NormalizeOptional(identifierSuffix);
        IdentifierNumberLength = identifierNumberLength;
        IdentifierPaddingDirection = identifierPaddingDirection;
        IdentifierPaddingCharacter = NormalizeOptional(identifierPaddingCharacter);
        UpdatedAt = now;
        return Result.Success<CredentialManagementErrors>();
    }

    public string FormatIdentifier(string identifier)
    {
        string normalizedIdentifier = identifier.Trim();
        string formattedIdentifier = normalizedIdentifier;

        if (IdentifierNumberLength.HasValue)
        {
            char paddingCharacter = IdentifierPaddingCharacter![0];
            formattedIdentifier = IdentifierPaddingDirection == CredentialIdentifierPaddingDirection.Right
                ? formattedIdentifier.PadRight(IdentifierNumberLength.Value, paddingCharacter)
                : formattedIdentifier.PadLeft(IdentifierNumberLength.Value, paddingCharacter);
        }

        return string.Concat(IdentifierPrefix, formattedIdentifier, IdentifierSuffix);
    }

    public void Activate(DateTimeOffset now)
    {
        Status = CredentialTypeStatus.Active;
        UpdatedAt = now;
    }

    public void Disable(DateTimeOffset now)
    {
        Status = CredentialTypeStatus.Disabled;
        UpdatedAt = now;
    }

    private static Result<CredentialManagementErrors> ValidateRecycleSettings(
        CredentialAllocationMode allocationMode,
        CredentialRecyclePolicy recyclePolicy,
        TimeSpan recycleGracePeriod)
    {
        if (recyclePolicy != CredentialRecyclePolicy.NeverReuse && allocationMode != CredentialAllocationMode.Range)
            return Result.Failure(CredentialManagementErrors.CredentialRecyclePolicyInvalid);

        if (recycleGracePeriod < TimeSpan.Zero)
            return Result.Failure(CredentialManagementErrors.CredentialRecyclePolicyInvalid);

        if (recyclePolicy != CredentialRecyclePolicy.ReuseAfterRevocationAndGrace && recycleGracePeriod != TimeSpan.Zero)
            return Result.Failure(CredentialManagementErrors.CredentialRecyclePolicyInvalid);

        return Result.Success<CredentialManagementErrors>();
    }

    private static Result<CredentialManagementErrors> ValidateIdentifierFormatting(
        CredentialTechnology technology,
        string? identifierPrefix,
        string? identifierSuffix,
        int? identifierNumberLength,
        CredentialIdentifierPaddingDirection? identifierPaddingDirection,
        string? identifierPaddingCharacter)
    {
        string? normalizedPrefix = NormalizeOptional(identifierPrefix);
        string? normalizedSuffix = NormalizeOptional(identifierSuffix);
        string? normalizedPaddingCharacter = NormalizeOptional(identifierPaddingCharacter);
        bool hasFormatting = normalizedPrefix is not null
            || normalizedSuffix is not null
            || identifierNumberLength.HasValue
            || identifierPaddingDirection.HasValue
            || normalizedPaddingCharacter is not null;

        if (!hasFormatting)
            return Result.Success<CredentialManagementErrors>();

        if (technology != CredentialTechnology.Qr)
            return Result.Failure(CredentialManagementErrors.CredentialTypeInvalid);

        if (identifierNumberLength is < 1)
            return Result.Failure(CredentialManagementErrors.CredentialTypeInvalid);

        if (identifierNumberLength.HasValue)
        {
            if (!identifierPaddingDirection.HasValue || normalizedPaddingCharacter is null)
                return Result.Failure(CredentialManagementErrors.CredentialTypeInvalid);
        }
        else if (identifierPaddingDirection.HasValue || normalizedPaddingCharacter is not null)
        {
            return Result.Failure(CredentialManagementErrors.CredentialTypeInvalid);
        }

        if (normalizedPaddingCharacter is not null && normalizedPaddingCharacter.Length != 1)
            return Result.Failure(CredentialManagementErrors.CredentialTypeInvalid);

        return Result.Success<CredentialManagementErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
