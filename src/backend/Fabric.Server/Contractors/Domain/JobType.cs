using Fabric.Server.Core;

namespace Fabric.Server.Contractors.Domain;

public sealed class JobType
{
    private JobType() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<JobType, JobTypeErrors> Create(string code, string name, string? description, DateTimeOffset now)
    {
        Result<JobTypeErrors> validation = Validate(code, name);
        if (validation.IsFailure(out JobTypeErrors error))
            return Result.Failure<JobType, JobTypeErrors>(error);

        return Result.Success<JobType, JobTypeErrors>(new JobType
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Description = NormalizeOptional(description),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<JobTypeErrors> Update(string code, string name, string? description, DateTimeOffset now)
    {
        Result<JobTypeErrors> validation = Validate(code, name);
        if (validation.IsFailure(out JobTypeErrors error))
            return Result.Failure(error);

        Code = code.Trim();
        Name = name.Trim();
        Description = NormalizeOptional(description);
        UpdatedAt = now;
        return Result.Success<JobTypeErrors>();
    }

    public Result<JobTypeErrors> Activate(DateTimeOffset now)
    {
        if (IsActive)
            return Result.Failure(JobTypeErrors.JobTypeAlreadyActive);

        IsActive = true;
        UpdatedAt = now;
        return Result.Success<JobTypeErrors>();
    }

    public Result<JobTypeErrors> Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
            return Result.Failure(JobTypeErrors.JobTypeAlreadyInactive);

        IsActive = false;
        UpdatedAt = now;
        return Result.Success<JobTypeErrors>();
    }

    private static Result<JobTypeErrors> Validate(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(JobTypeErrors.CodeRequired);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(JobTypeErrors.NameRequired);

        return Result.Success<JobTypeErrors>();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
