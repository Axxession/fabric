using Fabric.Server.Contractors.Domain;

namespace Fabric.Server.Contractors.Contracts;

public sealed record JobTypeResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class JobTypeMapper
{
    public static JobTypeResponse ToResponse(this JobType jobType) =>
        new(jobType.Id, jobType.Code, jobType.Name, jobType.Description, jobType.IsActive, jobType.CreatedAt, jobType.UpdatedAt);
}
