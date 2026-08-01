using Fabric.Server.Core;
using Fabric.Server.Identities.Contracts;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Identities.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Identities.Application;

public class IdentityService(IdentitiesDbContext db, TimeProvider timeProvider)
{
    public async Task<Result<Identity, IdentityErrors>> CreateIdentityAsync(
        string firstName,
        string? middleName,
        string lastName,
        string? preferredName,
        string? email,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        Result<Identity, IdentityErrors> create = Identity.Create(
            firstName,
            middleName,
            lastName,
            preferredName,
            email,
            phone,
            timeProvider.GetUtcNow());

        if (create.IsFailure(out IdentityErrors error))
            return Result.Failure<Identity, IdentityErrors>(error);

        create.IsSuccess(out Identity identity);
        db.Identities.Add(identity);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Identity, IdentityErrors>(identity);
    }

    public async Task<Result<Identity, IdentityErrors>> UpdateProfileAsync(
        Guid identityId,
        string firstName,
        string? middleName,
        string lastName,
        string? preferredName,
        string? email,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        Identity? identity = await db.Identities.SingleOrDefaultAsync(item => item.Id == identityId, cancellationToken);
        if (identity is null)
            return Result.Failure<Identity, IdentityErrors>(IdentityErrors.IdentityNotFound);

        Result<IdentityErrors> result = identity.UpdateProfile(firstName, middleName, lastName, preferredName, email, phone, timeProvider.GetUtcNow());
        if (result.IsFailure(out IdentityErrors error))
            return Result.Failure<Identity, IdentityErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Identity, IdentityErrors>(identity);
    }

    public async Task<Result<Identity, IdentityErrors>> UpsertFromVisitorAsync(
        Guid? identityId,
        Guid visitorId,
        string firstName,
        string lastName,
        string? email,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        Identity? identity = null;

        if (identityId.HasValue)
        {
            identity = await db.Identities
                .Include(item => item.VisitorAffiliations)
                .SingleOrDefaultAsync(item => item.Id == identityId.Value, cancellationToken);

            if (identity is null)
                return Result.Failure<Identity, IdentityErrors>(IdentityErrors.IdentityNotFound);

            Result<IdentityErrors> update = identity.UpdateProfile(firstName, null, lastName, null, email, null, now);
            if (update.IsFailure(out IdentityErrors updateError))
                return Result.Failure<Identity, IdentityErrors>(updateError);
        }
        else
        {
            Result<Identity, IdentityErrors> create = Identity.Create(firstName, null, lastName, null, email, null, now);
            if (create.IsFailure(out IdentityErrors createError))
                return Result.Failure<Identity, IdentityErrors>(createError);

            create.IsSuccess(out identity);
            db.Identities.Add(identity);
        }

        VisitorAffiliation? existingAffiliation = await db.VisitorAffiliations
            .SingleOrDefaultAsync(affiliation => affiliation.VisitorId == visitorId, cancellationToken);

        if (existingAffiliation is not null && existingAffiliation.IdentityId != identity.Id)
            return Result.Failure<Identity, IdentityErrors>(IdentityErrors.VisitorAlreadyLinkedToDifferentIdentity);

        if (existingAffiliation is null)
        {
            Result<VisitorAffiliation, IdentityErrors> addAffiliation = identity.AddVisitorAffiliation(visitorId, now, null, now);
            if (addAffiliation.IsFailure(out IdentityErrors error))
                return Result.Failure<Identity, IdentityErrors>(error);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Identity, IdentityErrors>(identity);
    }

    public async Task<Identity?> GetIdentityAsync(Guid identityId, CancellationToken cancellationToken = default) =>
        await db.Identities
            .AsNoTracking()
            .Include(identity => identity.EmployeeAffiliations)
            .Include(identity => identity.ContractorAffiliations)
            .Include(identity => identity.VisitorAffiliations)
            .SingleOrDefaultAsync(identity => identity.Id == identityId, cancellationToken);

    public async Task<Guid?> GetIdentityIdForVisitorAsync(Guid visitorId, CancellationToken cancellationToken = default) =>
        await db.VisitorAffiliations
            .AsNoTracking()
            .Where(affiliation => affiliation.VisitorId == visitorId)
            .Select(affiliation => (Guid?)affiliation.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Guid?> GetIdentityIdForContractorAsync(Guid contractorId, CancellationToken cancellationToken = default) =>
        await db.ContractorAffiliations
            .AsNoTracking()
            .Where(affiliation => affiliation.ContractorId == contractorId)
            .Select(affiliation => (Guid?)affiliation.IdentityId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IPaged<Identity>> SearchIdentitiesAsync(
        ListIdentitiesRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Identity> query = db.Identities.AsNoTracking()
            .Include(identity => identity.EmployeeAffiliations)
            .Include(identity => identity.ContractorAffiliations)
            .Include(identity => identity.VisitorAffiliations);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(identity =>
                EF.Functions.ILike(identity.FirstName, filter)
                || EF.Functions.ILike(identity.LastName, filter)
                || EF.Functions.ILike(identity.DisplayName, filter)
                || identity.Email != null && EF.Functions.ILike(identity.Email, filter));
        }

        if (request.Status.HasValue)
            query = query.Where(identity => identity.Status == request.Status.Value);

        if (request.AffiliationType.HasValue)
        {
            query = request.AffiliationType.Value switch
            {
                IdentityAffiliationType.Employee => query.Where(identity => identity.EmployeeAffiliations.Any()),
                IdentityAffiliationType.Contractor => query.Where(identity => identity.ContractorAffiliations.Any()),
                IdentityAffiliationType.Visitor => query.Where(identity => identity.VisitorAffiliations.Any()),
                _ => query,
            };
        }

        query = query.OrderBy(identity => identity.LastName).ThenBy(identity => identity.FirstName);

        return await query.GetPageAsync(request.Page, request.PageSize, cancellationToken);
    }
}
