using System.Globalization;
using System.Xml.Linq;
using Fabric.Server.Core;
using Fabric.Server.Learning.Domain;

namespace Fabric.Server.Learning.Application;

public sealed class LearningManifestParser
{
    private static readonly XNamespace Adlcp12 = "http://www.adlnet.org/xsd/adlcp_rootv1p2";
    private static readonly XNamespace Adlcp2004 = "http://www.adlnet.org/xsd/adlcp_v1p3";

    public Result<ParsedScormManifest, CourseErrors> Parse(string extractedPackageDirectory)
    {
        string manifestPath = Path.Combine(extractedPackageDirectory, "imsmanifest.xml");
        if (!File.Exists(manifestPath))
            return Result.Failure<ParsedScormManifest, CourseErrors>(CourseErrors.ManifestNotFound);

        XDocument document;
        try
        {
            document = XDocument.Load(manifestPath, LoadOptions.PreserveWhitespace);
        }
        catch
        {
            return Result.Failure<ParsedScormManifest, CourseErrors>(CourseErrors.InvalidPackage);
        }

        XElement? manifest = document.Root;
        if (manifest is null)
            return Result.Failure<ParsedScormManifest, CourseErrors>(CourseErrors.InvalidPackage);

        string? schemaVersionValue = manifest.Descendants().FirstOrDefault(item => item.Name.LocalName == "schemaversion")?.Value?.Trim();
        ScormVersion scormVersion = schemaVersionValue is not null && schemaVersionValue.StartsWith("2004", StringComparison.OrdinalIgnoreCase)
            ? ScormVersion.Scorm2004
            : ScormVersion.Scorm12;

        Dictionary<string, ResourceNode> resources = BuildScoResourceMap(manifest, scormVersion);
        XElement? organization = FindDefaultOrganization(manifest);
        string title = ExtractCourseTitle(manifest, organization);
        if (organization is null)
            return Result.Failure<ParsedScormManifest, CourseErrors>(CourseErrors.NoLaunchableScoFound);

        List<ParsedSco> scos = [];
        TraverseItemsDepthFirst(organization, resources, title, scos);

        if (scos.Count == 0)
            return Result.Failure<ParsedScormManifest, CourseErrors>(CourseErrors.NoLaunchableScoFound);

        bool emitsScore = scos.Any(item => item.MasteryScore.HasValue);
        return Result.Success<ParsedScormManifest, CourseErrors>(new ParsedScormManifest(title, scormVersion, emitsScore, scos));
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result) ? result : null;

    private static string ExtractCourseTitle(XElement manifest, XElement? organization)
    {
        string? organizationTitle = organization?
            .Elements()
            .FirstOrDefault(item => item.Name.LocalName == "title")?
            .Value?
            .Trim();
        if (!string.IsNullOrWhiteSpace(organizationTitle))
            return organizationTitle;

        string? manifestTitle = manifest
            .Elements()
            .FirstOrDefault(item => item.Name.LocalName == "organizations")?
            .Elements()
            .FirstOrDefault(item => item.Name.LocalName == "organization")?
            .Elements()
            .FirstOrDefault(item => item.Name.LocalName == "title")?
            .Value?
            .Trim();
        if (!string.IsNullOrWhiteSpace(manifestTitle))
            return manifestTitle;

        string? firstTitle = manifest.Descendants().FirstOrDefault(item => item.Name.LocalName == "title" && !string.IsNullOrWhiteSpace(item.Value))?.Value?.Trim();
        return string.IsNullOrWhiteSpace(firstTitle) ? "Untitled course" : firstTitle;
    }

    private static Dictionary<string, ResourceNode> BuildScoResourceMap(XElement manifest, ScormVersion version)
    {
        XName scormTypeName = version == ScormVersion.Scorm12 ? Adlcp12 + "scormtype" : Adlcp2004 + "scormtype";

        return manifest.Descendants()
            .Where(item => item.Name.LocalName == "resource")
            .Select(item => new ResourceNode(
                item.Attribute("identifier")?.Value,
                item.Attribute("href")?.Value,
                item.Attribute(scormTypeName)?.Value ?? item.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "scormtype")?.Value))
            .Where(item => !string.IsNullOrWhiteSpace(item.Identifier))
            .Where(item => string.Equals(item.ScormType, "sco", StringComparison.OrdinalIgnoreCase))
            .Where(item => !string.IsNullOrWhiteSpace(item.Href))
            .ToDictionary(item => item.Identifier!, item => item, StringComparer.OrdinalIgnoreCase);
    }

    private static XElement? FindDefaultOrganization(XElement manifest)
    {
        XElement? organizations = manifest.Descendants().FirstOrDefault(item => item.Name.LocalName == "organizations");
        if (organizations is null)
            return null;

        XElement[] organizationNodes = organizations.Elements().Where(item => item.Name.LocalName == "organization").ToArray();
        if (organizationNodes.Length == 0)
            return null;

        string? defaultIdentifier = organizations.Attribute("default")?.Value;
        if (!string.IsNullOrWhiteSpace(defaultIdentifier))
        {
            XElement? defaultOrganization = organizationNodes.FirstOrDefault(item => string.Equals(item.Attribute("identifier")?.Value, defaultIdentifier, StringComparison.OrdinalIgnoreCase));
            if (defaultOrganization is not null)
                return defaultOrganization;
        }

        return organizationNodes[0];
    }

    private static void TraverseItemsDepthFirst(XElement parent, IReadOnlyDictionary<string, ResourceNode> resourceMap, string courseTitle, ICollection<ParsedSco> result)
    {
        foreach (XElement item in parent.Elements().Where(element => element.Name.LocalName == "item"))
        {
            string? identifierRef = item.Attribute("identifierref")?.Value;
            if (!string.IsNullOrWhiteSpace(identifierRef) && resourceMap.TryGetValue(identifierRef, out ResourceNode? resource))
            {
                string scoId = item.Attribute("identifier")?.Value ?? identifierRef;
                string scoTitle = item.Elements().FirstOrDefault(element => element.Name.LocalName == "title")?.Value?.Trim() ?? scoId;
                if (string.IsNullOrWhiteSpace(scoTitle))
                    scoTitle = courseTitle;

                decimal? masteryScore = ParseDecimal(item.Descendants().FirstOrDefault(element => element.Name.LocalName is "masteryscore" or "masteryScore")?.Value);
                result.Add(new ParsedSco(
                    scoId,
                    scoTitle,
                    resource.Identifier!,
                    NormalizeRelativePath(resource.Href!),
                    NormalizeRelativePath(resource.Href!),
                    result.Count,
                    masteryScore));
            }

            TraverseItemsDepthFirst(item, resourceMap, courseTitle, result);
        }
    }

    private static string NormalizeRelativePath(string path) => path.Replace('\\', '/').TrimStart('/');

    private sealed record ResourceNode(string? Identifier, string? Href, string? ScormType);
}

public sealed record ParsedScormManifest(string Title, ScormVersion ScormVersion, bool EmitsScore, IReadOnlyList<ParsedSco> Scos);

public sealed record ParsedSco(string ScoIdentifier, string Title, string ResourceId, string LaunchUrl, string ResourcePath, int ManifestOrder, decimal? MasteryScore);
