using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Tests.Printing.Application;

public sealed class PrintDesignParserTests
{
    private readonly PrintDesignParser _parser = new();

    [Fact(DisplayName = "Parser allows objects with missing numeric fields")]
    public void TryParse_AllowsMissingNumericFields()
    {
        const string designJson = """
        {
          "version": 2,
          "media": { "label": "CR80", "width": 85.6, "height": 54, "orientation": "Landscape" },
          "dpi": 300,
          "objects": [
            { "type": "textbox", "text": "Hello", "fieldType": "text", "left": 100, "top": 100, "fill": "#000000" },
            { "type": "rect", "fieldType": "image-placeholder", "dataField": "photo" }
          ]
        }
        """;

        bool success = _parser.TryParse(designJson, out PrintTemplate? template, out string? error);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(template);
        Assert.Equal(2, template.Version);
        Assert.Equal(300, template.Dpi);
        Assert.Equal("CR80", template.Media.Label);
        Assert.Equal(2, template.Objects.Count);
        Assert.Equal(0, template.Objects[1].Width);
        Assert.Equal(12, template.Objects[0].FontSize);
        Assert.True(template.Objects[1].Placeholder);
    }

    [Fact(DisplayName = "Parser ignores non-numeric optional numeric fields")]
    public void TryParse_IgnoresNonNumericOptionalFields()
    {
        const string designJson = """
        {
          "version": 2,
          "media": { "label": "CR80", "width": 85.6, "height": 54, "orientation": "Landscape" },
          "dpi": 300,
          "objects": [
            {
              "type": "textbox",
              "text": "Hello",
              "fieldType": "text",
              "left": "100px",
              "top": 100,
              "fontSize": "large",
              "angle": "none"
            }
          ]
        }
        """;

        bool success = _parser.TryParse(designJson, out PrintTemplate? template, out string? error);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(template);
        TemplateObject textObject = Assert.Single(template.Objects);
        Assert.Equal(0, textObject.Left);
        Assert.Equal(100, textObject.Top);
        Assert.Equal(12, textObject.FontSize);
        Assert.Equal(0, textObject.Angle);
    }

    [Fact(DisplayName = "Parser rejects missing media")]
    public void TryParse_RejectsMissingMedia()
    {
        const string designJson = """
        {
          "version": 2,
          "dpi": 300,
          "objects": []
        }
        """;

        bool success = _parser.TryParse(designJson, out PrintTemplate? template, out string? error);

        Assert.False(success);
        Assert.Null(template);
        Assert.Equal("Design JSON must contain a media object.", error);
    }

    [Fact(DisplayName = "Parser reads media orientation case-insensitively")]
    public void TryParse_ReadsMediaOrientationCaseInsensitively()
    {
        const string designJson = """
        {
          "version": 2,
          "media": { "label": "Visitor Label", "width": 50.0, "height": 75.0, "orientation": "portrait" },
          "dpi": 300,
          "objects": []
        }
        """;

        bool success = _parser.TryParse(designJson, out PrintTemplate? template, out string? error);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(template);
        Assert.Equal(Orientation.Portrait, template.Media.Orientation);
    }
}
