using Fabric.Server.Core;
using Fabric.Server.Identities.Application;
using Fabric.Server.Identities.Domain;
using Fabric.Server.Visitors.Contracts;
using Fabric.Server.Visitors.Domain;
using Fabric.Server.Visitors.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Visitors.Application;

public class VisitService(VisitorsDbContext db, HostService hostService, TimeProvider timeProvider, IdentityService identityService)
{
    private async Task<Visit?> GetVisitAggregate(Guid visitId, CancellationToken cancellationToken = default)
    {
        return await db.Visits
            .Include(x => x.Invitations)
            .SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);
    }

    public async Task<Result<(Visit, HostResponse), VisitErrors>> Create(
            Guid hostEmployeeId,
            string summary,
            DateTimeOffset start,
            DateTimeOffset end,
            Guid? locationId,
            CancellationToken cancellationToken = default)
    {
        bool isHost = await hostService.IsEmployeeHostAsync(hostEmployeeId, cancellationToken);
        if (!isHost)
            return Result.Failure<(Visit, HostResponse), VisitErrors>(VisitErrors.HostNotFound);

        HostResponse? host = await hostService.GetHostByEmployeeIdAsync(hostEmployeeId, cancellationToken);
        if (host is null)
            return Result.Failure<(Visit, HostResponse), VisitErrors>(VisitErrors.HostNotFound);

        Result<Visit, VisitErrors> result = Visit.Create(hostEmployeeId, summary, start, end, locationId, timeProvider.GetUtcNow());

        if (result.IsSuccess(out Visit visit))
        {
            db.Visits.Add(visit);
            await db.SaveChangesAsync(cancellationToken);
        }

        return result.Map(v => (v, host));
    }

    public async Task<Result<VisitErrors>> ReassignHost(Guid visitId, Guid hostEmployeeId,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);

        bool isHost = await hostService.IsEmployeeHostAsync(hostEmployeeId, cancellationToken);
        if (!isHost)
            return Result.Failure(VisitErrors.HostNotFound);

        Result<VisitErrors> result = visit.ReassignHost(hostEmployeeId);

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;

    }

    public async Task<Result<VisitErrors>> Cancel(Guid visitId, CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);

        Result<VisitErrors> result = visit.Cancel();
        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<VisitErrors>> Complete(Guid visitId, CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);
        Result<VisitErrors> result = visit.Complete(timeProvider.GetUtcNow());

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }


    public async Task<Result<VisitErrors>> Reschedule(Guid visitId, DateTimeOffset start, DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);

        Result<VisitErrors> result = visit.Reschedule(start, end, timeProvider.GetUtcNow());

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<VisitErrors>> UpdateSummary(Guid visitId, string summary,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);

        Result<VisitErrors> result = visit.UpdateSummary(summary);

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<VisitErrors>> Relocate(Guid visitId, Guid? locationId,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);

        Result<VisitErrors> result = visit.Relocate(locationId);

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<VisitInvitation, VisitErrors>> Invite(Guid visitId, string firstName, string lastName,
        string email, string company, CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure<VisitInvitation, VisitErrors>(VisitErrors.VisitNotFound);

        Visitor? visitor = await db.Visitors
            .Where(x => x.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (visitor is null)
        {
            Guid visitorId = Guid.NewGuid();
            Result<Identity, IdentityErrors> identityResult = await identityService.UpsertFromVisitorAsync(null, visitorId, firstName, lastName, email, cancellationToken);
            if (identityResult.IsFailure(out _))
                return Result.Failure<VisitInvitation, VisitErrors>(VisitErrors.IdentitySyncFailed);

            identityResult.IsSuccess(out Identity identity);
            visitor = Visitor.Create(visitorId, identity.Id, firstName, lastName, email, company);
            db.Visitors.Add(visitor);
        }
        else
        {
            Result<Identity, IdentityErrors> identityResult = await identityService.UpsertFromVisitorAsync(visitor.IdentityId, visitor.Id, firstName, lastName, email, cancellationToken);
            if (identityResult.IsFailure(out _))
                return Result.Failure<VisitInvitation, VisitErrors>(VisitErrors.IdentitySyncFailed);

            visitor.UpdateProfile(firstName, lastName, email, company, visitor.LicensePlate);
        }

        Result<VisitInvitation, VisitErrors> result = visit.AddInvitation(visitor.Id, firstName, lastName, email, company);

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }


    public async Task<Result<VisitErrors>> RejectInvitation(Guid visitId, Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);

        Result<VisitErrors> result = visit.RejectParticipation(invitationId, timeProvider.GetUtcNow());

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    public async Task<Result<Visitor, VisitErrors>> AcceptInvitation(Guid visitId, Guid invitationId,
        string firstName, string lastName, string email, string company, ModeOfTransport transport, string? licensePlate, CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure<Visitor, VisitErrors>(VisitErrors.VisitNotFound);

        VisitInvitation? invitation = visit.Invitations.SingleOrDefault(x => x.Id == invitationId);

        if (invitation is null)
            return Result.Failure<Visitor, VisitErrors>(VisitErrors.InvitationNotFound);

        Result<VisitErrors> result = visit.ConfirmParticipation(invitationId, transport, licensePlate,
            timeProvider.GetUtcNow());

        if (result.IsFailure(out VisitErrors error))
            return Result.Failure<Visitor, VisitErrors>(error);

        Guid visitorId = invitation.VisitorId;
        Visitor? visitor = await db.Visitors
            .Where(x => x.Id == visitorId)
            .FirstOrDefaultAsync(cancellationToken);

        if (visitor is null)
        {
            Result<Identity, IdentityErrors> identityResult = await identityService.UpsertFromVisitorAsync(null, visitorId, firstName, lastName, email, cancellationToken);
            if (identityResult.IsFailure(out _))
                return Result.Failure<Visitor, VisitErrors>(VisitErrors.IdentitySyncFailed);

            identityResult.IsSuccess(out Identity identity);
            visitor = Visitor.Create(visitorId, identity.Id, firstName, lastName, email, company, licensePlate);
            db.Visitors.Add(visitor);
        }
        else
        {
            Result<Identity, IdentityErrors> identityResult = await identityService.UpsertFromVisitorAsync(visitor.IdentityId, visitor.Id, firstName, lastName, email, cancellationToken);
            if (identityResult.IsFailure(out _))
                return Result.Failure<Visitor, VisitErrors>(VisitErrors.IdentitySyncFailed);

            visitor.UpdateProfile(firstName, lastName, email, company, licensePlate);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success<Visitor, VisitErrors>(visitor);
    }

    public async Task<Result<VisitErrors>> MarkVisitorArrived(Guid visitId, Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        Visit? visit = await GetVisitAggregate(visitId, cancellationToken);

        if (visit is null)
            return Result.Failure(VisitErrors.VisitNotFound);

        Result<VisitErrors> result = visit.MarkVisitorArrived(invitationId, timeProvider.GetUtcNow());

        if (result.IsSuccess(out _))
            await db.SaveChangesAsync(cancellationToken);

        return result;

    }

}
