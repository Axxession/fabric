using Fabric.Server.Printing.Application;
using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Tests.Printing.Application;

public sealed class MailMergeTests
{
    private readonly MailMerge _mailMerge = new();

    [Fact(DisplayName = "Merge_WhenKnownTokensExist_ReplacesCurrentKnownStringValues")]
    public void Merge_WhenKnownTokensExist_ReplacesCurrentKnownStringValues()
    {
        PrintTemplate template = new()
        {
            Version = 2,
            Dpi = 300,
            Media = new RenderMedia(85.6, 54, Orientation.Landscape, "{{ BadgeType }} for {{ Name }}"),
            Objects =
            [
                new TemplateObject
                {
                    Type = "{{ ObjectType }}",
                    Src = "/images/{{ PhotoFile }}",
                    Text = "Hi, {{ Name }}",
                    Field = "{{ FieldName }}",
                    Fill = "{{ BrandColor }}",
                    FontStyle = "{{ FontStyle }}",
                    FontWeight = "{{ FontWeight }}",
                    FontFamily = "{{ FontFamily }}",
                    FixedImageSrc = "/fixed/{{ FixedFile }}",
                    DataField = "{{ DataPoint }}",
                    ResolvedSrc = "/resolved/{{ ResolvedFile }}"
                }
            ]
        };

        Dictionary<string, string> data = new()
        {
            ["BadgeType"] = "Visitor badge",
            ["Name"] = "Sverre",
            ["ObjectType"] = "textbox",
            ["PhotoFile"] = "sverre.png",
            ["FieldName"] = "displayName",
            ["BrandColor"] = "#112233",
            ["FontStyle"] = "italic",
            ["FontWeight"] = "700",
            ["FontFamily"] = "Inter",
            ["FixedFile"] = "logo.png",
            ["DataPoint"] = "employeeNumber",
            ["ResolvedFile"] = "merged.png"
        };

        PrintTemplate merged = _mailMerge.Merge(data, template);

        Assert.Equal("Visitor badge for Sverre", merged.Media.Label);

        TemplateObject mergedObject = Assert.Single(merged.Objects);
        Assert.Equal("textbox", mergedObject.Type);
        Assert.Equal("/images/sverre.png", mergedObject.Src);
        Assert.Equal("Hi, Sverre", mergedObject.Text);
        Assert.Equal("displayName", mergedObject.Field);
        Assert.Equal("#112233", mergedObject.Fill);
        Assert.Equal("italic", mergedObject.FontStyle);
        Assert.Equal("700", mergedObject.FontWeight);
        Assert.Equal("Inter", mergedObject.FontFamily);
        Assert.Equal("/fixed/logo.png", mergedObject.FixedImageSrc);
        Assert.Equal("employeeNumber", mergedObject.DataField);
        Assert.Equal("/resolved/merged.png", mergedObject.ResolvedSrc);
    }

    [Fact(DisplayName = "Merge_WhenTokenWhitespaceOrMissingKey_UsesTrimmedKeyAndLeavesUnknownToken")]
    public void Merge_WhenTokenWhitespaceOrMissingKey_UsesTrimmedKeyAndLeavesUnknownToken()
    {
        PrintTemplate template = new()
        {
            Media = new RenderMedia(85.6, 54, Orientation.Landscape, "{{ Name }}"),
            Objects =
            [
                new TemplateObject
                {
                    Text = "Hi, {{ Name }} {{ Missing }}"
                }
            ]
        };

        Dictionary<string, string> data = new()
        {
            ["Name"] = "Sverre"
        };

        PrintTemplate merged = _mailMerge.Merge(data, template);

        Assert.Equal("Sverre", merged.Media.Label);
        Assert.Equal("Hi, Sverre {{ Missing }}", Assert.Single(merged.Objects).Text);
    }

    [Fact(DisplayName = "Merge_DoesNotMutateSourceTemplate")]
    public void Merge_DoesNotMutateSourceTemplate()
    {
        PrintTemplate template = new()
        {
            Media = new RenderMedia(85.6, 54, Orientation.Landscape, "{{ Name }}"),
            Objects =
            [
                new TemplateObject
                {
                    Text = "Hi, {{ Name }}",
                    Fill = "{{ BrandColor }}"
                }
            ]
        };

        Dictionary<string, string> data = new()
        {
            ["Name"] = "Sverre",
            ["BrandColor"] = "#112233"
        };

        PrintTemplate merged = _mailMerge.Merge(data, template);

        Assert.NotSame(template, merged);
        Assert.Equal("{{ Name }}", template.Media.Label);
        Assert.Equal("Hi, {{ Name }}", template.Objects[0].Text);
        Assert.Equal("{{ BrandColor }}", template.Objects[0].Fill);
    }
}
