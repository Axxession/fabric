using Fabric.Server.Core;
using Fabric.Server.Learning.Application;
using Fabric.Server.Learning.Domain;

namespace Fabric.Server.Tests.Learning.Application;

public sealed class LearningManifestParserTests
{
    [Fact]
    public void Parse_WhenDefaultOrganizationExists_UsesItsStructureAndTitle()
    {
        string manifest = """
            <manifest xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
              <metadata>
                <schemaversion>1.2</schemaversion>
              </metadata>
              <organizations default="ORG-2">
                <organization identifier="ORG-1">
                  <title>Wrong org</title>
                  <item identifier="ITEM-1" identifierref="RES-1"><title>Wrong intro</title></item>
                </organization>
                <organization identifier="ORG-2">
                  <title>Right org</title>
                  <item identifier="ITEM-2" identifierref="RES-2"><title>Right intro</title></item>
                </organization>
              </organizations>
              <resources>
                <resource identifier="RES-1" href="wrong.html" adlcp:scormtype="sco" />
                <resource identifier="RES-2" href="right.html" adlcp:scormtype="sco" />
              </resources>
            </manifest>
            """;

        ParsedScormManifest parsed = ParseManifest(manifest);

        Assert.Equal("Right org", parsed.Title);
        ParsedSco sco = Assert.Single(parsed.Scos);
        Assert.Equal("ITEM-2", sco.ScoIdentifier);
        Assert.Equal("RES-2", sco.ResourceId);
        Assert.Equal("right.html", sco.LaunchUrl);
    }

    [Fact]
    public void Parse_WhenNestedItemsExist_PreservesDepthFirstOrder()
    {
        string manifest = """
            <manifest xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_v1p3">
              <metadata>
                <schemaversion>2004 4th Edition</schemaversion>
              </metadata>
              <organizations default="ORG-1">
                <organization identifier="ORG-1">
                  <title>Safety Training</title>
                  <item identifier="CH-1">
                    <title>Chapter 1</title>
                    <item identifier="ITEM-1" identifierref="RES-1"><title>Intro</title></item>
                    <item identifier="ITEM-2" identifierref="RES-2"><title>Quiz</title></item>
                  </item>
                  <item identifier="ITEM-3" identifierref="RES-3"><title>Wrap up</title></item>
                </organization>
              </organizations>
              <resources>
                <resource identifier="RES-1" href="intro/index.html" adlcp:scormtype="sco" />
                <resource identifier="RES-2" href="quiz/index.html" adlcp:scormtype="sco" />
                <resource identifier="RES-3" href="wrapup/index.html" adlcp:scormtype="sco" />
              </resources>
            </manifest>
            """;

        ParsedScormManifest parsed = ParseManifest(manifest);

        Assert.Equal(ScormVersion.Scorm2004, parsed.ScormVersion);
        Assert.Equal(["ITEM-1", "ITEM-2", "ITEM-3"], parsed.Scos.Select(item => item.ScoIdentifier).ToArray());
        Assert.Equal([0, 1, 2], parsed.Scos.Select(item => item.ManifestOrder).ToArray());
    }

    [Fact]
    public void Parse_WhenItemTitleMissing_FallsBackToItemIdentifier()
    {
        string manifest = """
            <manifest xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
              <metadata><schemaversion>1.2</schemaversion></metadata>
              <organizations default="ORG-1">
                <organization identifier="ORG-1">
                  <title>Fallback Course</title>
                  <item identifier="ITEM-1" identifierref="RES-1" />
                </organization>
              </organizations>
              <resources>
                <resource identifier="RES-1" href="index.html" adlcp:scormtype="sco" />
              </resources>
            </manifest>
            """;

        ParsedScormManifest parsed = ParseManifest(manifest);

        ParsedSco sco = Assert.Single(parsed.Scos);
        Assert.Equal("ITEM-1", sco.Title);
    }

    [Fact]
    public void Parse_WhenNoScoResourcesExist_ReturnsFailure()
    {
        string manifest = """
            <manifest xmlns:adlcp="http://www.adlnet.org/xsd/adlcp_rootv1p2">
              <metadata><schemaversion>1.2</schemaversion></metadata>
              <organizations default="ORG-1">
                <organization identifier="ORG-1">
                  <title>Assets only</title>
                  <item identifier="ITEM-1" identifierref="RES-1"><title>Asset</title></item>
                </organization>
              </organizations>
              <resources>
                <resource identifier="RES-1" href="asset.png" adlcp:scormtype="asset" />
              </resources>
            </manifest>
            """;

        LearningManifestParser parser = new();
        string directory = CreateManifestDirectory(manifest);

        try
        {
            Result<ParsedScormManifest, CourseErrors> result = parser.Parse(directory);
            Assert.True(result.IsFailure(out CourseErrors error));
            Assert.Equal(CourseErrors.NoLaunchableScoFound, error);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ParsedScormManifest ParseManifest(string manifest)
    {
        LearningManifestParser parser = new();
        string directory = CreateManifestDirectory(manifest);

        try
        {
            Result<ParsedScormManifest, CourseErrors> result = parser.Parse(directory);
            Assert.True(result.IsSuccess(out ParsedScormManifest parsed));
            return parsed;
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateManifestDirectory(string manifest)
    {
        string directory = Path.Combine(Path.GetTempPath(), "fabric-learning-parser-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "imsmanifest.xml"), manifest);
        return directory;
    }
}
