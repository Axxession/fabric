using Fabric.Server.Locations.Domain;
using Fabric.Server.Locations.Persistence;
using Fabric.Server.Visitors.Domain;

namespace Fabric.Server.Sagas.VisitorPreOnboarding;

public class VisitorPreOnboardingSaga
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public Guid InvitationId { get; set; }
    public Guid? ArrivalId { get; set; }
    public Guid? AccessPolicyId { get; set; }
    public Guid? CredentialId { get; set; }
    public string? QrCode { get; set; }
    public DateTimeOffset? ArrivalNotificationSentAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? InvitationSentAt { get; set; }
    public VisitorPreOnboardingState State { get; set; }
}

public sealed class VisitorPreOnboardingSagaEvent
{
    private VisitorPreOnboardingSagaEvent() { }

    public Guid Id { get; private set; }
    public VisitorPreOnboardingSagaEventType Type { get; private set; }
    public Guid? SagaId { get; private set; }
    public Guid? VisitId { get; private set; }
    public Guid? InvitationId { get; private set; }
    public Guid? ArrivalId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public static VisitorPreOnboardingSagaEvent Create(
        VisitorPreOnboardingSagaEventType type,
        DateTimeOffset createdAt,
        Guid? sagaId = null,
        Guid? visitId = null,
        Guid? invitationId = null,
        Guid? arrivalId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            SagaId = sagaId,
            VisitId = visitId,
            InvitationId = invitationId,
            ArrivalId = arrivalId,
            CreatedAt = createdAt,
            RetryCount = 0,
        };

    public void MarkProcessed(DateTimeOffset timestamp)
    {
        ProcessedAt = timestamp;
        FailureReason = null;
        NextRetryAt = null;
    }

    public void ScheduleRetry(DateTimeOffset nextRetryAt, string? failureReason)
    {
        RetryCount++;
        NextRetryAt = nextRetryAt;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason;
    }
}

public enum VisitorPreOnboardingSagaEventType
{
    Started,
    VisitorConfirmed,
    VisitorRejected,
    VisitCancelled,
    VisitRescheduled,
    VisitRelocated,
    VisitorArrived,
}

public enum VisitorPreOnboardingState
{
    GeneratingQr,
    RegisteringArrival,
    SendingInvitation,
    AwaitingConfirmation,
    Confirmed,
    Rejected,
    Cancelling,
    Cancelled,
    Expired,
}

public class VisitorPreOnboardingSagaConfig
{
    public Guid Id { get; set; }
    public bool UseCustomInviteNotification { get; set; }
    public CustomNotification? CustomInviteNotification { get; set; }
    public Guid? QrCredentialTypeId { get; set; }
    public int GraceStartMinutes { get; set; }
    public int GraceEndMinutes { get; set; }
    public bool SendConfirmNotificationToHost { get; set; }
    public bool UseCustomConfirmNotification { get; set; }
    public CustomNotification? CustomConfirmNotification { get; set; }
    public bool SendCancellationNotification { get; set; }
    public bool UseCustomCancellationNotification { get; set; }
    public CustomNotification? CustomCancellationNotification { get; set; }
    public bool SendRescheduleNotification { get; set; }
    public bool UseCustomRescheduleNotification { get; set; }
    public CustomNotification? CustomRescheduleNotification { get; set; }
    public bool SendRelocationNotification { get; set; }
    public bool UseCustomRelocationNotification { get; set; }
    public CustomNotification? CustomRelocationNotification { get; set; }
    public bool SendArrivalNotificationToHost { get; set; }
    public bool UseCustomArrivalNotification { get; set; }
    public CustomNotification? CustomArrivalNotification { get; set; }

    public static VisitorPreOnboardingSagaConfig Default => new()
    {
        Id = Guid.Empty,
        UseCustomInviteNotification = false,
        CustomInviteNotification = null,
        QrCredentialTypeId = null,
        GraceStartMinutes = 30,
        GraceEndMinutes = 30,
        SendConfirmNotificationToHost = false,
        UseCustomConfirmNotification = false,
        CustomConfirmNotification = null,
        SendCancellationNotification = false,
        UseCustomCancellationNotification = false,
        CustomCancellationNotification = null,
        SendRescheduleNotification = false,
        UseCustomRescheduleNotification = false,
        CustomRescheduleNotification = null,
        SendRelocationNotification = false,
        UseCustomRelocationNotification = false,
        CustomRelocationNotification = null,
        SendArrivalNotificationToHost = false,
        UseCustomArrivalNotification = false,
        CustomArrivalNotification = null,
    };
}

public sealed record CustomNotification
{
    public required string Subject { get; init; }
    public required string Body { get; init; }
}

public record SagaNotificationModel(VisitInvitation Visitor, VisitNotificationModel Visit, LocationNotificationModel? Location, string PlatformBaseUrl, string? QrCodeLink, string ConfirmationLink);

public record VisitNotificationModel(
    Guid Id,
    string Summary,
    Guid HostEmployeeId,
    VisitStatus Status,
    DateTimeOffset Start,
    DateTimeOffset Stop,
    Guid? LocationId)
{
    public static VisitNotificationModel FromVisit(Visit visit) => new(
        visit.Id,
        visit.Summary,
        visit.HostEmployeeId,
        visit.Status,
        visit.Start,
        visit.Stop,
        visit.LocationId);
}

public record LocationNotificationModel(
    Guid Id,
    LocationType Type,
    LocationPartNotificationModel Site,
    LocationPartNotificationModel? Building,
    LocationPartNotificationModel? Room,
    string DisplayName)
{
    public static LocationNotificationModel FromLocation(Location location) => location switch
    {
        Location.SiteLocation siteLocation => new(
            siteLocation.Id,
            LocationType.Site,
            LocationPartNotificationModel.FromSite(siteLocation.Site),
            null,
            null,
            siteLocation.Site.Name),
        Location.BuildingLocation buildingLocation => new(
            buildingLocation.Id,
            LocationType.Building,
            LocationPartNotificationModel.FromSite(buildingLocation.Site),
            LocationPartNotificationModel.FromBuilding(buildingLocation.Building),
            null,
            $"{buildingLocation.Site.Name} / {buildingLocation.Building.Name}"),
        Location.RoomLocation roomLocation => new(
            roomLocation.Id,
            LocationType.Room,
            LocationPartNotificationModel.FromSite(roomLocation.Site),
            LocationPartNotificationModel.FromBuilding(roomLocation.Building),
            LocationPartNotificationModel.FromRoom(roomLocation.Room),
            $"{roomLocation.Site.Name} / {roomLocation.Building.Name} / {roomLocation.Room.Name}"),
        _ => throw new InvalidOperationException("Unknown location type.")
    };
}

public record LocationPartNotificationModel(Guid Id, string Name, string? Address)
{
    public static LocationPartNotificationModel FromSite(Site site) => new(site.Id, site.Name, site.Address);

    public static LocationPartNotificationModel FromBuilding(Building building) => new(building.Id, building.Name, building.Address);

    public static LocationPartNotificationModel FromRoom(Room room) => new(room.Id, room.Name, null);
}
