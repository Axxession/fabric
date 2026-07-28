using Fabric.Server.Core;
using Fabric.Server.Visitors.Domain;

namespace Fabric.Server.Visitors.Contracts;

public record ListVisitsRequest : BaseListRequest
{
    public List<VisitStatus> WithStatus { get; set; } = [];
    public Guid? HostEmployeeId { get; set; }
    public DateTimeOffset? After { get; set; }
    public DateTimeOffset? Before { get; set; }
}

public record ListVisitorsRequest : BaseListRequest
{
    public string? Query { get; set; }
}


public record CreateVisitRequest(
    string Summary,
    DateTimeOffset Start,
    DateTimeOffset Stop,
    Guid? LocationId
);

public record RescheduleVisitRequest(
    DateTimeOffset Start,
    DateTimeOffset Stop
);

public record RelocateVisitRequest(Guid? LocationId);

public record UpdateVisitSummaryRequest(string Summary);

public record InviteVisitRequest(
    string FirstName,
    string LastName,
    string Email,
    string Company
);

public record ConfirmInvitationRequest(
    string FirstName,
    string LastName,
    string Email,
    string Company,
    ModeOfTransport Transport,
    string? LicensePlate
);
