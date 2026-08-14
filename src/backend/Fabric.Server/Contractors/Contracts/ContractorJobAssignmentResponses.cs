using Fabric.Server.Contractors.Domain;

namespace Fabric.Server.Contractors.Contracts;

public sealed record ContractorJobAssignmentResponse(
    Guid Id,
    Guid ContractorJobId,
    Guid ContractorId,
    DateTimeOffset AssignedFrom,
    DateTimeOffset AssignedUntil,
    ContractorJobAssignmentStatus Status);

public static class ContractorJobAssignmentMapper
{
    public static ContractorJobAssignmentResponse ToResponse(this ContractorJobAssignment assignment) =>
        new(assignment.Id, assignment.ContractorJobId, assignment.ContractorId, assignment.AssignedFrom, assignment.AssignedUntil, assignment.Status);
}
