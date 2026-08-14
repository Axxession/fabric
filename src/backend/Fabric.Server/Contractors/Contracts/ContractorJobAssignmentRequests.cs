using Fabric.Server.Contractors.Domain;
using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ListContractorJobAssignmentsRequest : BaseListRequest
{
    public Guid? ContractorId { get; set; }
    public ContractorJobAssignmentStatus[]? Status { get; set; }
    public DateTimeOffset? AssignedAfter { get; set; }
    public DateTimeOffset? AssignedBefore { get; set; }
}

public sealed record CreateContractorJobAssignmentRequest(Guid ContractorId, DateTimeOffset AssignedFrom, DateTimeOffset AssignedUntil);

public sealed record UpdateContractorJobAssignmentRequest(DateTimeOffset AssignedFrom, DateTimeOffset AssignedUntil);
