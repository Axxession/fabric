using Fabric.Server.Core;

namespace Fabric.Server.Learning.Domain;

public sealed class CourseLanguage
{
    private CourseLanguage() { }

    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }
    public string LanguageCode { get; private set; } = null!;
    public string DisplayLabel { get; private set; } = null!;
    public Guid? CurrentVersionId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<CourseLanguage, CourseErrors> Create(Guid courseId, string languageCode, string displayLabel, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return Result.Failure<CourseLanguage, CourseErrors>(CourseErrors.CourseLanguageCodeRequired);
        if (string.IsNullOrWhiteSpace(displayLabel))
            return Result.Failure<CourseLanguage, CourseErrors>(CourseErrors.CourseLanguageDisplayLabelRequired);

        return Result.Success<CourseLanguage, CourseErrors>(new CourseLanguage
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            LanguageCode = languageCode.Trim(),
            DisplayLabel = displayLabel.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result<CourseErrors> Update(string languageCode, string displayLabel, bool isActive, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return Result.Failure(CourseErrors.CourseLanguageCodeRequired);
        if (string.IsNullOrWhiteSpace(displayLabel))
            return Result.Failure(CourseErrors.CourseLanguageDisplayLabelRequired);

        LanguageCode = languageCode.Trim();
        DisplayLabel = displayLabel.Trim();
        IsActive = isActive;
        UpdatedAt = now;
        return Result.Success<CourseErrors>();
    }

    public void SetCurrentVersion(Guid courseVersionId, DateTimeOffset now)
    {
        CurrentVersionId = courseVersionId;
        UpdatedAt = now;
    }
}
