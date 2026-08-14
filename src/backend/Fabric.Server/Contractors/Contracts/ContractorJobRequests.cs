using Fabric.Server.Contractors.Domain;
using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ListContractorJobsRequest : BaseListRequest
{
    public string? Query { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? JobTypeId { get; set; }
    public Guid? LocationId { get; set; }
    public ContractorJobStatus[]? Status { get; set; }
    public DateTimeOffset? PlannedStartAfter { get; set; }
    public DateTimeOffset? PlannedEndBefore { get; set; }
}

public sealed record CreateContractorJobRequest(
    Guid CompanyId,
    Guid JobTypeId,
    Guid LocationId,
    string Name,
    string? Description,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd);

public sealed record UpdateContractorJobRequest(
    Guid CompanyId,
    Guid JobTypeId,
    Guid LocationId,
    string Name,
    string? Description,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd);
