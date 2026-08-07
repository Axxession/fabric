using AccessControl.Unipass.ChangeSets;
using AccessControl.Unipass.Contracts;
using AccessControl.Unipass.Entities;
using Fabric.Server.AccessControl.Domain;
using Fabric.Server.AccessControl.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.AccessControl.Application;

public sealed class PACSSubjectService(
    AccessControlDbContext db,
    IdentitiesDbContext identitiesDb,
    UnipassApiFactory apiFactory,
    TimeProvider timeProvider)
{
    public async Task<PACSSubject?> GetAsync(Guid subjectId, CancellationToken cancellationToken = default) =>
        await db.PACSSubjects.SingleOrDefaultAsync(subject => subject.Id == subjectId, cancellationToken);

    public async Task<PACSSubject?> GetAsync(Guid identityId, Guid accessControlSystemId, CancellationToken cancellationToken = default) =>
        await db.PACSSubjects.SingleOrDefaultAsync(subject => subject.IdentityId == identityId && subject.AccessControlSystemId == accessControlSystemId, cancellationToken);

    public async Task<Result<PACSSubject, AccessControlErrors>> GetOrCreateAsync(
        Guid identityId,
        AccessControlSystem system,
        CancellationToken cancellationToken = default)
    {
        PACSSubject? existing = await db.PACSSubjects
            .SingleOrDefaultAsync(subject => subject.IdentityId == identityId && subject.AccessControlSystemId == system.Id, cancellationToken);

        if (existing is not null)
            return Result.Success<PACSSubject, AccessControlErrors>(existing);

        Identity? identity = await identitiesDb.Identities
            .Include(item => item.VisitorAffiliations)
            .SingleOrDefaultAsync(item => item.Id == identityId, cancellationToken);

        if (identity is null)
            return Result.Failure<PACSSubject, AccessControlErrors>(AccessControlErrors.IdentityNotFound);

        if (system.ProviderKind != AccessControlProviderKind.Unipass || system.UnipassConfig is null)
            return Result.Failure<PACSSubject, AccessControlErrors>(AccessControlErrors.SystemProviderNotSupported);

        using IUnipassApi api = apiFactory.Create(system.UnipassConfig);
        PersonChangeSet changeSet = PersonChangeSet.Create()
            .FirstName(identity.FirstName)
            .LastName(identity.LastName)
            .PersonType(ResolvePersonType(identity));

        UnipassOperationResponse response = await api.ApplyChangeSet(changeSet, cancellationToken);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Id))
            return Result.Failure<PACSSubject, AccessControlErrors>(AccessControlErrors.ConfigInvalid);

        DateTimeOffset now = timeProvider.GetUtcNow();
        PACSSubject subject = PACSSubject.Create(
            identityId,
            system.Id,
            response.Id,
            PACSSubjectState.Active,
            identity.FirstName,
            identity.LastName,
            identity.Email,
            now);
        db.PACSSubjects.Add(subject);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<PACSSubject, AccessControlErrors>(subject);
    }

    public async Task<Result<PACSSubject, AccessControlErrors>> BlockProvisioningManuallyAsync(Guid subjectId, string reason, CancellationToken cancellationToken = default)
    {
        PACSSubject? subject = await db.PACSSubjects.SingleOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
        if (subject is null)
            return Result.Failure<PACSSubject, AccessControlErrors>(AccessControlErrors.PACSSubjectNotFound);

        Result<AccessControlErrors> block = subject.BlockProvisioningManually(reason, timeProvider.GetUtcNow());
        if (block.IsFailure(out AccessControlErrors error))
            return Result.Failure<PACSSubject, AccessControlErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<PACSSubject, AccessControlErrors>(subject);
    }

    public async Task<Result<PACSSubject, AccessControlErrors>> AllowProvisioningManuallyAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        PACSSubject? subject = await db.PACSSubjects.SingleOrDefaultAsync(item => item.Id == subjectId, cancellationToken);
        if (subject is null)
            return Result.Failure<PACSSubject, AccessControlErrors>(AccessControlErrors.PACSSubjectNotFound);

        subject.AllowProvisioningManually();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<PACSSubject, AccessControlErrors>(subject);
    }

    public async Task<(PACSSubjectProvisioningBlockStatus Status, string? Reason)> GetProvisioningBlockAsync(Guid identityId, Guid accessControlSystemId, CancellationToken cancellationToken = default)
    {
        PACSSubject? subject = await db.PACSSubjects
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdentityId == identityId && item.AccessControlSystemId == accessControlSystemId, cancellationToken);
        if (subject is null)
            return (PACSSubjectProvisioningBlockStatus.ProvisioningAllowed, null);

        AnomalyBlockMode anomalyBlockMode = await db.AccessControlSystems
            .Where(item => item.Id == accessControlSystemId)
            .Select(item => item.AnomalyBlockMode)
            .SingleAsync(cancellationToken);

        return (subject.GetProvisioningBlockStatus(anomalyBlockMode), subject.GetProvisioningBlockedReason(anomalyBlockMode));
    }

    private static UnipassPersonType ResolvePersonType(Identity identity) =>
        identity.VisitorAffiliations.Any(affiliation => affiliation.Status == AffiliationStatus.Active)
            ? UnipassPersonType.Visitor
            : UnipassPersonType.Staff;
}
