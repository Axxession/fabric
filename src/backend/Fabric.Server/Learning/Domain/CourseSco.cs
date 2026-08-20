namespace Fabric.Server.Learning.Domain;

public sealed class CourseSco
{
    private CourseSco() { }

    public Guid Id { get; private set; }
    public Guid CourseVersionId { get; private set; }
    public string ScoIdentifier { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string LaunchUrl { get; private set; } = null!;
    public string ResourcePath { get; private set; } = null!;
    public int ManifestOrder { get; private set; }
    public decimal? MasteryScore { get; private set; }

    public static CourseSco Create(Guid courseVersionId, string scoIdentifier, string title, string launchUrl, string resourcePath, int manifestOrder, decimal? masteryScore) =>
        new()
        {
            Id = Guid.NewGuid(),
            CourseVersionId = courseVersionId,
            ScoIdentifier = scoIdentifier,
            Title = title,
            LaunchUrl = launchUrl,
            ResourcePath = resourcePath,
            ManifestOrder = manifestOrder,
            MasteryScore = masteryScore,
        };
}
