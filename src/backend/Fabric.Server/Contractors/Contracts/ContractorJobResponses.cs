using Fabric.Server.Contractors.Domain;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ContractorJobResponse(
    Guid Id,
    Guid CompanyId,
    Guid JobTypeId,
    Guid LocationId,
    Guid CreatedByIdentityId,
    string Name,
    string? Description,
    DateTimeOffset PlannedStart,
    DateTimeOffset PlannedEnd,
    ContractorJobStatus Status,
    int AssignmentCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class ContractorJobMapper
{
    public static ContractorJobResponse ToResponse(this ContractorJob job) =>
        new(
            job.Id,
            job.CompanyId,
            job.JobTypeId,
            job.LocationId,
            job.CreatedByIdentityId,
            job.Name,
            job.Description,
            job.PlannedStart,
            job.PlannedEnd,
            job.Status,
            job.Assignments.Count,
            job.CreatedAt,
            job.UpdatedAt);
}
