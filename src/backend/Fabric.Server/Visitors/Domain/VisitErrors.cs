namespace Fabric.Server.Visitors.Domain;

public enum VisitErrors
{
    VisitNotFound,
    HostNotFound,
    LicensePlateRequired,
    InvalidStatus,
    Cancelled,
    Completed,
    DuplicateInvitationEmail,
    InvitationNotFound,
    InvitationAlreadyResponded,
    IdentitySyncFailed,
    AlreadyCancelled,
    StartMustBeBeforeStop,
    StopMustBeFuture
}
