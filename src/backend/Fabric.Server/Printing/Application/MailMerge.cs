using System.Text.RegularExpressions;

namespace Fabric.Server.Printing.Application;

internal sealed class MailMerge
{
    private static readonly Regex TokenPattern = new("\\{\\{(.*?)\\}\\}", RegexOptions.Compiled);

    public PrintTemplate Merge(IReadOnlyDictionary<string, string> data, PrintTemplate template)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(template);

        return template with
        {
            Media = Merge(template.Media, data),
            Objects = [.. template.Objects.Select(item => Merge(item, data))]
        };
    }

    private static RenderMedia Merge(RenderMedia media, IReadOnlyDictionary<string, string> data) => media with
    {
        Label = MergeValue(media.Label, data) ?? string.Empty
    };

    private static TemplateObject Merge(TemplateObject templateObject, IReadOnlyDictionary<string, string> data) => templateObject with
    {
        Type = MergeValue(templateObject.Type, data),
        Src = MergeValue(templateObject.Src, data),
        Text = MergeValue(templateObject.Text, data),
        Field = MergeValue(templateObject.Field, data),
        Fill = MergeValue(templateObject.Fill, data),
        FontStyle = MergeValue(templateObject.FontStyle, data),
        FontWeight = MergeValue(templateObject.FontWeight, data),
        FontFamily = MergeValue(templateObject.FontFamily, data),
        FixedImageSrc = MergeValue(templateObject.FixedImageSrc, data),
        DataField = MergeValue(templateObject.DataField, data),
        ResolvedSrc = MergeValue(templateObject.ResolvedSrc, data)
    };

    private static string? MergeValue(string? value, IReadOnlyDictionary<string, string> data)
    {
        if (value is null)
            return null;

        return TokenPattern.Replace(value, match =>
        {
            string key = match.Groups[1].Value.Trim();
            return data.TryGetValue(key, out string? replacement)
                ? replacement
                : match.Value;
        });
    }
}
