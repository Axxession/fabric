using System.IO.Compression;
using Fabric.Server.Core;
using Fabric.Server.Learning.Contracts;
using Fabric.Server.Learning.Domain;
using Fabric.Server.Learning.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Server.Learning.Application;

public sealed class CourseService(
    LearningDbContext db,
    LearningManifestParser manifestParser,
    ILearningPackageStorage packageStorage,
    TimeProvider timeProvider,
    ILogger<CourseService> logger)
{
    public async Task<Result<Course, CourseErrors>> CreateCourseAsync(CreateCourseRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.Courses.AnyAsync(item => item.Code == request.Code, cancellationToken))
            return Result.Failure<Course, CourseErrors>(CourseErrors.CourseCodeAlreadyExists);

        Result<Course, CourseErrors> create = Course.Create(request.Code, request.Title, request.Description, timeProvider.GetUtcNow());
        if (create.IsFailure(out CourseErrors error))
            return Result.Failure<Course, CourseErrors>(error);

        create.IsSuccess(out Course course);
        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Course, CourseErrors>(course);
    }

    public async Task<Result<CourseVersion, CourseErrors>> CreateCourseAsync(CreateCourseUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.Courses.AnyAsync(item => item.Code == request.Code, cancellationToken))
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.CourseCodeAlreadyExists);

        DateTimeOffset now = timeProvider.GetUtcNow();
        string title = string.IsNullOrWhiteSpace(request.Title) ? request.Code : request.Title;
        Result<Course, CourseErrors> create = Course.Create(request.Code, title, request.Description, now);
        if (create.IsFailure(out CourseErrors error))
            return Result.Failure<CourseVersion, CourseErrors>(error);

        create.IsSuccess(out Course course);
        db.Courses.Add(course);
        Result<CourseLanguage, CourseErrors> languageCreate = CourseLanguage.Create(course.Id, "default", "Default", now);
        if (languageCreate.IsFailure(out error))
            return Result.Failure<CourseVersion, CourseErrors>(error);

        languageCreate.IsSuccess(out CourseLanguage language);
        db.CourseLanguages.Add(language);
        Result<CourseVersion, CourseErrors> versionResult = await CreateVersionCoreAsync(course, language, request.File, request.Title, cancellationToken);
        if (versionResult.IsFailure(out error))
            return Result.Failure<CourseVersion, CourseErrors>(error);

        versionResult.IsSuccess(out CourseVersion version);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CourseVersion, CourseErrors>(version);
    }

    public async Task<Result<CourseLanguage, CourseErrors>> CreateCourseLanguageAsync(Guid courseId, CreateCourseLanguageRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.Courses.AnyAsync(item => item.Id == courseId, cancellationToken))
            return Result.Failure<CourseLanguage, CourseErrors>(CourseErrors.CourseNotFound);

        bool exists = await db.CourseLanguages.AnyAsync(item => item.CourseId == courseId && item.LanguageCode == request.LanguageCode, cancellationToken);
        if (exists)
            return Result.Failure<CourseLanguage, CourseErrors>(CourseErrors.CourseLanguageAlreadyExists);

        Result<CourseLanguage, CourseErrors> create = CourseLanguage.Create(courseId, request.LanguageCode, request.DisplayLabel, timeProvider.GetUtcNow());
        if (create.IsFailure(out CourseErrors error))
            return Result.Failure<CourseLanguage, CourseErrors>(error);

        create.IsSuccess(out CourseLanguage language);
        db.CourseLanguages.Add(language);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CourseLanguage, CourseErrors>(language);
    }

    public async Task<Result<CourseLanguage, CourseErrors>> UpdateCourseLanguageAsync(Guid courseId, Guid languageId, UpdateCourseLanguageRequest request, CancellationToken cancellationToken = default)
    {
        CourseLanguage? language = await db.CourseLanguages.SingleOrDefaultAsync(item => item.Id == languageId && item.CourseId == courseId, cancellationToken);
        if (language is null)
            return Result.Failure<CourseLanguage, CourseErrors>(CourseErrors.CourseLanguageNotFound);

        bool duplicate = await db.CourseLanguages.AnyAsync(item => item.Id != languageId && item.CourseId == courseId && item.LanguageCode == request.LanguageCode, cancellationToken);
        if (duplicate)
            return Result.Failure<CourseLanguage, CourseErrors>(CourseErrors.CourseLanguageAlreadyExists);

        Result<CourseErrors> update = language.Update(request.LanguageCode, request.DisplayLabel, request.IsActive, timeProvider.GetUtcNow());
        if (update.IsFailure(out CourseErrors error))
            return Result.Failure<CourseLanguage, CourseErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<CourseLanguage, CourseErrors>(language);
    }

    public async Task<Result<CourseVersion, CourseErrors>> CreateCourseVersionAsync(Guid courseId, CreateCourseVersionUploadRequest request, CancellationToken cancellationToken = default)
    {
        Course? course = await db.Courses.SingleOrDefaultAsync(item => item.Id == courseId, cancellationToken);
        if (course is null)
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.CourseNotFound);

        CourseLanguage? language = await db.CourseLanguages.OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(item => item.CourseId == courseId, cancellationToken);
        if (language is null)
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.CourseLanguageNotFound);

        return await CreateVersionCoreAsync(course, language, request.File, request.Title, cancellationToken);
    }

    public async Task<Result<CourseVersion, CourseErrors>> CreateCourseLanguageVersionAsync(Guid courseId, Guid languageId, CreateCourseVersionUploadRequest request, CancellationToken cancellationToken = default)
    {
        Course? course = await db.Courses.SingleOrDefaultAsync(item => item.Id == courseId, cancellationToken);
        if (course is null)
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.CourseNotFound);

        CourseLanguage? language = await db.CourseLanguages.SingleOrDefaultAsync(item => item.Id == languageId && item.CourseId == courseId, cancellationToken);
        if (language is null)
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.CourseLanguageNotFound);

        return await CreateVersionCoreAsync(course, language, request.File, request.Title, cancellationToken);
    }

    public async Task<Result<Course, CourseErrors>> SetCourseActiveAsync(Guid courseId, bool isActive, CancellationToken cancellationToken = default)
    {
        Course? course = await db.Courses.SingleOrDefaultAsync(item => item.Id == courseId, cancellationToken);
        if (course is null)
            return Result.Failure<Course, CourseErrors>(CourseErrors.CourseNotFound);

        Result<CourseErrors> result = isActive ? course.Activate(timeProvider.GetUtcNow()) : course.Deactivate(timeProvider.GetUtcNow());
        if (result.IsFailure(out CourseErrors error))
            return Result.Failure<Course, CourseErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Course, CourseErrors>(course);
    }

    public async Task<Result<Course, CourseErrors>> UpdateCourseAsync(Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken = default)
    {
        Course? course = await db.Courses.SingleOrDefaultAsync(item => item.Id == courseId, cancellationToken);
        if (course is null)
            return Result.Failure<Course, CourseErrors>(CourseErrors.CourseNotFound);

        Result<CourseErrors> update = course.UpdateMetadata(request.Title, request.Description, timeProvider.GetUtcNow());
        if (update.IsFailure(out CourseErrors error))
            return Result.Failure<Course, CourseErrors>(error);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<Course, CourseErrors>(course);
    }

    private async Task<Result<CourseVersion, CourseErrors>> CreateVersionCoreAsync(Course course, CourseLanguage language, IFormFile file, string? requestedTitle, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.InvalidPackage);

        string tempDirectory = Path.Combine(Path.GetTempPath(), "fabric-learning", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            await using (Stream stream = file.OpenReadStream())
            using (ZipArchive archive = new(stream, ZipArchiveMode.Read, leaveOpen: false))
            {
                ExtractArchiveSafely(archive, tempDirectory);
            }

            Result<ParsedScormManifest, CourseErrors> manifestResult = manifestParser.Parse(tempDirectory);
            if (manifestResult.IsFailure(out CourseErrors error))
                return Result.Failure<CourseVersion, CourseErrors>(error);

            manifestResult.IsSuccess(out ParsedScormManifest manifest);
            int nextVersion = (await db.CourseVersions
                .Where(item => item.CourseLanguageId == language.Id)
                .Select(item => (int?)item.VersionNumber)
                .MaxAsync(cancellationToken) ?? 0) + 1;
            CourseVersion version = CourseVersion.Create(course.Id, language.Id, nextVersion, string.IsNullOrWhiteSpace(requestedTitle) ? manifest.Title : requestedTitle!, manifest.ScormVersion, manifest.EmitsScore, string.Empty, null, timeProvider.GetUtcNow());

            (string storagePath, string? manifestChecksum) = await packageStorage.SavePackageAsync(course.Id, version.Id, tempDirectory, cancellationToken);
            version.SetStorageDetails(storagePath, manifestChecksum);
            db.CourseVersions.Add(version);
            db.CourseScos.AddRange(manifest.Scos.Select(item => CourseSco.Create(version.Id, item.ScoIdentifier, item.Title, item.LaunchUrl, item.ResourcePath, item.ManifestOrder, item.MasteryScore)));

            string courseTitle = string.IsNullOrWhiteSpace(requestedTitle) ? manifest.Title : requestedTitle!;
            Result<CourseErrors> metadata = course.UpdateMetadata(courseTitle, course.Description, timeProvider.GetUtcNow());
            if (metadata.IsFailure(out error))
                return Result.Failure<CourseVersion, CourseErrors>(error);

            course.SetCurrentVersion(version.Id, timeProvider.GetUtcNow());
            language.SetCurrentVersion(version.Id, timeProvider.GetUtcNow());
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success<CourseVersion, CourseErrors>(version);
        }
        catch (InvalidDataException)
        {
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.InvalidPackage);
        }
        catch (InvalidOperationException exception)
        {
            CourseServiceLog.CoursePackageStorageFailed(logger, exception, course.Id);
            return Result.Failure<CourseVersion, CourseErrors>(CourseErrors.PackageStorageFailed);
        }
        catch (Exception exception)
        {
            CourseServiceLog.CourseVersionCreationFailed(logger, exception, course.Id);
            throw;
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void ExtractArchiveSafely(ZipArchive archive, string destinationDirectory)
    {
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string destinationRoot = Path.GetFullPath(destinationDirectory + Path.DirectorySeparatorChar);
            string destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName));
            if (!destinationPath.StartsWith(destinationRoot, StringComparison.Ordinal))
                throw new InvalidDataException("Archive entry attempted path traversal.");

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(destinationPath);
                continue;
            }

            string? directory = Path.GetDirectoryName(destinationPath);
            if (directory is not null)
                Directory.CreateDirectory(directory);

            entry.ExtractToFile(destinationPath, overwrite: true);
        }
    }
}

internal static partial class CourseServiceLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to store SCORM package for course {CourseId}")]
    public static partial void CoursePackageStorageFailed(ILogger logger, Exception exception, Guid courseId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected failure while creating SCORM course version for course {CourseId}")]
    public static partial void CourseVersionCreationFailed(ILogger logger, Exception exception, Guid courseId);
}
