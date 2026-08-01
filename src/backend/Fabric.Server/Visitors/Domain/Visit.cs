using Fabric.Server.Core;

namespace Fabric.Server.Visitors.Domain;

public enum VisitStatus
{
    Scheduled,
    Cancelled,
    Completed
}

public sealed class Visit
{
    private Visit() { }

    public Guid Id { get; private set; }
    public string Summary { get; private set; } = null!;
    public Guid HostEmployeeId { get; private set; }
    public VisitStatus Status { get; private set; }

    public DateTimeOffset Start { get; private set; }
    public DateTimeOffset Stop { get; private set; }
    public Guid? LocationId { get; private set; }

    public List<VisitInvitation> Invitations { get; private set; } = [];

    public static Result<Visit, VisitErrors> Create(
        Guid hostEmployeeId,
        string summary,
        DateTimeOffset start,
        DateTimeOffset stop,
        Guid? locationId,
        DateTimeOffset now)
    {
        Result<VisitErrors> validation = ValidateTimeRange(start, stop, now);
        if (validation.IsFailure(out VisitErrors error))
            return Result.Failure<Visit,VisitErrors>(error);

        return Result.Success<Visit, VisitErrors>(new Visit
        {
            Id = Guid.NewGuid(),
            HostEmployeeId = hostEmployeeId,
            Summary = summary,
            Status = VisitStatus.Scheduled,
            Start = start,
            Stop = stop,
            LocationId = locationId,
            Invitations = []
        });
    }

    public Result<VisitInvitation, VisitErrors> AddInvitation(Guid visitorId, string firstName, string lastName, string email, string company)
    {
        Result<VisitErrors> guard = GuardScheduled();
        if (guard.IsFailure(out VisitErrors error))
            return Result.Failure<VisitInvitation, VisitErrors>(error);

        if (Invitations.Any(x => x.Email == email))
            return Result.Failure<VisitInvitation, VisitErrors>(VisitErrors.DuplicateInvitationEmail);

        var invitation = VisitInvitation.Create(Guid.NewGuid(), visitorId, firstName, lastName, email, company);
        Invitations.Add(invitation);

        return Result.Success<VisitInvitation, VisitErrors>(invitation);
    }

    public Result<VisitErrors> ConfirmParticipation(
        Guid invitationId,
        ModeOfTransport modeOfTransport,
        string? licensePlate,
        DateTimeOffset timestamp)
    {
        Result<VisitErrors> guard = GuardScheduled();

        if (guard.IsFailure(out _))
            return guard;

        VisitInvitation? invitation = Invitations.SingleOrDefault(x => x.Id == invitationId);

        if (invitation is null)
            return Result.Failure<VisitErrors>(VisitErrors.InvitationNotFound);

        return invitation.Confirm(modeOfTransport, licensePlate, timestamp);
    }

    public Result<VisitErrors> RejectParticipation(Guid invitationId, DateTimeOffset timestamp)
    {
        Result<VisitErrors> guard = GuardScheduled();

        if (guard.IsFailure(out _))
            return guard;

        VisitInvitation? invitation = Invitations.SingleOrDefault(x => x.Id == invitationId);
        if (invitation is null)
            return Result.Failure(VisitErrors.InvitationNotFound);

        return invitation.Reject(timestamp);
    }

    public Result<VisitErrors> Cancel()
    {
        if (Status == VisitStatus.Cancelled)
            return Result.Failure(VisitErrors.AlreadyCancelled);

        if (Status == VisitStatus.Completed)
            return Result.Failure(VisitErrors.Completed);

        Status = VisitStatus.Cancelled;
        return Result.Success<VisitErrors>();
    }

    public Result<VisitErrors> Complete(DateTimeOffset timestamp)
    {
        if (Status == VisitStatus.Cancelled)
            return Result.Failure(VisitErrors.Cancelled);

        if (Status == VisitStatus.Completed)
            return Result.Failure(VisitErrors.Completed);

        foreach (VisitInvitation invitation in Invitations)
            invitation.MarkNoShow(timestamp);

        Status = VisitStatus.Completed;
        return Result.Success<VisitErrors>();
    }

    public Result<VisitErrors> MarkVisitorArrived(Guid invitationId, DateTimeOffset timestamp)
    {
        Result<VisitErrors> guard = GuardScheduled();
        if (guard.IsFailure(out _))
            return guard;

        VisitInvitation? invitation = Invitations.SingleOrDefault(x => x.Id == invitationId);
        if (invitation is null)
            return Result.Failure(VisitErrors.InvitationNotFound);

        invitation.MarkArrived(timestamp);
        return Result.Success<VisitErrors>();
    }

    public Result<VisitErrors> Reschedule(DateTimeOffset start, DateTimeOffset stop, DateTimeOffset now)
    {
        Result<VisitErrors> guard = GuardScheduled();
        if (guard.IsFailure(out VisitErrors _))
            return guard;

        Result<VisitErrors> validation = ValidateTimeRange(start, stop, now);
        if (validation.IsFailure(out _))
            return validation;

        Start = start;
        Stop = stop;
        return Result.Success<VisitErrors>();
    }

    public Result<VisitErrors> Relocate(Guid? locationId)
    {
        Result<VisitErrors> guard = GuardScheduled();
        if (guard.IsFailure(out _))
            return guard;

        LocationId = locationId;
        return Result.Success<VisitErrors>();
    }

    public Result<VisitErrors> ReassignHost(Guid hostEmployeeId)
    {
        Result<VisitErrors> guard = GuardScheduled();
        if (guard.IsFailure(out _))
            return guard;

        HostEmployeeId = hostEmployeeId;
        return Result.Success<VisitErrors>();
    }

    public Result<VisitErrors> UpdateSummary(string summary)
    {
        Result<VisitErrors> guard = GuardScheduled();
        if (guard.IsFailure(out _))
            return guard;

        Summary = summary;
        return Result.Success<VisitErrors>();
    }

    private Result<VisitErrors> GuardScheduled()
    {
        return Status switch
        {
            VisitStatus.Scheduled => Result.Success<VisitErrors>(),
            VisitStatus.Cancelled => Result.Failure(VisitErrors.Cancelled),
            VisitStatus.Completed => Result.Failure(VisitErrors.Completed),
            _ => Result.Failure(VisitErrors.InvalidStatus)
        };
    }

    private static Result<VisitErrors> ValidateTimeRange(
        DateTimeOffset start,
        DateTimeOffset stop,
        DateTimeOffset now)
    {
        if (start >= stop)
            return Result.Failure(VisitErrors.StartMustBeBeforeStop);

        if (stop <= now)
            return Result.Failure(VisitErrors.StopMustBeFuture);

        return Result.Success<VisitErrors>();
    }
}
