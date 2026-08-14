using SkiaSharp;

namespace Fabric.Server.Printing.Application;

internal sealed class FontProvider
{
    private const string BundledFontDirectory = "Printing/Assets/Fonts";
    private const string BundledDefaultFontFamily = "Noto Sans";
    private static readonly string[] PreferredFonts =
    [
        "Times New Roman",
        "DejaVu Sans",
        "Arial",
        "Liberation Sans",
        "Noto Sans"
    ];

    private readonly Dictionary<string, SKTypeface> _resolvedTypefaces = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SKTypeface> _bundledTypefaces;
    private readonly ILogger<FontProvider> _logger;
    private readonly Lock _lock = new();
    private readonly string[] _installedFamilies;

    public SKTypeface DefaultTypeface { get; }

    public FontProvider(ILogger<FontProvider> logger)
    {
        _logger = logger;
        _bundledTypefaces = LoadBundledTypefaces(logger);
        _installedFamilies = [.. SKFontManager.Default.FontFamilies.Where(family => !string.IsNullOrWhiteSpace(family)).Distinct(StringComparer.OrdinalIgnoreCase)];
        DefaultTypeface = ResolveDefaultTypeface(logger);
    }

    public SKTypeface ResolveTypeface(string? fontFamily)
    {
        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            SKTypeface? resolved = ResolveTypefaceInternal(fontFamily.Trim(), out _);
            if (resolved is not null)
                return resolved;
        }

        return DefaultTypeface;
    }

    public SKFont CreateFont(string? fontFamily, float size)
    {
        SKTypeface typeface = ResolveTypefaceInternal(fontFamily?.Trim(), out string source) ?? DefaultTypeface;
        string resolvedFont = string.IsNullOrWhiteSpace(typeface.FamilyName) ? "<unnamed>" : typeface.FamilyName;
        _logger.LogDebug("Resolved font request '{RequestedFont}' to '{ResolvedFont}' from {Source} at size {Size}", fontFamily, resolvedFont, source, size);
        return new SKFont(typeface, size);
    }

    private SKTypeface ResolveDefaultTypeface(ILogger logger)
    {
        foreach (string family in PreferredFonts)
        {
            SKTypeface? resolved = ResolveSystemTypeface(family);
            if (resolved is not null)
            {
                logger.LogInformation("Using default font: {Font} from system", resolved.FamilyName);
                return resolved;
            }

            if (_bundledTypefaces.TryGetValue(family, out SKTypeface? bundledResolved))
            {
                logger.LogInformation("Using default font: {Font} from bundled", bundledResolved.FamilyName);
                return bundledResolved;
            }
        }

        if (_bundledTypefaces.TryGetValue(BundledDefaultFontFamily, out SKTypeface? bundledDefault))
        {
            logger.LogInformation("Using default font: {Font} from bundled", bundledDefault.FamilyName);
            return bundledDefault;
        }

        if (_bundledTypefaces.Count > 0)
        {
            SKTypeface bundledFallback = _bundledTypefaces.Values.First();
            logger.LogInformation("Using default font: {Font} from bundled", bundledFallback.FamilyName);
            return bundledFallback;
        }

        logger.LogInformation("Using default font: {Font} from default", SKTypeface.Default.FamilyName);
        return SKTypeface.Default;
    }

    private SKTypeface? ResolveTypefaceInternal(string? fontFamily, out string source)
    {
        source = "default";
        if (string.IsNullOrWhiteSpace(fontFamily))
            return DefaultTypeface;

        lock (_lock)
        {
            if (_resolvedTypefaces.TryGetValue(fontFamily, out SKTypeface? cached))
            {
                source = GetTypefaceSource(cached);
                return cached;
            }

            SKTypeface? resolved = ResolveSystemTypeface(fontFamily);
            if (resolved is not null)
            {
                source = "system";
            }
            else if (_bundledTypefaces.TryGetValue(fontFamily, out SKTypeface? bundledResolved))
            {
                resolved = bundledResolved;
                source = "bundled";
            }
            else if (ResolvePreferredInstalledTypeface(_installedFamilies) is SKTypeface preferredSystem)
            {
                resolved = preferredSystem;
                source = "system";
            }
            else if (ResolvePreferredBundledTypeface() is SKTypeface preferredBundled)
            {
                resolved = preferredBundled;
                source = "bundled";
            }
            else if (_installedFamilies.Length > 0 && ResolveSystemTypeface(_installedFamilies[0]) is SKTypeface firstSystem)
            {
                resolved = firstSystem;
                source = "system";
            }
            else if (_bundledTypefaces.Count > 0)
            {
                resolved = _bundledTypefaces.Values.First();
                source = "bundled";
            }

            if (resolved is null)
                return null;

            _resolvedTypefaces[fontFamily] = resolved;
            return resolved;
        }
    }

    private static SKTypeface? ResolvePreferredInstalledTypeface(IEnumerable<string> installedFamilies)
    {
        HashSet<string> installed = [.. installedFamilies];
        string? matchingFamily = PreferredFonts.FirstOrDefault(installed.Contains);
        return matchingFamily is null ? null : ResolveSystemTypeface(matchingFamily);
    }

    private SKTypeface? ResolvePreferredBundledTypeface()
    {
        foreach (string family in PreferredFonts)
        {
            if (_bundledTypefaces.TryGetValue(family, out SKTypeface? typeface))
                return typeface;
        }

        return null;
    }

    private static Dictionary<string, SKTypeface> LoadBundledTypefaces(ILogger logger)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, BundledFontDirectory);
        if (!Directory.Exists(directory))
        {
            logger.LogInformation("No bundled font directory found at {FontDirectory}", directory);
            return new Dictionary<string, SKTypeface>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, SKTypeface>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in Directory.EnumerateFiles(directory, "*.ttf", SearchOption.TopDirectoryOnly))
        {
            try
            {
                SKTypeface? typeface = SKTypeface.FromFile(path);
                if (typeface is null || string.IsNullOrWhiteSpace(typeface.FamilyName))
                    continue;

                result.TryAdd(typeface.FamilyName, typeface);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to load bundled font {FontPath}", path);
            }
        }

        logger.LogInformation("Loaded {BundledFontCount} bundled font families from {FontDirectory}", result.Count, directory);
        return result;
    }

    private string GetTypefaceSource(SKTypeface typeface) =>
        _bundledTypefaces.Values.Any(candidate => ReferenceEquals(candidate, typeface)) ? "bundled" : "system";

    private static SKTypeface? ResolveSystemTypeface(string family)
    {
        SKTypeface? resolved = SKFontManager.Default.MatchFamily(family)
            ?? SKTypeface.FromFamilyName(family);

        return IsUsableTypeface(resolved) ? resolved : null;
    }

    private static bool IsUsableTypeface(SKTypeface? typeface) =>
        typeface is not null && !string.IsNullOrWhiteSpace(typeface.FamilyName);
}
