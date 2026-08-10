using System.IO.Compression;
using System.Globalization;
using System.Text.RegularExpressions;
using Fabric.Server.Printing.Domain;
using SkiaSharp;

namespace Fabric.Server.Printing.Application;

internal sealed class ImageRenderService : IRenderService
{
    private static readonly Regex RgbColorPattern = new(@"^rgba?\((.+)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<ImageRenderService> _logger;
    private readonly MailMerge _mailMerge;
    private readonly FontProvider _fontProvider;
    private readonly ImageType _imageType;
    private readonly RenderProfile _renderProfile;

    public ImageRenderService(
        ILogger<ImageRenderService> logger,
        MailMerge mailMerge,
        FontProvider fontProvider,
        ImageType imageType,
        RenderProfile renderProfile)
    {
        _logger = logger;
        _mailMerge = mailMerge;
        _fontProvider = fontProvider;
        _imageType = imageType;
        _renderProfile = renderProfile;
    }

    public Task<RenderedDocument> RenderAsync(
        IReadOnlyDictionary<string, string> data,
        PrintTemplate template,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PrintTemplate mergedTemplate = _mailMerge.Merge(data, template);
        byte[] content = RenderImage(mergedTemplate);
        return Task.FromResult(new RenderedDocument(GetContentType(_imageType), $"rendered.{GetFileExtension(_imageType)}", content));
    }

    public async Task<RenderedDocument> RenderManyAsync(
        IReadOnlyList<IReadOnlyDictionary<string, string>> rows,
        PrintTemplate template,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(template);

        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (int index = 0; index < rows.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[] content = RenderImage(_mailMerge.Merge(rows[index], template));
                ZipArchiveEntry entry = archive.CreateEntry($"render-{index + 1:000}.{GetFileExtension(_imageType)}", CompressionLevel.Fastest);
                using Stream entryStream = entry.Open();
                await entryStream.WriteAsync(content, cancellationToken);
            }
        }

        return new RenderedDocument("application/zip", "rendered-images.zip", stream.ToArray());
    }

    private byte[] RenderImage(PrintTemplate mergedTemplate)
    {
        if (mergedTemplate.Media is null)
            throw new InvalidOperationException("Template media is missing.");

        int dpi = _renderProfile.Dpi > 0 ? _renderProfile.Dpi : mergedTemplate.Dpi;
        if (dpi <= 0)
            throw new InvalidOperationException("Render DPI must be positive.");

        int widthPx = MmToPx(mergedTemplate.Media.Width, dpi);
        int heightPx = MmToPx(mergedTemplate.Media.Height, dpi);
        if (widthPx <= 0 || heightPx <= 0)
            throw new InvalidOperationException("Template media must resolve to positive pixel dimensions.");

        using SKBitmap bitmap = new(new SKImageInfo(widthPx, heightPx, SKColorType.Rgba8888, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(ParseColor(_renderProfile.Background, SKColors.White));

        foreach (TemplateObject templateObject in mergedTemplate.Objects)
        {
            string type = (templateObject.Type ?? string.Empty).ToLowerInvariant();
            switch (type)
            {
                case "textbox":
                case "text":
                    DrawText(canvas, templateObject);
                    break;
                case "rect":
                case "image":
                    DrawImage(canvas, templateObject);
                    break;
            }
        }

        return EncodeBitmap(bitmap, dpi);
    }

    private byte[] EncodeBitmap(SKBitmap bitmap, int dpi) => _imageType switch
    {
        ImageType.Bmp => EncodeBmp(bitmap, dpi),
        ImageType.Jpeg => EncodeSkiaBitmap(bitmap, SKEncodedImageFormat.Jpeg, _renderProfile.Quality ?? 90),
        _ => EncodeSkiaBitmap(bitmap, SKEncodedImageFormat.Png)
    };

    private void DrawText(SKCanvas canvas, TemplateObject templateObject)
    {
        string text = templateObject.Text ?? string.Empty;
        float renderWidth = templateObject.Width * ResolveScale(templateObject.ScaleX);
        float angle = NormalizeAngle(templateObject.Angle);

        using SKFont font = _fontProvider.CreateFont(templateObject.FontFamily, MathF.Max(6, templateObject.FontSize));
        using SKPaint paint = CreateTextPaint(templateObject, font);

        if (angle != 0)
        {
            DrawRotatedText(canvas, templateObject, text, font, paint, angle, renderWidth);
            return;
        }

        DrawUnrotatedText(canvas, templateObject, text, font, paint, renderWidth);
    }

    private SKPaint CreateTextPaint(TemplateObject templateObject, SKFont font) => new()
    {
        Color = ParseColor(templateObject.Fill, SKColors.Black),
        IsAntialias = true,
        Typeface = font.Typeface,
        TextSize = font.Size
    };

    private static void DrawUnrotatedText(SKCanvas canvas, TemplateObject templateObject, string text, SKFont font, SKPaint paint, float renderWidth)
    {
        IReadOnlyList<string> wrappedLines = WrapText(text, paint, renderWidth * 1.03f);
        SKRect firstLineBounds = MeasureBounds(paint, wrappedLines[0]);
        float baseline = templateObject.Top - firstLineBounds.Top;

        foreach (string line in wrappedLines)
        {
            canvas.DrawText(line, templateObject.Left, baseline, font, paint);
            baseline += font.Spacing;
        }
    }

    private static void DrawRotatedText(SKCanvas canvas, TemplateObject templateObject, string text, SKFont font, SKPaint paint, float angle, float renderWidth)
    {
        IReadOnlyList<string> wrappedLines = WrapText(text, paint, renderWidth * 1.05f);
        SKSize measuredSize = MeasureWrappedText(wrappedLines, font, paint);

        const float pad = 4f;
        int tempWidth = Math.Max(1, (int)Math.Ceiling(measuredSize.Width + pad * 2));
        int tempHeight = Math.Max(1, (int)Math.Ceiling(measuredSize.Height + pad * 2));

        using SKBitmap tempBitmap = new(new SKImageInfo(tempWidth, tempHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        using SKCanvas tempCanvas = new(tempBitmap);
        tempCanvas.Clear(SKColors.Transparent);
        DrawWrappedText(tempCanvas, wrappedLines, font, paint, pad, pad);

        float radians = angle * MathF.PI / 180f;
        int rotatedWidth = Math.Max(1, (int)Math.Ceiling(Math.Abs(tempWidth * MathF.Cos(radians)) + Math.Abs(tempHeight * MathF.Sin(radians))));
        int rotatedHeight = Math.Max(1, (int)Math.Ceiling(Math.Abs(tempWidth * MathF.Sin(radians)) + Math.Abs(tempHeight * MathF.Cos(radians))));

        using SKBitmap rotatedBitmap = new(new SKImageInfo(rotatedWidth, rotatedHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        using SKCanvas rotatedCanvas = new(rotatedBitmap);
        rotatedCanvas.Clear(SKColors.Transparent);
        rotatedCanvas.Translate(rotatedWidth / 2f, rotatedHeight / 2f);
        rotatedCanvas.RotateDegrees(180 - angle);
        rotatedCanvas.DrawBitmap(tempBitmap, new SKRect(-tempWidth / 2f, -tempHeight / 2f, tempWidth / 2f, tempHeight / 2f));

        float destinationX = templateObject.Left - templateObject.Height;
        canvas.DrawBitmap(rotatedBitmap, destinationX, templateObject.Top);
    }

    private static void DrawWrappedText(SKCanvas canvas, IReadOnlyList<string> lines, SKFont font, SKPaint paint, float left, float top)
    {
        SKRect firstLineBounds = MeasureBounds(paint, lines[0]);
        float baseline = top - firstLineBounds.Top;

        foreach (string line in lines)
        {
            canvas.DrawText(line, left, baseline, font, paint);
            baseline += font.Spacing;
        }
    }

    private static SKSize MeasureWrappedText(IReadOnlyList<string> lines, SKFont font, SKPaint paint)
    {
        float maxWidth = 0;
        foreach (string line in lines)
            maxWidth = Math.Max(maxWidth, paint.MeasureText(line));

        int lineCount = Math.Max(1, lines.Count);
        float height = font.Spacing * lineCount;
        return new SKSize(maxWidth, height);
    }

    private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        List<string> lines = [];
        string[] rawLines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

        foreach (string rawLine in rawLines)
        {
            if (string.IsNullOrEmpty(rawLine))
            {
                lines.Add(string.Empty);
                continue;
            }

            if (maxWidth <= 0 || paint.MeasureText(rawLine) <= maxWidth)
            {
                lines.Add(rawLine);
                continue;
            }

            string current = string.Empty;
            foreach (string word in rawLine.Split(' ', StringSplitOptions.None))
            {
                if (string.IsNullOrEmpty(word))
                {
                    if (!string.IsNullOrEmpty(current))
                        current += " ";

                    continue;
                }

                string candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
                if (paint.MeasureText(candidate) <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(current))
                    lines.Add(current);

                if (paint.MeasureText(word) <= maxWidth)
                {
                    current = word;
                    continue;
                }

                current = string.Empty;
                foreach (char character in word)
                {
                    string nextCandidate = current + character;
                    if (paint.MeasureText(nextCandidate) > maxWidth && current.Length > 0)
                    {
                        lines.Add(current);
                        current = character.ToString();
                    }
                    else
                    {
                        current = nextCandidate;
                    }
                }
            }

            if (!string.IsNullOrEmpty(current))
                lines.Add(current);
        }

        return lines.Count == 0 ? [string.Empty] : lines;
    }

    private void DrawImage(SKCanvas canvas, TemplateObject templateObject)
    {
        using SKBitmap? resolved = ResolveImageForObject(templateObject);
        if (resolved is not null)
        {
            float renderWidth = templateObject.Width * ResolveScale(templateObject.ScaleX);
            float renderHeight = templateObject.Height * ResolveScale(templateObject.ScaleY);
            SKRect destination = new(templateObject.Left, templateObject.Top, templateObject.Left + Math.Max(1, renderWidth), templateObject.Top + Math.Max(1, renderHeight));
            using SKPaint bitmapPaint = new();
            canvas.DrawBitmap(resolved, destination, bitmapPaint);
            return;
        }

        float placeholderWidth = templateObject.Width * ResolveScale(templateObject.ScaleX);
        float placeholderHeight = templateObject.Height * ResolveScale(templateObject.ScaleY);
        SKRect placeholder = new(templateObject.Left, templateObject.Top, templateObject.Left + Math.Max(1, placeholderWidth), templateObject.Top + Math.Max(1, placeholderHeight));
        using SKPaint placeholderPaint = new() { Color = SKColors.LightGray, IsAntialias = true };
        canvas.DrawRect(placeholder, placeholderPaint);
    }

    private static SKBitmap? ResolveImageForObject(TemplateObject templateObject)
    {
        string? source = templateObject.Src;

        if (string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(templateObject.FixedImageSrc))
            source = templateObject.FixedImageSrc;

        if (string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(templateObject.ResolvedSrc))
            source = templateObject.ResolvedSrc;

        if (string.IsNullOrWhiteSpace(source))
            return null;

        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            int base64Index = source.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            if (base64Index < 0)
                return null;

            string base64 = source[(base64Index + 7)..];
            try
            {
                return SKBitmap.Decode(Convert.FromBase64String(base64));
            }
            catch
            {
                return null;
            }
        }

        if (!File.Exists(source))
            return null;

        try
        {
            return SKBitmap.Decode(source);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] EncodeSkiaBitmap(SKBitmap bitmap, SKEncodedImageFormat format, int quality = 100)
    {
        using SKImage image = SKImage.FromBitmap(bitmap) ?? throw new InvalidOperationException("Failed to create rendered image.");
        using SKData data = image.Encode(format, quality) ?? throw new InvalidOperationException($"Failed to encode rendered image as {format}.");
        return data.ToArray();
    }

    private static byte[] EncodeBmp(SKBitmap bitmap, int dpi)
    {
        const int fileHeaderSize = 14;
        const int infoHeaderSize = 40;
        const int bitsPerPixel = 24;

        int width = bitmap.Width;
        int height = bitmap.Height;
        int rowSize = ((bitsPerPixel * width + 31) / 32) * 4;
        int pixelDataSize = rowSize * height;
        int fileSize = fileHeaderSize + infoHeaderSize + pixelDataSize;

        using MemoryStream stream = new(fileSize);
        using BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(fileHeaderSize + infoHeaderSize);

        writer.Write(infoHeaderSize);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)bitsPerPixel);
        writer.Write(0);
        writer.Write(pixelDataSize);

        int pixelsPerMeter = (int)Math.Round(dpi / 0.0254d);
        writer.Write(pixelsPerMeter);
        writer.Write(pixelsPerMeter);
        writer.Write(0);
        writer.Write(0);

        byte[] padding = new byte[rowSize - width * 3];
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor color = bitmap.GetPixel(x, y);
                writer.Write(color.Blue);
                writer.Write(color.Green);
                writer.Write(color.Red);
            }

            if (padding.Length > 0)
                writer.Write(padding);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static int MmToPx(double millimeters, int dpi)
    {
        const double MillimetersPerInch = 25.4;
        return (int)Math.Round(millimeters / MillimetersPerInch * dpi);
    }

    private static SKRect MeasureBounds(SKPaint paint, string text)
    {
        SKRect bounds = default;
        paint.MeasureText(text, ref bounds);
        return bounds;
    }

    private static float NormalizeAngle(float angle)
    {
        if (Near(angle, 90))
            return 90;

        if (Near(angle, 180))
            return 180;

        if (Near(angle, 270))
            return 270;

        if (Near(angle, 0) || Near(angle, 360))
            return 0;

        return angle;

        static bool Near(float left, float right, float tolerance = 3f) => Math.Abs(left - right) <= tolerance;
    }

    private static float ResolveScale(float scale) => scale == 0 ? 1 : scale;

    private static string GetContentType(ImageType type) => type switch
    {
        ImageType.Bmp => "image/bmp",
        ImageType.Jpeg => "image/jpeg",
        _ => "image/png"
    };

    private static string GetFileExtension(ImageType type) => type switch
    {
        ImageType.Bmp => "bmp",
        ImageType.Jpeg => "jpg",
        _ => "png"
    };

    private SKColor ParseColor(string? colorValue, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(colorValue))
            return fallback;

        try
        {
            string normalized = colorValue.Trim();

            if (TryParseCssRgbColor(normalized, out SKColor cssColor))
                return cssColor;

            return SKColor.Parse(normalized);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(exception, "Failed to parse color '{ColorValue}'. Using fallback.", colorValue);
            return fallback;
        }
    }

    private static bool TryParseCssRgbColor(string colorValue, out SKColor color)
    {
        color = default;

        Match match = RgbColorPattern.Match(colorValue);
        if (!match.Success)
            return false;

        string[] parts = match.Groups[1].Value.Split(',');
        if (parts.Length is not 3 and not 4)
            return false;

        if (!TryParseRgbChannel(parts[0], out byte red)
            || !TryParseRgbChannel(parts[1], out byte green)
            || !TryParseRgbChannel(parts[2], out byte blue))
            return false;

        byte alpha = 255;
        if (parts.Length == 4 && !TryParseAlphaChannel(parts[3], out alpha))
            return false;

        color = new SKColor(red, green, blue, alpha);
        return true;
    }

    private static bool TryParseRgbChannel(string value, out byte channel)
    {
        channel = 0;
        string normalized = value.Trim();

        if (normalized.EndsWith("%", StringComparison.Ordinal))
        {
            if (!double.TryParse(normalized[..^1], CultureInfo.InvariantCulture, out double percent) || percent is < 0 or > 100)
                return false;

            channel = (byte)Math.Round(percent / 100d * 255d);
            return true;
        }

        if (!int.TryParse(normalized, CultureInfo.InvariantCulture, out int intValue) || intValue is < 0 or > 255)
            return false;

        channel = (byte)intValue;
        return true;
    }

    private static bool TryParseAlphaChannel(string value, out byte alpha)
    {
        alpha = 255;
        string normalized = value.Trim();

        if (normalized.EndsWith("%", StringComparison.Ordinal))
        {
            if (!double.TryParse(normalized[..^1], CultureInfo.InvariantCulture, out double percent) || percent is < 0 or > 100)
                return false;

            alpha = (byte)Math.Round(percent / 100d * 255d);
            return true;
        }

        if (!double.TryParse(normalized, CultureInfo.InvariantCulture, out double numericValue))
            return false;

        if (numericValue is >= 0 and <= 1)
        {
            alpha = (byte)Math.Round(numericValue * 255d);
            return true;
        }

        if (numericValue is < 0 or > 255)
            return false;

        alpha = (byte)Math.Round(numericValue);
        return true;
    }
}
