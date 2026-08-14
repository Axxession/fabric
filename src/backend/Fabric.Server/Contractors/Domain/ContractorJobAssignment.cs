using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Domain;

public sealed class ContractorJobAssignment
{
    private ContractorJobAssignment() { }

    public Guid Id { get; internal set; }
    public Guid ContractorJobId { get; internal set; }
    public Guid ContractorId { get; internal set; }
    public DateTimeOffset AssignedFrom { get; internal set; }
    public DateTimeOffset AssignedUntil { get; internal set; }
    public ContractorJobAssignmentStatus Status { get; internal set; }

    internal static Result<ContractorJobAssignment, ContractorJobErrors> Create(Guid contractorId, DateTimeOffset assignedFrom, DateTimeOffset assignedUntil, DateTimeOffset plannedEnd)
    {
        Result<ContractorJobErrors> validation = ValidateWindow(assignedFrom, assignedUntil, plannedEnd);
        if (validation.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(error);

        return Result.Success<ContractorJobAssignment, ContractorJobErrors>(new ContractorJobAssignment
        {
            Id = Guid.NewGuid(),
            ContractorId = contractorId,
            AssignedFrom = assignedFrom,
            AssignedUntil = assignedUntil,
            Status = ContractorJobAssignmentStatus.Planned,
        });
    }

    internal Result<ContractorJobErrors> Update(DateTimeOffset assignedFrom, DateTimeOffset assignedUntil, DateTimeOffset plannedEnd)
    {
        if (Status == ContractorJobAssignmentStatus.Completed)
            return Result.Failure(ContractorJobErrors.AssignmentCompleted);

        if (Status == ContractorJobAssignmentStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.AssignmentCancelled);

        Result<ContractorJobErrors> validation = ValidateWindow(assignedFrom, assignedUntil, plannedEnd);
        if (validation.IsFailure(out ContractorJobErrors error))
            return Result.Failure(error);

        AssignedFrom = assignedFrom;
        AssignedUntil = assignedUntil;
        return Result.Success<ContractorJobErrors>();
    }

    internal Result<ContractorJobErrors> Activate()
    {
        if (Status == ContractorJobAssignmentStatus.Active)
            return Result.Failure(ContractorJobErrors.AssignmentAlreadyActive);

        if (Status == ContractorJobAssignmentStatus.Completed)
            return Result.Failure(ContractorJobErrors.AssignmentCompleted);

        if (Status == ContractorJobAssignmentStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.AssignmentCancelled);

        Status = ContractorJobAssignmentStatus.Active;
        return Result.Success<ContractorJobErrors>();
    }

    internal Result<ContractorJobErrors> Complete()
    {
        if (Status == ContractorJobAssignmentStatus.Completed)
            return Result.Failure(ContractorJobErrors.AssignmentCompleted);

        if (Status == ContractorJobAssignmentStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.AssignmentCancelled);

        Status = ContractorJobAssignmentStatus.Completed;
        return Result.Success<ContractorJobErrors>();
    }

    internal Result<ContractorJobErrors> Cancel()
    {
        if (Status == ContractorJobAssignmentStatus.Completed)
            return Result.Failure(ContractorJobErrors.AssignmentCompleted);

        if (Status == ContractorJobAssignmentStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.AssignmentCancelled);

        Status = ContractorJobAssignmentStatus.Cancelled;
        return Result.Success<ContractorJobErrors>();
    }

    internal void ForceComplete()
    {
        if (Status is ContractorJobAssignmentStatus.Completed or ContractorJobAssignmentStatus.Cancelled)
            return;

        Status = ContractorJobAssignmentStatus.Completed;
    }

    internal void ForceCancel()
    {
        if (Status is ContractorJobAssignmentStatus.Completed or ContractorJobAssignmentStatus.Cancelled)
            return;

        Status = ContractorJobAssignmentStatus.Cancelled;
    }

    private static Result<ContractorJobErrors> ValidateWindow(DateTimeOffset assignedFrom, DateTimeOffset assignedUntil, DateTimeOffset plannedEnd)
    {
        if (assignedUntil <= assignedFrom)
            return Result.Failure(ContractorJobErrors.AssignmentUntilMustBeAfterFrom);

        if (assignedUntil > plannedEnd)
            return Result.Failure(ContractorJobErrors.AssignmentEndsAfterJobEnds);

        return Result.Success<ContractorJobErrors>();
    }
}
