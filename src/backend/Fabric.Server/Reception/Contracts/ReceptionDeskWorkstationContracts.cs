using Fabric.Server.Reception.Domain;
using Riok.Mapperly.Abstractions;

namespace Fabric.Server.Reception.Contracts;

public record ReceptionDeskWorkstationResponse(
    Guid Id,
    string Name,
    Guid LocationId,
    bool Enabled
);

public record CreateReceptionDeskWorkstationRequest(
    string Name,
    Guid LocationId
);

public record UpdateReceptionDeskWorkstationRequest(
    string Name,
    Guid LocationId,
    bool Enabled
);

public record ReceptionDeskWorkstationKeyResponse(
    ReceptionDeskWorkstationResponse Workstation,
    string ApiKey
);

[Mapper]
public static partial class ReceptionDeskWorkstationMapper
{
    [MapperIgnoreSource(nameof(ReceptionDeskWorkstation.ApiKeyHash))]
    [MapperIgnoreSource(nameof(ReceptionDeskWorkstation.ApiKeySalt))]
    public static partial ReceptionDeskWorkstationResponse ToResponse(this ReceptionDeskWorkstation workstation);
}
