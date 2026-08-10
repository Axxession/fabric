using SkiaSharp;

namespace Fabric.Server.Printing.Application;

internal sealed class FontProvider(ILogger<FontProvider> logger)
{
    private static readonly string[] PreferredFonts =
    [
        "Times New Roman",
        "DejaVu Sans",
        "Arial",
        "Liberation Sans"
    ];

    private readonly Dictionary<string, SKTypeface> _resolvedTypefaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public SKTypeface DefaultTypeface { get; } = ResolveDefaultTypeface(logger);

    public SKTypeface ResolveTypeface(string? fontFamily)
    {
        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            SKTypeface? resolved = ResolveTypefaceInternal(fontFamily.Trim());
            if (resolved is not null)
                return resolved;
        }

        return DefaultTypeface;
    }

    public SKFont CreateFont(string? fontFamily, float size) => new(ResolveTypeface(fontFamily), size);

    private static SKTypeface ResolveDefaultTypeface(ILogger logger)
    {
        foreach (string family in PreferredFonts)
        {
            SKTypeface? resolved = ResolveSystemTypeface(family);
            if (resolved is not null)
            {
                logger.LogInformation("Using default font: {Font}", resolved.FamilyName);
                return resolved;
            }
        }

        logger.LogInformation("Using default font: {Font}", SKTypeface.Default.FamilyName);
        return SKTypeface.Default;
    }

    private SKTypeface? ResolveTypefaceInternal(string fontFamily)
    {
        lock (_lock)
        {
            if (_resolvedTypefaces.TryGetValue(fontFamily, out SKTypeface? cached))
                return cached;

            SKTypeface? resolved = ResolveSystemTypeface(fontFamily);
            if (resolved is null)
                return null;

            _resolvedTypefaces[fontFamily] = resolved;
            return resolved;
        }
    }

    private static SKTypeface? ResolveSystemTypeface(string family) =>
        SKFontManager.Default.MatchFamily(family)
        ?? SKTypeface.FromFamilyName(family);
}
