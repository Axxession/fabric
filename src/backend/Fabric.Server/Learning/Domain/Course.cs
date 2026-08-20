using Fabric.Server.Core;

namespace Fabric.Server.Learning.Domain;

public sealed class Course
{
    private Course() { }

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid? CurrentVersionId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Course, CourseErrors> Create(string code, string title, string? description, DateTimeOffset now)
    {
        Result<CourseErrors> validation = Validate(code, title);
        if (validation.IsFailure(out CourseErrors error))
            return Result.Failure<Course, CourseErrors>(error);

        return Result.Success<Course, CourseErrors>(new Course
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Title = title.Trim(),
            Description = NormalizeOptional(description),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<CourseErrors> UpdateMetadata(string title, string? description, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(CourseErrors.CourseTitleRequired);

        Title = title.Trim();
        Description = NormalizeOptional(description);
        UpdatedAt = now;
        return Result.Success<CourseErrors>();
    }

    public void SetCurrentVersion(Guid courseVersionId, DateTimeOffset now)
    {
        CurrentVersionId = courseVersionId;
        UpdatedAt = now;
    }

    public Result<CourseErrors> Activate(DateTimeOffset now)
    {
        if (IsActive)
            return Result.Failure(CourseErrors.CourseAlreadyActive);

        IsActive = true;
        UpdatedAt = now;
        return Result.Success<CourseErrors>();
    }

    public Result<CourseErrors> Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
            return Result.Failure(CourseErrors.CourseAlreadyInactive);

        IsActive = false;
        UpdatedAt = now;
        return Result.Success<CourseErrors>();
    }

    private static Result<CourseErrors> Validate(string code, string title)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(CourseErrors.CourseCodeRequired);

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure(CourseErrors.CourseTitleRequired);

        return Result.Success<CourseErrors>();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
