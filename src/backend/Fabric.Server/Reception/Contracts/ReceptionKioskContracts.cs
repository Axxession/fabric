using Fabric.Server.Reception.Domain;
using Fabric.Server.Visitors.Domain;
using Riok.Mapperly.Abstractions;

namespace Fabric.Server.Reception.Contracts;

public record ReceptionKioskResponse(
    Guid Id,
    string Name,
    Guid LocationId,
    bool Enabled,
    bool RequireFacePicture,
    IdentityVerificationMethod? IdentityVerificationMethod,
    int OnboardingGracePeriodMinutes
);

public record CreateReceptionKioskRequest(
    string Name,
    Guid LocationId,
    bool RequireFacePicture,
    IdentityVerificationMethod? IdentityVerificationMethod,
    int OnboardingGracePeriodMinutes
);

public record UpdateReceptionKioskRequest(
    string Name,
    Guid LocationId,
    bool Enabled,
    bool RequireFacePicture,
    IdentityVerificationMethod? IdentityVerificationMethod,
    int OnboardingGracePeriodMinutes
);

public record ReceptionKioskKeyResponse(
    ReceptionKioskResponse Kiosk,
    string ApiKey
);

public record ReceptionKioskExpectedArrivalResponse(
    Guid Id,
    ArrivalType Type,
    DateTimeOffset ExpectedArrivalTime,
    DateTimeOffset ExpectedOffboardTime,
    string FirstName,
    string LastName,
    string? Company,
    OnboardingStatus Status,
    bool CheckedIn,
    Guid? LocationId,
    ReceptionKioskOnboardingRequirementsResponse OnboardingRequirements,
    ReceptionKioskVisitorDetailsResponse? Visitor,
    ReceptionKioskContractorDetailsResponse? Contractor
);

public record ReceptionKioskOnboardingRequirementsResponse(
    bool RequireFacePicture,
    IdentityVerificationMethod? IdentityVerificationMethod
);

public record ReceptionKioskVisitorDetailsResponse(
    Guid VisitorId,
    Guid InvitationId,
    string Email,
    ParticipantConfirmationStatus ConfirmationStatus,
    ModeOfTransport? Transport,
    string? LicensePlate,
    ReceptionKioskVisitDetailsResponse? Visit
);

public record ReceptionKioskVisitDetailsResponse(
    Guid Id,
    string Summary,
    VisitStatus Status,
    DateTimeOffset Start,
    DateTimeOffset Stop,
    Guid? LocationId,
    string HostName,
    string? HostEmail
);

public record ReceptionKioskContractorDetailsResponse();

[Mapper]
public static partial class ReceptionKioskMapper
{
    [MapperIgnoreSource(nameof(ReceptionKiosk.ApiKeyHash))]
    [MapperIgnoreSource(nameof(ReceptionKiosk.ApiKeySalt))]
    public static partial ReceptionKioskResponse ToResponse(this ReceptionKiosk kiosk);

    public static ReceptionKioskExpectedArrivalResponse ToKioskResponse(
        this ExpectedArrival arrival,
        ReceptionKiosk kiosk,
        ReceptionKioskVisitorDetailsResponse? visitor = null,
        ReceptionKioskContractorDetailsResponse? contractor = null) =>
        new(
            arrival.Id,
            arrival.Type,
            arrival.ExpectedArrivalTime,
            arrival.ExpectedOffboardTime,
            arrival.FirstName,
            arrival.LastName,
            arrival.Company,
            arrival.Status,
            arrival.CheckedIn,
            arrival.LocationId,
            new ReceptionKioskOnboardingRequirementsResponse(kiosk.RequireFacePicture, kiosk.IdentityVerificationMethod),
            visitor,
            contractor);
}
