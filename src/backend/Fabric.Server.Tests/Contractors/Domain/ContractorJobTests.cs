using Fabric.Server.Contractors.Domain;
using Fabric.Server.Core;

namespace Fabric.Server.Tests.Contractors.Domain;

public sealed class ContractorJobTests
{
    [Fact]
    public void AddAssignment_WhenAssignedUntilAfterJobEnd_ReturnsFailure()
    {
        DateTimeOffset now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);
        Result<ContractorJob, ContractorJobErrors> create = ContractorJob.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Boiler repair",
            null,
            now,
            now.AddHours(8),
            now);
        create.IsSuccess(out ContractorJob job);

        Result<ContractorJobAssignment, ContractorJobErrors> result = job.AddAssignment(
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(9),
            now);

        Assert.True(result.IsFailure(out ContractorJobErrors error));
        Assert.Equal(ContractorJobErrors.AssignmentEndsAfterJobEnds, error);
    }

    [Fact]
    public void Complete_WhenAssignmentsExist_CompletesOpenAssignments()
    {
        DateTimeOffset now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);
        Result<ContractorJob, ContractorJobErrors> create = ContractorJob.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Panel replacement",
            null,
            now,
            now.AddHours(8),
            now);
        create.IsSuccess(out ContractorJob job);

        Result<ContractorJobAssignment, ContractorJobErrors> addAssignment = job.AddAssignment(
            Guid.NewGuid(),
            now.AddHours(1),
            now.AddHours(6),
            now);
        addAssignment.IsSuccess(out ContractorJobAssignment assignment);
        Result<ContractorJobErrors> activate = job.ActivateAssignment(assignment.Id, now);
        Assert.True(activate.IsSuccess(out _));

        Result<ContractorJobErrors> result = job.Complete(now.AddHours(7));

        Assert.True(result.IsSuccess(out _));
        Assert.Equal(ContractorJobStatus.Completed, job.Status);
        Assert.Equal(ContractorJobAssignmentStatus.Completed, assignment.Status);
    }
}
