namespace Fabric.Server.Learning.Domain;

public sealed class CourseVersion
{
    private CourseVersion() { }

    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid CourseLanguageId { get; private set; }
    public int VersionNumber { get; private set; }
    public string Title { get; private set; } = null!;
    public ScormVersion ScormVersion { get; private set; }
    public bool EmitsScore { get; private set; }
    public string StoragePath { get; private set; } = null!;
    public string? ManifestChecksum { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static CourseVersion Create(Guid courseId, Guid courseLanguageId, int versionNumber, string title, ScormVersion scormVersion, bool emitsScore, string storagePath, string? manifestChecksum, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            CourseLanguageId = courseLanguageId,
            VersionNumber = versionNumber,
            Title = title.Trim(),
            ScormVersion = scormVersion,
            EmitsScore = emitsScore,
            StoragePath = storagePath,
            ManifestChecksum = string.IsNullOrWhiteSpace(manifestChecksum) ? null : manifestChecksum.Trim(),
            PublishedAt = now,
            CreatedAt = now,
        };

    public void SetStorageDetails(string storagePath, string? manifestChecksum)
    {
        StoragePath = storagePath;
        ManifestChecksum = string.IsNullOrWhiteSpace(manifestChecksum) ? null : manifestChecksum.Trim();
    }
}
