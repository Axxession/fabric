using System.Text.Json;
using Fabric.Server.Printing.Domain;

namespace Fabric.Server.Printing.Application;

public sealed class PrintDesignParser
{
    public bool TryParse(string designJson, out PrintTemplate? template, out string? error)
    {
        template = null;
        error = null;

        if (string.IsNullOrWhiteSpace(designJson))
        {
            error = "Design JSON is required.";
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(designJson);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("media", out JsonElement mediaElement) || mediaElement.ValueKind != JsonValueKind.Object)
            {
                error = "Design JSON must contain a media object.";
                return false;
            }

            RenderMedia media = ParseMedia(mediaElement);
            int version = TryGetInt(root, "version") ?? 0;
            int dpi = TryGetInt(root, "dpi") ?? 0;
            List<TemplateObject> objects = [];

            if (root.TryGetProperty("objects", out JsonElement objectsElement) && objectsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement objectElement in objectsElement.EnumerateArray())
                    objects.Add(ParseObject(objectElement));
            }

            template = new PrintTemplate
            {
                Version = version,
                Media = media,
                Dpi = dpi,
                Objects = objects
            };

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Design JSON is invalid: {ex.Message}";
            return false;
        }
    }

    private static RenderMedia ParseMedia(JsonElement mediaElement)
    {
        string label = TryGetString(mediaElement, "label") ?? string.Empty;
        double width = TryGetDouble(mediaElement, "width") ?? 0;
        double height = TryGetDouble(mediaElement, "height") ?? 0;
        string orientationRaw = TryGetString(mediaElement, "orientation") ?? nameof(Orientation.Landscape);

        if (!Enum.TryParse(orientationRaw, ignoreCase: true, out Orientation orientation))
            orientation = Orientation.Landscape;

        return new RenderMedia(width, height, orientation, label);
    }

    private static TemplateObject ParseObject(JsonElement objectElement)
    {
        string? fieldType = TryGetString(objectElement, "fieldType");
        string? src = TryGetString(objectElement, "src");
        string? dataField = TryGetString(objectElement, "dataField");

        return new TemplateObject
        {
            Type = TryGetString(objectElement, "type"),
            Src = src,
            Text = TryGetString(objectElement, "text"),
            Field = TryGetString(objectElement, "field"),
            Placeholder = string.Equals(fieldType, "image-placeholder", StringComparison.OrdinalIgnoreCase),
            Fill = TryGetString(objectElement, "fill"),
            Left = TryGetFloat(objectElement, "left") ?? 0,
            Top = TryGetFloat(objectElement, "top") ?? 0,
            Width = TryGetFloat(objectElement, "width") ?? 0,
            Height = TryGetFloat(objectElement, "height") ?? 0,
            ScaleX = TryGetFloat(objectElement, "scaleX") ?? 1,
            ScaleY = TryGetFloat(objectElement, "scaleY") ?? 1,
            FontSize = TryGetFloat(objectElement, "fontSize") ?? 12,
            FontStyle = TryGetString(objectElement, "fontStyle"),
            FontWeight = TryGetString(objectElement, "fontWeight"),
            Underline = TryGetBool(objectElement, "underline") ?? false,
            FontFamily = TryGetString(objectElement, "fontFamily"),
            Angle = TryGetFloat(objectElement, "angle") ?? 0,
            FixedImageSrc = string.Equals(fieldType, "image-fixed", StringComparison.OrdinalIgnoreCase) ? src : null,
            DataField = dataField,
            ResolvedSrc = TryGetString(objectElement, "resolvedSrc")
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool? TryGetBool(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property) && (property.ValueKind is JsonValueKind.True or JsonValueKind.False)
            ? property.GetBoolean()
            : null;

    private static int? TryGetInt(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int value)
            ? value
            : null;

    private static double? TryGetDouble(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out double value)
            ? value
            : null;

    private static float? TryGetFloat(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property))
            return null;

        if (property.ValueKind != JsonValueKind.Number)
            return null;

        if (property.TryGetSingle(out float value))
            return value;

        return property.TryGetDouble(out double doubleValue)
            ? (float)doubleValue
            : null;
    }
}
