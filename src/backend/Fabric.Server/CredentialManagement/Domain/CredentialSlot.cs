using Fabric.Server.Core;

namespace Fabric.Server.CredentialManagement.Domain;

public sealed class CredentialSlot
{
    private CredentialSlot() { }

    public Guid Id { get; private set; }
    public Guid CredentialRangeId { get; private set; }
    public long Number { get; private set; }
    public CredentialSlotStatus Status { get; private set; }
    public Guid? CredentialId { get; private set; }
    public DateTimeOffset? ReservationExpiresAt { get; private set; }
    public DateTimeOffset? ReusableFrom { get; private set; }
    public DateTimeOffset LastStateChangedAt { get; private set; }

    public static CredentialSlot Reserve(Guid credentialRangeId, long number, DateTimeOffset reservationExpiresAt, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            CredentialRangeId = credentialRangeId,
            Number = number,
            Status = CredentialSlotStatus.Reserved,
            ReservationExpiresAt = reservationExpiresAt,
            LastStateChangedAt = now
        };

    public Result<CredentialManagementErrors> Reserve(DateTimeOffset reservationExpiresAt, DateTimeOffset now)
    {
        if (Status is not CredentialSlotStatus.Free)
            return Result.Failure(CredentialManagementErrors.CredentialIdentifierUnavailable);

        Status = CredentialSlotStatus.Reserved;
        ReservationExpiresAt = reservationExpiresAt;
        CredentialId = null;
        ReusableFrom = null;
        LastStateChangedAt = now;
        return Result.Success<CredentialManagementErrors>();
    }

    public Result<CredentialManagementErrors> Assign(Guid credentialId, DateTimeOffset now)
    {
        if (Status is not CredentialSlotStatus.Reserved)
            return Result.Failure(CredentialManagementErrors.CredentialIdentifierUnavailable);

        CredentialId = credentialId;
        Status = CredentialSlotStatus.Issued;
        ReservationExpiresAt = null;
        ReusableFrom = null;
        LastStateChangedAt = now;
        return Result.Success<CredentialManagementErrors>();
    }

    public Result<CredentialManagementErrors> MoveToCoolingDown(DateTimeOffset reusableFrom, DateTimeOffset now)
    {
        if (Status is not CredentialSlotStatus.Issued)
            return Result.Failure(CredentialManagementErrors.CredentialIdentifierUnavailable);

        Status = CredentialSlotStatus.CoolingDown;
        ReservationExpiresAt = null;
        ReusableFrom = reusableFrom;
        LastStateChangedAt = now;
        return Result.Success<CredentialManagementErrors>();
    }

    public Result<CredentialManagementErrors> Free(DateTimeOffset now)
    {
        if (Status is not CredentialSlotStatus.CoolingDown and not CredentialSlotStatus.Reserved and not CredentialSlotStatus.Issued)
            return Result.Failure(CredentialManagementErrors.CredentialIdentifierUnavailable);

        Status = CredentialSlotStatus.Free;
        CredentialId = null;
        ReservationExpiresAt = null;
        ReusableFrom = null;
        LastStateChangedAt = now;
        return Result.Success<CredentialManagementErrors>();
    }

    public bool IsReservationExpired(DateTimeOffset now) =>
        Status == CredentialSlotStatus.Reserved && ReservationExpiresAt.HasValue && ReservationExpiresAt.Value <= now;
}
