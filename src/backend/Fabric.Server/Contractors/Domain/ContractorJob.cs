using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Domain;

public sealed class ContractorJob
{
    private readonly List<ContractorJobAssignment> _assignments = [];

    private ContractorJob() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid JobTypeId { get; private set; }
    public Guid LocationId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTimeOffset PlannedStart { get; private set; }
    public DateTimeOffset PlannedEnd { get; private set; }
    public ContractorJobStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<ContractorJobAssignment> Assignments => _assignments;

    public static Result<ContractorJob, ContractorJobErrors> Create(
        Guid companyId,
        Guid jobTypeId,
        Guid locationId,
        string name,
        string? description,
        DateTimeOffset plannedStart,
        DateTimeOffset plannedEnd,
        DateTimeOffset now)
    {
        Result<ContractorJobErrors> validation = Validate(name, plannedStart, plannedEnd);
        if (validation.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJob, ContractorJobErrors>(error);

        return Result.Success<ContractorJob, ContractorJobErrors>(new ContractorJob
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            JobTypeId = jobTypeId,
            LocationId = locationId,
            Name = name.Trim(),
            Description = NormalizeOptional(description),
            PlannedStart = plannedStart,
            PlannedEnd = plannedEnd,
            Status = ContractorJobStatus.Planned,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<ContractorJobErrors> Update(
        Guid companyId,
        Guid jobTypeId,
        Guid locationId,
        string name,
        string? description,
        DateTimeOffset plannedStart,
        DateTimeOffset plannedEnd,
        DateTimeOffset now)
    {
        if (Status == ContractorJobStatus.Completed)
            return Result.Failure(ContractorJobErrors.ContractorJobCompleted);

        if (Status == ContractorJobStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.ContractorJobCancelled);

        Result<ContractorJobErrors> validation = Validate(name, plannedStart, plannedEnd);
        if (validation.IsFailure(out ContractorJobErrors error))
            return Result.Failure(error);

        if (_assignments.Any(assignment => assignment.AssignedUntil > plannedEnd))
            return Result.Failure(ContractorJobErrors.AssignmentEndsAfterJobEnds);

        CompanyId = companyId;
        JobTypeId = jobTypeId;
        LocationId = locationId;
        Name = name.Trim();
        Description = NormalizeOptional(description);
        PlannedStart = plannedStart;
        PlannedEnd = plannedEnd;
        UpdatedAt = now;
        return Result.Success<ContractorJobErrors>();
    }

    public Result<ContractorJobErrors> Activate(DateTimeOffset now)
    {
        if (Status == ContractorJobStatus.Active)
            return Result.Failure(ContractorJobErrors.ContractorJobAlreadyActive);

        if (Status == ContractorJobStatus.Completed)
            return Result.Failure(ContractorJobErrors.ContractorJobCompleted);

        if (Status == ContractorJobStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.ContractorJobCancelled);

        Status = ContractorJobStatus.Active;
        UpdatedAt = now;
        return Result.Success<ContractorJobErrors>();
    }

    public Result<ContractorJobErrors> Complete(DateTimeOffset now)
    {
        if (Status == ContractorJobStatus.Completed)
            return Result.Failure(ContractorJobErrors.ContractorJobCompleted);

        if (Status == ContractorJobStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.ContractorJobCancelled);

        Status = ContractorJobStatus.Completed;
        UpdatedAt = now;
        foreach (ContractorJobAssignment assignment in _assignments)
            assignment.ForceComplete();

        return Result.Success<ContractorJobErrors>();
    }

    public Result<ContractorJobErrors> Cancel(DateTimeOffset now)
    {
        if (Status == ContractorJobStatus.Completed)
            return Result.Failure(ContractorJobErrors.ContractorJobCompleted);

        if (Status == ContractorJobStatus.Cancelled)
            return Result.Failure(ContractorJobErrors.ContractorJobCancelled);

        Status = ContractorJobStatus.Cancelled;
        UpdatedAt = now;
        foreach (ContractorJobAssignment assignment in _assignments)
            assignment.ForceCancel();

        return Result.Success<ContractorJobErrors>();
    }

    public Result<ContractorJobAssignment, ContractorJobErrors> AddAssignment(Guid contractorId, DateTimeOffset assignedFrom, DateTimeOffset assignedUntil, DateTimeOffset now)
    {
        if (Status == ContractorJobStatus.Completed)
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(ContractorJobErrors.ContractorJobCompleted);

        if (Status == ContractorJobStatus.Cancelled)
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(ContractorJobErrors.ContractorJobCancelled);

        Result<ContractorJobAssignment, ContractorJobErrors> create = ContractorJobAssignment.Create(contractorId, assignedFrom, assignedUntil, PlannedEnd);
        if (create.IsFailure(out ContractorJobErrors error))
            return Result.Failure<ContractorJobAssignment, ContractorJobErrors>(error);

        create.IsSuccess(out ContractorJobAssignment assignment);
        assignment.ContractorJobId = Id;
        _assignments.Add(assignment);
        UpdatedAt = now;
        return Result.Success<ContractorJobAssignment, ContractorJobErrors>(assignment);
    }

    public Result<ContractorJobErrors> UpdateAssignment(Guid assignmentId, DateTimeOffset assignedFrom, DateTimeOffset assignedUntil, DateTimeOffset now)
    {
        ContractorJobAssignment? assignment = _assignments.SingleOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
            return Result.Failure(ContractorJobErrors.AssignmentNotFound);

        Result<ContractorJobErrors> result = assignment.Update(assignedFrom, assignedUntil, PlannedEnd);
        if (result.IsFailure(out ContractorJobErrors error))
            return Result.Failure(error);

        UpdatedAt = now;
        return Result.Success<ContractorJobErrors>();
    }

    public Result<ContractorJobErrors> ActivateAssignment(Guid assignmentId, DateTimeOffset now) =>
        UpdateAssignmentStatus(assignmentId, now, assignment => assignment.Activate());

    public Result<ContractorJobErrors> CompleteAssignment(Guid assignmentId, DateTimeOffset now) =>
        UpdateAssignmentStatus(assignmentId, now, assignment => assignment.Complete());

    public Result<ContractorJobErrors> CancelAssignment(Guid assignmentId, DateTimeOffset now) =>
        UpdateAssignmentStatus(assignmentId, now, assignment => assignment.Cancel());

    private Result<ContractorJobErrors> UpdateAssignmentStatus(
        Guid assignmentId,
        DateTimeOffset now,
        Func<ContractorJobAssignment, Result<ContractorJobErrors>> action)
    {
        ContractorJobAssignment? assignment = _assignments.SingleOrDefault(item => item.Id == assignmentId);
        if (assignment is null)
            return Result.Failure(ContractorJobErrors.AssignmentNotFound);

        Result<ContractorJobErrors> result = action(assignment);
        if (result.IsFailure(out ContractorJobErrors error))
            return Result.Failure(error);

        UpdatedAt = now;
        return Result.Success<ContractorJobErrors>();
    }

    private static Result<ContractorJobErrors> Validate(string name, DateTimeOffset plannedStart, DateTimeOffset plannedEnd)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(ContractorJobErrors.NameRequired);

        if (plannedEnd <= plannedStart)
            return Result.Failure(ContractorJobErrors.PlannedEndMustBeAfterStart);

        return Result.Success<ContractorJobErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
