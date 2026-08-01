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

    private static UnipassPersonType ResolvePersonType(Identity identity) =>
        identity.VisitorAffiliations.Any(affiliation => affiliation.Status == AffiliationStatus.Active)
            ? UnipassPersonType.Visitor
            : UnipassPersonType.Staff;
}
