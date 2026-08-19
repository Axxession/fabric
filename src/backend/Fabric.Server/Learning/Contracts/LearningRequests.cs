using Fabric.Server.Core;
using Fabric.Server.Learning.Domain;
using Microsoft.AspNetCore.Http;

namespace Fabric.Server.Learning.Contracts;

public sealed record ListCoursesRequest : BaseListRequest
{
    public Guid[]? Ids { get; set; }
    public string? Query { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record CreateCourseRequest(string Code, string Title, string? Description);

public sealed record CreateCourseLanguageRequest(string LanguageCode, string DisplayLabel);

public sealed record UpdateCourseLanguageRequest(string LanguageCode, string DisplayLabel, bool IsActive);

public sealed class CreateCourseUploadRequest
{
    public string Code { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public IFormFile File { get; set; } = null!;
}

public sealed class CreateCourseVersionUploadRequest
{
    public string? Title { get; set; }
    public IFormFile File { get; set; } = null!;
}

public sealed record UpdateCourseRequest(string Title, string? Description);

public sealed record CreateEnrollmentRequest(Guid CourseId, Guid IdentityId);

public sealed record CancelEnrollmentRequest(string? Reason);

public sealed record ListEnrollmentsRequest : BaseListRequest
{
    public Guid? CourseId { get; set; }
    public Guid? IdentityId { get; set; }
    public EnrollmentStatus? Status { get; set; }
}

public sealed record CreateLaunchSessionRequest(Guid EnrollmentId, Guid? ScoId);

public sealed record RecordScormProgressRequest(
    string Token,
    Guid? ScoId,
    string? CompletionStatus,
    string? SuccessStatus,
    decimal? Score,
    decimal? ScoreScaled,
    string? BookmarkLocation,
    string? SessionTime,
    string? SuspendData,
    bool IsCompleted,
    string RawCmiData);
