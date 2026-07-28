using Fabric.Server.Reception.Domain;
using Riok.Mapperly.Abstractions;

namespace Fabric.Server.Reception.Contracts;

public record ArrivalResponse(
    Guid Id,
    ArrivalType Type,
    DateTimeOffset ExpectedArrivalTime,
    DateTimeOffset ExpectedOffboardTime,
    string FirstName,
    string LastName,
    string? Company,
    string? ArrivalCode,
    OnboardingStatus Status,
    DateTimeOffset? OnboardedAt,
    ReceptionActorResponse? OnboardedBy,
    DateTimeOffset? OffboardedAt,
    ReceptionActorResponse? OffboardedBy,
    bool CheckedIn,
    Guid? LocationId,
    Guid? IdentityId,
    bool? Confirmed,
    Guid? VisitorId,
    Guid? InvitationId,
    Guid? ContractorId,
    Guid? JobAssignmentId,
    IReadOnlyCollection<ArrivalEntryResponse> Entries,
    IReadOnlyCollection<CheckInDocumentResponse> Documents
);

public record ArrivalEntryResponse(
    Guid Id,
    ArrivalEntryType Type,
    DateTimeOffset Timestamp,
    ReceptionActorResponse? Actor
);

public record ReceptionActorResponse(
    ReceptionActorType Type,
    string Identifier,
    string? DisplayName
);

public record CheckInDocumentResponse(
    Guid Id,
    string Name,
    CheckInDocumentType DocumentType
);

[Mapper]
public static partial class ArrivalMapper
{
    public static partial ArrivalResponse ToResponse(this ExpectedArrival arrival);
    public static partial ArrivalEntryResponse ToResponse(this ArrivalEntry entry);
    [MapperIgnoreSource(nameof(CheckInDocument.Content))]
    public static partial CheckInDocumentResponse ToResponse(this CheckInDocument document);

    private static ReceptionActorResponse? MapActor(ReceptionActor? actor) =>
        actor is null ? null : new ReceptionActorResponse(actor.Type, actor.Identifier, actor.DisplayName);
}
