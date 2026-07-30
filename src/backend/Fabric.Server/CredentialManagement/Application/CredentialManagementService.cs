using Fabric.Server.AccessControl.Application;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.CredentialManagement.Contracts;
using Fabric.Server.CredentialManagement.Domain;
using Fabric.Server.CredentialManagement.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Fabric.Server.CredentialManagement.Application;

public sealed class CredentialManagementService(
    CredentialManagementDbContext db,
    AccessControlDbContext accessControlDb,
    CredentialPACSAssignmentService credentialPacsAssignmentService,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan SlotReservationTtl = TimeSpan.FromMinutes(5);

    public async Task<Result<CredentialType, CredentialManagementErrors>> CreateCredentialTypeAsync(
        CreateCredentialTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        string name = request.Name.Trim();
        if (await db.CredentialTypes.AnyAsync(type => type.Name == name, cancellationToken))
            return Result.Failure<CredentialType, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeAlreadyExists);

        Result<CredentialType, CredentialManagementErrors> create = CredentialType.Create(
            name,
            request.Technology,
            request.AllocationMode,
            request.RecyclePolicy,
            request.RecycleGracePeriod,
            request.RequiresConfirmedPacsRevocation,
            request.NearLimitThreshold,
            request.IdentifierPrefix,
            request.IdentifierSuffix,
            request.IdentifierNumberLength,
            request.IdentifierPaddingDirection,
            request.IdentifierPaddingCharacter,
            timeProvider.GetUtcNow());

        if (create.IsFailure(out CredentialManagementErrors error))
            return Result.Failure<CredentialType, CredentialManagementErrors>(error);

        create.IsSuccess(out CredentialType credentialType);
        db.CredentialTypes.Add(credentialType);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialType, CredentialManagementErrors>(credentialType);
    }

    public async Task<Result<CredentialType, CredentialManagementErrors>> UpdateCredentialTypeAsync(
        Guid id,
        UpdateCredentialTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        CredentialType? credentialType = await db.CredentialTypes.SingleOrDefaultAsync(type => type.Id == id, cancellationToken);
        if (credentialType is null)
            return Result.Failure<CredentialType, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeNotFound);

        string name = request.Name.Trim();
        if (await db.CredentialTypes.AnyAsync(type => type.Id != id && type.Name == name, cancellationToken))
            return Result.Failure<CredentialType, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeAlreadyExists);

        Result<CredentialManagementErrors> update = credentialType.Update(
            name,
            request.Technology,
            request.AllocationMode,
            request.RecyclePolicy,
            request.RecycleGracePeriod,
            request.RequiresConfirmedPacsRevocation,
            request.NearLimitThreshold,
            request.IdentifierPrefix,
            request.IdentifierSuffix,
            request.IdentifierNumberLength,
            request.IdentifierPaddingDirection,
            request.IdentifierPaddingCharacter,
            timeProvider.GetUtcNow());

        if (update.IsFailure(out CredentialManagementErrors error))
            return Result.Failure<CredentialType, CredentialManagementErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialType, CredentialManagementErrors>(credentialType);
    }

    public async Task<Result<CredentialType, CredentialManagementErrors>> SetCredentialTypeStatusAsync(
        Guid id,
        CredentialTypeStatus status,
        CancellationToken cancellationToken = default)
    {
        CredentialType? credentialType = await db.CredentialTypes.SingleOrDefaultAsync(type => type.Id == id, cancellationToken);
        if (credentialType is null)
            return Result.Failure<CredentialType, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeNotFound);

        DateTimeOffset now = timeProvider.GetUtcNow();
        if (status == CredentialTypeStatus.Active)
            credentialType.Activate(now);
        else
            credentialType.Disable(now);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialType, CredentialManagementErrors>(credentialType);
    }

    public async Task<Result<CredentialRange, CredentialManagementErrors>> CreateCredentialRangeAsync(
        Guid credentialTypeId,
        CreateCredentialRangeRequest request,
        CancellationToken cancellationToken = default)
    {
        CredentialType? credentialType = await db.CredentialTypes
            .Include(type => type.Ranges)
            .SingleOrDefaultAsync(type => type.Id == credentialTypeId, cancellationToken);
        if (credentialType is null)
            return Result.Failure<CredentialRange, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeNotFound);

        if (credentialType.AllocationMode != CredentialAllocationMode.Range)
            return Result.Failure<CredentialRange, CredentialManagementErrors>(CredentialManagementErrors.CredentialRangeInvalid);

        bool overlaps = credentialType.Ranges.Any(range => !(request.RangeStop < range.RangeStart || request.RangeStart > range.RangeStop));
        if (overlaps)
            return Result.Failure<CredentialRange, CredentialManagementErrors>(CredentialManagementErrors.CredentialRangeInvalid);

        Result<CredentialRange, CredentialManagementErrors> create = CredentialRange.Create(credentialTypeId, request.RangeStart, request.RangeStop, request.IsActive);
        if (create.IsFailure(out CredentialManagementErrors error))
            return Result.Failure<CredentialRange, CredentialManagementErrors>(error);

        create.IsSuccess(out CredentialRange range);
        db.CredentialRanges.Add(range);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialRange, CredentialManagementErrors>(range);
    }

    public async Task<Result<CredentialRange, CredentialManagementErrors>> UpdateCredentialRangeAsync(
        Guid rangeId,
        UpdateCredentialRangeRequest request,
        CancellationToken cancellationToken = default)
    {
        CredentialRange? range = await db.CredentialRanges.SingleOrDefaultAsync(item => item.Id == rangeId, cancellationToken);
        if (range is null)
            return Result.Failure<CredentialRange, CredentialManagementErrors>(CredentialManagementErrors.CredentialRangeNotFound);

        bool overlaps = await db.CredentialRanges.AnyAsync(
            item => item.Id != rangeId && item.CredentialTypeId == range.CredentialTypeId && !(request.RangeStop < item.RangeStart || request.RangeStart > item.RangeStop),
            cancellationToken);
        if (overlaps)
            return Result.Failure<CredentialRange, CredentialManagementErrors>(CredentialManagementErrors.CredentialRangeInvalid);

        Result<CredentialManagementErrors> update = range.Update(request.RangeStart, request.RangeStop, request.IsActive);
        if (update.IsFailure(out CredentialManagementErrors error))
            return Result.Failure<CredentialRange, CredentialManagementErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialRange, CredentialManagementErrors>(range);
    }

    public async Task<Result<Credential, CredentialManagementErrors>> IssueCredentialAsync(
        IssueCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        CredentialType? credentialType = await db.CredentialTypes
            .Include(type => type.Ranges)
            .SingleOrDefaultAsync(type => type.Id == request.CredentialTypeId, cancellationToken);
        if (credentialType is null)
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeNotFound);

        if (credentialType.Status != CredentialTypeStatus.Active)
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.CredentialTypeDisabled);

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            Result<ResolvedIdentifier, CredentialManagementErrors> identifierResult = await ResolveIdentifierAsync(
                credentialType,
                request.Identifier,
                now,
                cancellationToken);

            if (identifierResult.IsFailure(out CredentialManagementErrors identifierError))
                return Result.Failure<Credential, CredentialManagementErrors>(identifierError);

            identifierResult.IsSuccess(out ResolvedIdentifier resolvedIdentifier);

            if (credentialType.AllocationMode == CredentialAllocationMode.Provided &&
                await db.Credentials.AnyAsync(
                    item => item.CredentialTypeId == credentialType.Id && item.Identifier == resolvedIdentifier.Identifier && item.Status != CredentialStatus.Archived,
                    cancellationToken))
                return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierAlreadyExists);

            Result<Credential, CredentialManagementErrors> create = Credential.Create(
                request.CredentialTypeId,
                resolvedIdentifier.Identifier,
                request.IdentityId,
                request.DurationKind,
                request.ValidFrom,
                request.ValidUntil,
                request.Purpose,
                request.SourceKind,
                request.SourceId,
                request.RequestedByIdentityId,
                request.ReasonText,
                now);

            if (create.IsFailure(out CredentialManagementErrors error))
                return Result.Failure<Credential, CredentialManagementErrors>(error);

            create.IsSuccess(out Credential credential);
            db.Credentials.Add(credential);

            if (resolvedIdentifier.Slot is not null)
            {
                Result<CredentialManagementErrors> assign = resolvedIdentifier.Slot.Assign(credential.Id, now);
                if (assign.IsFailure(out CredentialManagementErrors assignError))
                    return Result.Failure<Credential, CredentialManagementErrors>(assignError);
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await credentialPacsAssignmentService.CreateAssignmentsForCredentialAsync(
                credential.Id,
                credential.CredentialTypeId,
                request.LocationIds,
                credential.ValidFrom,
                credential.ValidUntil,
                cancellationToken);

            return Result.Success<Credential, CredentialManagementErrors>(credential);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<Credential, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierUnavailable);
        }
    }

    public async Task<int> ReleaseExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        CredentialSlot[] expiredReservations = await db.CredentialSlots
            .Where(slot => slot.Status == CredentialSlotStatus.Reserved)
            .Where(slot => slot.ReservationExpiresAt.HasValue && slot.ReservationExpiresAt.Value <= now)
            .ToArrayAsync(cancellationToken);

        foreach (CredentialSlot slot in expiredReservations)
        {
            Result<CredentialManagementErrors> free = slot.Free(now);
            if (free.IsFailure(out _))
                continue;
        }

        if (expiredReservations.Length > 0)
            await db.SaveChangesAsync(cancellationToken);

        return expiredReservations.Length;
    }

    public async Task<Result<CredentialManagementErrors>> RevokeCredentialAsync(Guid credentialId, CancellationToken cancellationToken = default)
    {
        Credential? credential = await db.Credentials.SingleOrDefaultAsync(item => item.Id == credentialId, cancellationToken);
        if (credential is null)
            return Result.Failure(CredentialManagementErrors.CredentialNotFound);

        if (credential.Status is CredentialStatus.Revoked or CredentialStatus.Archived)
            return Result.Success<CredentialManagementErrors>();

        credential.Revoke(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialManagementErrors>();
    }

    public async Task<Result<CredentialManagementErrors>> UpdateCredentialValidityWindowAsync(
        Guid credentialId,
        DateTimeOffset validFrom,
        DateTimeOffset? validUntil,
        CancellationToken cancellationToken = default)
    {
        Credential? credential = await db.Credentials.SingleOrDefaultAsync(item => item.Id == credentialId, cancellationToken);
        if (credential is null)
            return Result.Failure(CredentialManagementErrors.CredentialNotFound);

        Result<CredentialManagementErrors> update = credential.UpdateValidityWindow(validFrom, validUntil, timeProvider.GetUtcNow());
        if (update.IsFailure(out CredentialManagementErrors error))
            return Result.Failure(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CredentialManagementErrors>();
    }

    public async Task<int> ProcessExpiredCredentialsAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();

        Credential[] expiredCredentials = await db.Credentials
            .Where(item => item.ValidUntil.HasValue && item.ValidUntil.Value <= now)
            .Where(item => item.Status == CredentialStatus.Issued || item.Status == CredentialStatus.Active)
            .ToArrayAsync(cancellationToken);

        foreach (Credential credential in expiredCredentials)
            credential.MarkExpired(now);

        Credential[] recyclableCredentials = await db.Credentials
            .Where(item => item.ValidUntil.HasValue && item.ValidUntil.Value <= now)
            .Where(item => item.DurationKind == CredentialDurationKind.Temporary)
            .Where(item => item.Status == CredentialStatus.Expired)
            .ToArrayAsync(cancellationToken);

        if (recyclableCredentials.Length > 0)
        {
            Guid[] credentialTypeIds = recyclableCredentials.Select(item => item.CredentialTypeId).Distinct().ToArray();
            Guid[] recyclableCredentialIds = recyclableCredentials.Select(item => item.Id).ToArray();
            Dictionary<Guid, CredentialType> credentialTypes = await db.CredentialTypes
                .Where(item => credentialTypeIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            Dictionary<Guid, CredentialSlot> slotsByCredentialId = await db.CredentialSlots
                .Where(slot => slot.CredentialId.HasValue && recyclableCredentialIds.Contains(slot.CredentialId.Value))
                .ToDictionaryAsync(slot => slot.CredentialId!.Value, cancellationToken);

            Dictionary<Guid, CredentialPACSAssignmentStatus[]> assignmentStatusesByCredentialId = await accessControlDb.CredentialPACSAssignments
                .Where(item => recyclableCredentialIds.Contains(item.CredentialId))
                .GroupBy(item => item.CredentialId)
                .Select(group => new { CredentialId = group.Key, Statuses = group.Select(item => item.Status).ToArray() })
                .ToDictionaryAsync(item => item.CredentialId, item => item.Statuses, cancellationToken);

            foreach (Credential credential in recyclableCredentials)
            {
                if (!credentialTypes.TryGetValue(credential.CredentialTypeId, out CredentialType? credentialType))
                    continue;

                if (credentialType.RecyclePolicy == CredentialRecyclePolicy.NeverReuse)
                    continue;

                if (!slotsByCredentialId.TryGetValue(credential.Id, out CredentialSlot? slot) || slot.Status != CredentialSlotStatus.Issued)
                    continue;

                if (!CanRecycleSlot(credentialType, assignmentStatusesByCredentialId.GetValueOrDefault(credential.Id)))
                    continue;

                DateTimeOffset reusableFrom = GetReusableFrom(credentialType, now);
                if (reusableFrom <= now)
                {
                    slot.Free(now);
                    continue;
                }

                slot.MoveToCoolingDown(reusableFrom, now);
            }
        }

        CredentialSlot[] coolingSlots = await db.CredentialSlots
            .Where(slot => slot.Status == CredentialSlotStatus.CoolingDown)
            .Where(slot => slot.ReusableFrom.HasValue && slot.ReusableFrom.Value <= now)
            .ToArrayAsync(cancellationToken);

        foreach (CredentialSlot slot in coolingSlots)
            slot.Free(now);

        if (expiredCredentials.Length > 0 || coolingSlots.Length > 0)
            await db.SaveChangesAsync(cancellationToken);

        return expiredCredentials.Length + coolingSlots.Length;
    }

    private async Task<Result<ResolvedIdentifier, CredentialManagementErrors>> ResolveIdentifierAsync(
        CredentialType credentialType,
        string? requestedIdentifier,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return credentialType.AllocationMode switch
        {
            CredentialAllocationMode.Provided => ResolveProvidedIdentifier(requestedIdentifier),
            CredentialAllocationMode.Range => await ResolveRangeIdentifierAsync(credentialType, requestedIdentifier, now, cancellationToken),
            _ => Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierRequired)
        };
    }

    private static Result<ResolvedIdentifier, CredentialManagementErrors> ResolveProvidedIdentifier(string? requestedIdentifier)
    {
        if (string.IsNullOrWhiteSpace(requestedIdentifier))
            return Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierRequired);

        return Result.Success<ResolvedIdentifier, CredentialManagementErrors>(new ResolvedIdentifier(requestedIdentifier.Trim(), null));
    }

    private async Task<Result<ResolvedIdentifier, CredentialManagementErrors>> ResolveRangeIdentifierAsync(
        CredentialType credentialType,
        string? requestedIdentifier,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        CredentialRange[] activeRanges = credentialType.Ranges.Where(range => range.IsActive).OrderBy(range => range.RangeStart).ToArray();
        if (activeRanges.Length == 0)
            return Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierUnavailable);

        if (requestedIdentifier is not null)
        {
            if (!long.TryParse(requestedIdentifier, out long number))
                return Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierMustBeNumeric);

            CredentialRange? matchedRange = activeRanges.FirstOrDefault(range => number >= range.RangeStart && number <= range.RangeStop);
            if (matchedRange is null)
                return Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierOutsideRange);

            Result<CredentialSlot, CredentialManagementErrors> slotResult = await ReserveSpecificSlotAsync(matchedRange, number, now, cancellationToken);
            return slotResult.Match(
                slot => Result.Success<ResolvedIdentifier, CredentialManagementErrors>(new ResolvedIdentifier(number.ToString(), slot)),
                error => Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(error));
        }

        foreach (CredentialRange range in activeRanges)
        {
            Result<ResolvedIdentifier, CredentialManagementErrors>? candidate = await TryReserveNextSlotAsync(range, now, cancellationToken);
            if (candidate is not null)
                return candidate.Value;
        }

        return Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierUnavailable);
    }

    private async Task<Result<ResolvedIdentifier, CredentialManagementErrors>?> TryReserveNextSlotAsync(
        CredentialRange range,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        CredentialSlot[] touchedSlots = await db.CredentialSlots
            .Where(slot => slot.CredentialRangeId == range.Id)
            .ToArrayAsync(cancellationToken);

        Dictionary<long, CredentialSlot> slotsByNumber = touchedSlots.ToDictionary(slot => slot.Number);
        long candidate = range.NormalizeCandidate(range.NextCandidateNumber);
        long size = range.RangeStop - range.RangeStart + 1;

        for (long offset = 0; offset < size; offset++)
        {
            long number = candidate + offset;
            if (number > range.RangeStop)
                number = range.RangeStart + (number - range.RangeStop - 1);

            Result<CredentialSlot, CredentialManagementErrors>? reservation = TryReserveSlot(range, slotsByNumber, number, now);
            if (reservation is null)
                continue;

            if (reservation.Value.IsFailure(out CredentialManagementErrors error))
                return Result.Failure<ResolvedIdentifier, CredentialManagementErrors>(error);

            reservation.Value.IsSuccess(out CredentialSlot slot);
            range.AdvanceNextCandidate(number);
            return Result.Success<ResolvedIdentifier, CredentialManagementErrors>(new ResolvedIdentifier(number.ToString(), slot));
        }

        return null;
    }

    private async Task<Result<CredentialSlot, CredentialManagementErrors>> ReserveSpecificSlotAsync(
        CredentialRange range,
        long number,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        CredentialSlot[] touchedSlots = await db.CredentialSlots
            .Where(slot => slot.CredentialRangeId == range.Id && slot.Number == number)
            .ToArrayAsync(cancellationToken);

        Dictionary<long, CredentialSlot> slotsByNumber = touchedSlots.ToDictionary(slot => slot.Number);
        Result<CredentialSlot, CredentialManagementErrors>? reservation = TryReserveSlot(range, slotsByNumber, number, now);
        if (reservation is null)
            return Result.Failure<CredentialSlot, CredentialManagementErrors>(CredentialManagementErrors.CredentialIdentifierUnavailable);

        range.AdvanceNextCandidate(number);
        return reservation.Value;
    }

    private Result<CredentialSlot, CredentialManagementErrors>? TryReserveSlot(
        CredentialRange range,
        IReadOnlyDictionary<long, CredentialSlot> slotsByNumber,
        long number,
        DateTimeOffset now)
    {
        DateTimeOffset reservationExpiresAt = now.Add(SlotReservationTtl);

        if (!slotsByNumber.TryGetValue(number, out CredentialSlot? slot))
        {
            CredentialSlot newSlot = CredentialSlot.Reserve(range.Id, number, reservationExpiresAt, now);
            db.CredentialSlots.Add(newSlot);
            return Result.Success<CredentialSlot, CredentialManagementErrors>(newSlot);
        }

        if (slot.IsReservationExpired(now))
            slot.Free(now);

        if (slot.Status != CredentialSlotStatus.Free)
            return null;

        Result<CredentialManagementErrors> reserve = slot.Reserve(reservationExpiresAt, now);
        return reserve.IsFailure(out CredentialManagementErrors error)
            ? Result.Failure<CredentialSlot, CredentialManagementErrors>(error)
            : Result.Success<CredentialSlot, CredentialManagementErrors>(slot);
    }

    private static bool CanRecycleSlot(CredentialType credentialType, CredentialPACSAssignmentStatus[]? assignmentStatuses)
    {
        bool assignmentsRevoked = assignmentStatuses is null || assignmentStatuses.All(status => status is CredentialPACSAssignmentStatus.Revoked or CredentialPACSAssignmentStatus.FailedTerminal);

        return credentialType.RecyclePolicy switch
        {
            CredentialRecyclePolicy.NeverReuse => false,
            CredentialRecyclePolicy.ReuseAfterExpiry => credentialType.RequiresConfirmedPacsRevocation ? assignmentsRevoked : true,
            CredentialRecyclePolicy.ReuseAfterRevocation => assignmentsRevoked,
            CredentialRecyclePolicy.ReuseAfterRevocationAndGrace => assignmentsRevoked,
            _ => false
        };
    }

    private static DateTimeOffset GetReusableFrom(CredentialType credentialType, DateTimeOffset now) =>
        credentialType.RecyclePolicy == CredentialRecyclePolicy.ReuseAfterRevocationAndGrace
            ? now.Add(credentialType.RecycleGracePeriod)
            : now;

    private sealed record ResolvedIdentifier(string Identifier, CredentialSlot? Slot);
}
