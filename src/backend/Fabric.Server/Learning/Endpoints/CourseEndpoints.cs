using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Fabric.Server.Core;
using Fabric.Server.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Learning.Endpoints;

public static class CourseEndpoints
{
    public static IEndpointRouteBuilder MapCourseEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder courses = app.MapGroup("/api/learning/courses")
            .RequireAuthorization(new AuthorizeAttribute { Roles = FabricRoleDefaults.AdminRole });

        courses.MapGet("", ListCourses).Produces<Page<CourseResponse>>();
        courses.MapGet("/{id:guid}", GetCourse).Produces<CourseResponse>().Produces(StatusCodes.Status404NotFound);
        courses.MapGet("/{id:guid}/languages", ListCourseLanguages).Produces<CourseLanguageResponse[]>().Produces(StatusCodes.Status404NotFound);
        courses.MapGet("/{id:guid}/languages/{languageId:guid}", GetCourseLanguage).Produces<CourseLanguageResponse>().Produces(StatusCodes.Status404NotFound);
        courses.MapGet("/{id:guid}/languages/{languageId:guid}/versions", ListCourseLanguageVersions).Produces<CourseVersionResponse[]>().Produces(StatusCodes.Status404NotFound);
        courses.MapPost("", CreateCourseDefinition).Produces<CourseResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        courses.MapPost("/{id:guid}/languages", CreateCourseLanguage).Produces<CourseLanguageResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        courses.MapPut("/{id:guid}/languages/{languageId:guid}", UpdateCourseLanguage).Produces<CourseLanguageResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        courses.MapGet("/{id:guid}/versions", ListCourseVersions).Produces<CourseVersionResponse[]>().Produces(StatusCodes.Status404NotFound);
        courses.MapGet("/{id:guid}/reporting", GetCourseReporting).Produces<CourseCompletionReportRowResponse[]>().Produces(StatusCodes.Status404NotFound);
        courses.MapPost("/upload", UploadCoursePackage).DisableAntiforgery().Produces<CourseVersionResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        courses.MapPut("/{id:guid}", UpdateCourse).Produces<CourseResponse>().Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound);
        courses.MapPost("/{id:guid}/versions", CreateCourseVersion).DisableAntiforgery().Produces<CourseVersionResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        courses.MapPost("/{id:guid}/languages/{languageId:guid}/versions", CreateCourseLanguageVersion).DisableAntiforgery().Produces<CourseVersionResponse>(StatusCodes.Status201Created).Produces<ProblemDetails>(StatusCodes.Status400BadRequest).Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        courses.MapPost("/{id:guid}/activate", ActivateCourse).Produces<CourseResponse>().Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);
        courses.MapPost("/{id:guid}/deactivate", DeactivateCourse).Produces<CourseResponse>().Produces<ProblemDetails>(StatusCodes.Status404NotFound).Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListCourses([AsParameters] ListCoursesRequest request, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        IQueryable<Course> query = db.Courses.AsNoTracking();
        if (request.Ids is { Length: > 0 })
            query = query.Where(item => request.Ids.Contains(item.Id));

        if (request.IsActive.HasValue)
            query = query.Where(item => item.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            string filter = $"%{request.Query}%";
            query = query.Where(item => EF.Functions.ILike(item.Code, filter) || EF.Functions.ILike(item.Title, filter));
        }

        IPaged<Course> page = await query.OrderBy(item => item.Title).ThenBy(item => item.Id).GetPageAsync(request.Page, request.PageSize, cancellationToken);
        return Results.Ok(page.Map(item => item.ToResponse()));
    }

    private static async Task<IResult> GetCourse(Guid id, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        Course? course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return course is null ? Results.NotFound() : Results.Ok(course.ToResponse());
    }

    private static async Task<IResult> ListCourseLanguages(Guid id, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
            return Results.NotFound();

        CourseLanguage[] languages = await db.CourseLanguages.AsNoTracking().Where(item => item.CourseId == id).OrderBy(item => item.DisplayLabel).ThenBy(item => item.LanguageCode).ToArrayAsync(cancellationToken);
        return Results.Ok(languages.Select(item => item.ToResponse()).ToArray());
    }

    private static async Task<IResult> GetCourseLanguage(Guid id, Guid languageId, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        CourseLanguage? language = await db.CourseLanguages.AsNoTracking().SingleOrDefaultAsync(item => item.Id == languageId && item.CourseId == id, cancellationToken);
        return language is null ? Results.NotFound() : Results.Ok(language.ToResponse());
    }

    private static async Task<IResult> ListCourseLanguageVersions(Guid id, Guid languageId, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.CourseLanguages.AsNoTracking().AnyAsync(item => item.Id == languageId && item.CourseId == id, cancellationToken))
            return Results.NotFound();

        CourseVersion[] versions = await db.CourseVersions.AsNoTracking().Where(item => item.CourseId == id && item.CourseLanguageId == languageId).OrderByDescending(item => item.VersionNumber).ToArrayAsync(cancellationToken);
        Guid[] versionIds = versions.Select(item => item.Id).ToArray();
        Dictionary<Guid, CourseSco[]> scosByVersionId = await db.CourseScos.AsNoTracking().Where(item => versionIds.Contains(item.CourseVersionId)).OrderBy(item => item.ManifestOrder).GroupBy(item => item.CourseVersionId).ToDictionaryAsync(group => group.Key, group => group.ToArray(), cancellationToken);
        return Results.Ok(versions.Select(version => version.ToResponse(scosByVersionId.GetValueOrDefault(version.Id, []))).ToArray());
    }

    private static async Task<IResult> CreateCourseDefinition([FromBody] CreateCourseRequest request, CourseService service, CancellationToken cancellationToken = default)
    {
        Result<Course, CourseErrors> result = await service.CreateCourseAsync(request, cancellationToken);
        return result.Match<IResult>(course => Results.Created($"/api/learning/courses/{course.Id}", course.ToResponse()), _ => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> CreateCourseLanguage(Guid id, [FromBody] CreateCourseLanguageRequest request, CourseService service, CancellationToken cancellationToken = default)
    {
        Result<CourseLanguage, CourseErrors> result = await service.CreateCourseLanguageAsync(id, request, cancellationToken);
        return result.Match<IResult>(language => Results.Created($"/api/learning/courses/{id}/languages/{language.Id}", language.ToResponse()), _ => result.Map(item => item.ToResponse()).AsResponse(MapError));
    }

    private static async Task<IResult> UpdateCourseLanguage(Guid id, Guid languageId, [FromBody] UpdateCourseLanguageRequest request, CourseService service, CancellationToken cancellationToken = default)
    {
        Result<CourseLanguage, CourseErrors> result = await service.UpdateCourseLanguageAsync(id, languageId, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ListCourseVersions(Guid id, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
            return Results.NotFound();

        CourseVersion[] versions = await db.CourseVersions.AsNoTracking().Where(item => item.CourseId == id).OrderByDescending(item => item.VersionNumber).ToArrayAsync(cancellationToken);
        Guid[] versionIds = versions.Select(item => item.Id).ToArray();
        Dictionary<Guid, CourseSco[]> scosByVersionId = await db.CourseScos.AsNoTracking().Where(item => versionIds.Contains(item.CourseVersionId)).OrderBy(item => item.ManifestOrder).GroupBy(item => item.CourseVersionId).ToDictionaryAsync(group => group.Key, group => group.ToArray(), cancellationToken);
        return Results.Ok(versions.Select(version => version.ToResponse(scosByVersionId.GetValueOrDefault(version.Id, []))).ToArray());
    }

    private static async Task<IResult> GetCourseReporting(Guid id, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken))
            return Results.NotFound();

        Attempt[] attempts = await db.Attempts
            .AsNoTracking()
            .Where(item => item.CourseId == id && item.Status == AttemptStatus.Completed && item.CompletedAt.HasValue)
            .OrderByDescending(item => item.CompletedAt)
            .ToArrayAsync(cancellationToken);
        Guid[] versionIds = attempts.Select(item => item.CourseVersionId).Distinct().ToArray();
        Dictionary<Guid, int> versionNumbers = await db.CourseVersions
            .AsNoTracking()
            .Where(item => versionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.VersionNumber, cancellationToken);

        return Results.Ok(attempts.Select(item => new CourseCompletionReportRowResponse(
            item.IdentityId,
            item.EnrollmentId,
            item.Id,
            item.CourseVersionId,
            versionNumbers.GetValueOrDefault(item.CourseVersionId, 0),
            item.CompletedAt!.Value,
            item.CompletionStatus,
            item.SuccessStatus,
            item.Score,
            item.ScoreScaled)).ToArray());
    }

    private static async Task<IResult> UploadCoursePackage([FromForm] string code, [FromForm] string? title, [FromForm] string? description, [FromForm] IFormFile file, CourseService service, CancellationToken cancellationToken = default)
    {
        Result<CourseVersion, CourseErrors> result = await service.CreateCourseAsync(new CreateCourseUploadRequest { Code = code, Title = title, Description = description, File = file }, cancellationToken);
        return result.Match<IResult>(version => Results.Created($"/api/learning/courses/{version.CourseId}/versions/{version.Id}", version.ToResponse([])), _ => result.Map(version => version.ToResponse([])).AsResponse(MapError));
    }

    private static async Task<IResult> CreateCourseVersion(Guid id, [FromForm] string? title, [FromForm] IFormFile file, CourseService service, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        Result<CourseVersion, CourseErrors> result = await service.CreateCourseVersionAsync(id, new CreateCourseVersionUploadRequest { Title = title, File = file }, cancellationToken);
        if (result.IsFailure(out CourseErrors error))
            return result.Map(version => version.ToResponse([])).AsResponse(MapError);

        result.IsSuccess(out CourseVersion version);
        CourseSco[] scos = await db.CourseScos.AsNoTracking().Where(item => item.CourseVersionId == version.Id).OrderBy(item => item.ManifestOrder).ToArrayAsync(cancellationToken);
        return Results.Created($"/api/learning/courses/{id}/versions/{version.Id}", version.ToResponse(scos));
    }

    private static async Task<IResult> CreateCourseLanguageVersion(Guid id, Guid languageId, [FromForm] string? title, [FromForm] IFormFile file, CourseService service, LearningDbContext db, CancellationToken cancellationToken = default)
    {
        Result<CourseVersion, CourseErrors> result = await service.CreateCourseLanguageVersionAsync(id, languageId, new CreateCourseVersionUploadRequest { Title = title, File = file }, cancellationToken);
        if (result.IsFailure(out CourseErrors error))
            return result.Map(version => version.ToResponse([])).AsResponse(MapError);

        result.IsSuccess(out CourseVersion version);
        CourseSco[] scos = await db.CourseScos.AsNoTracking().Where(item => item.CourseVersionId == version.Id).OrderBy(item => item.ManifestOrder).ToArrayAsync(cancellationToken);
        return Results.Created($"/api/learning/courses/{id}/languages/{languageId}/versions/{version.Id}", version.ToResponse(scos));
    }

    private static async Task<IResult> UpdateCourse(Guid id, [FromBody] UpdateCourseRequest request, CourseService service, CancellationToken cancellationToken = default)
    {
        Result<Course, CourseErrors> result = await service.UpdateCourseAsync(id, request, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> ActivateCourse(Guid id, CourseService service, CancellationToken cancellationToken = default)
    {
        Result<Course, CourseErrors> result = await service.SetCourseActiveAsync(id, true, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static async Task<IResult> DeactivateCourse(Guid id, CourseService service, CancellationToken cancellationToken = default)
    {
        Result<Course, CourseErrors> result = await service.SetCourseActiveAsync(id, false, cancellationToken);
        return result.Map(item => item.ToResponse()).AsResponse(MapError);
    }

    private static (int statusCode, ProblemDetails? problemDetails) MapError(CourseErrors error) => error switch
    {
        CourseErrors.CourseNotFound => Problem(StatusCodes.Status404NotFound, "Course not found."),
        CourseErrors.CourseCodeRequired => Problem(StatusCodes.Status400BadRequest, "Course code is required."),
        CourseErrors.CourseTitleRequired => Problem(StatusCodes.Status400BadRequest, "Course title is required."),
        CourseErrors.CourseCodeAlreadyExists => Problem(StatusCodes.Status409Conflict, "Course code already exists."),
        CourseErrors.CourseLanguageNotFound => Problem(StatusCodes.Status404NotFound, "Course language not found."),
        CourseErrors.CourseLanguageCodeRequired => Problem(StatusCodes.Status400BadRequest, "Course language code is required."),
        CourseErrors.CourseLanguageDisplayLabelRequired => Problem(StatusCodes.Status400BadRequest, "Course language display label is required."),
        CourseErrors.CourseLanguageAlreadyExists => Problem(StatusCodes.Status409Conflict, "Course language already exists for this course."),
        CourseErrors.CourseAlreadyActive => Problem(StatusCodes.Status409Conflict, "Course is already active."),
        CourseErrors.CourseAlreadyInactive => Problem(StatusCodes.Status409Conflict, "Course is already inactive."),
        CourseErrors.ManifestNotFound => Problem(StatusCodes.Status400BadRequest, "SCORM manifest `imsmanifest.xml` was not found."),
        CourseErrors.NoLaunchableScoFound => Problem(StatusCodes.Status400BadRequest, "SCORM package did not contain a launchable SCO."),
        CourseErrors.PackageStorageFailed => Problem(StatusCodes.Status500InternalServerError, "Could not store SCORM package."),
        _ => Problem(StatusCodes.Status400BadRequest, "Course request is invalid.")
    };

    private static (int statusCode, ProblemDetails problemDetails) Problem(int statusCode, string detail) =>
        (statusCode, new ProblemDetails { Status = statusCode, Detail = detail });
}
