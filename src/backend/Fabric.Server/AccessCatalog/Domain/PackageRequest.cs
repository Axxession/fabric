using Fabric.Server.Core;

namespace Fabric.Server.AccessCatalog.Domain;

public sealed class PackageRequest
{
    private PackageRequest() { }

    public Guid Id { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid RequesterIdentityId { get; private set; }
    public Guid BeneficiaryIdentityId { get; private set; }
    public string RequestReason { get; private set; } = null!;
    public PackageRequestStatus Status { get; private set; }
    public PackageRequestSubStatus? SubStatus { get; private set; }
    public AccessDurationKind DurationKind { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }

    public static Result<PackageRequest, AccessCatalogErrors> Create(
        Guid packageId,
        Guid requesterIdentityId,
        Guid beneficiaryIdentityId,
        string requestReason,
        AccessDurationKind durationKind,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(requestReason))
            return Result.Failure<PackageRequest, AccessCatalogErrors>(AccessCatalogErrors.ReasonRequired);

        if (durationKind == AccessDurationKind.Temporary && !validUntil.HasValue)
            return Result.Failure<PackageRequest, AccessCatalogErrors>(AccessCatalogErrors.InvalidValidityRange);

        if (durationKind == AccessDurationKind.Permanent && validUntil.HasValue)
            return Result.Failure<PackageRequest, AccessCatalogErrors>(AccessCatalogErrors.InvalidValidityRange);

        if (validUntil.HasValue && validUntil.Value <= validFrom)
            return Result.Failure<PackageRequest, AccessCatalogErrors>(AccessCatalogErrors.InvalidValidityRange);

        if (expiresAt <= createdAt)
            return Result.Failure<PackageRequest, AccessCatalogErrors>(AccessCatalogErrors.InvalidValidityRange);

        return Result.Success<PackageRequest, AccessCatalogErrors>(new PackageRequest
        {
            Id = Guid.NewGuid(),
            PackageId = packageId,
            RequesterIdentityId = requesterIdentityId,
            BeneficiaryIdentityId = beneficiaryIdentityId,
            RequestReason = requestReason.Trim(),
            Status = PackageRequestStatus.InProgress,
            DurationKind = durationKind,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt
        });
    }

    public void MarkCompleted(PackageRequestSubStatus subStatus, DateTimeOffset decidedAt)
    {
        Status = PackageRequestStatus.Completed;
        SubStatus = subStatus;
        DecidedAt = decidedAt;
    }
}
