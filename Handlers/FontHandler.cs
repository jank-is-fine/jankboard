using Managers;

public class FontHandler : IDisposable
{
    private readonly Dictionary<string, MsdfFontCollection> _fontCollections = [];
    public IReadOnlyDictionary<string, MsdfFontCollection> FontCollections => _fontCollections;

    public FontHandler()
    {
        LoadAllFontCollections();
    }

    private void LoadAllFontCollections()
    {
        /*
                Console.WriteLine("All embedded resources:");
                foreach (var resource in resourceNames.Where(r => r.Contains("Font")))
                {
                    Console.WriteLine($"  {resource}");
                }
        */
        var fontResources = ResourceHelper.ManifestResources
            .Where(name => name.Contains(".Fonts.") || name.Contains(".fonts."))
            .Select(name =>
            {
                var fontPartIndex = name.IndexOf(".Fonts.", StringComparison.OrdinalIgnoreCase);
                var relevantPart = name[(fontPartIndex + ".Fonts.".Length)..];
                var parts = relevantPart.Split('.');

                return new
                {
                    FullName = name,
                    FontFamily = parts.Length > 0 ? parts[0] : "Unknown",
                    Style = parts.Length > 1 ? parts[1].ToLowerInvariant() : "unknown",
                    Extension = parts.Length > 2 ? parts[2].ToLowerInvariant() : ""
                };
            })
            .Where(x => !string.IsNullOrEmpty(x.FontFamily) &&
                       !string.IsNullOrEmpty(x.Style) &&
                       !string.IsNullOrEmpty(x.Extension))
            .GroupBy(x => x.FontFamily)
            .ToList();

        foreach (var fontFamilyGroup in fontResources)
        {
            var fontFamilyName = fontFamilyGroup.Key;
            Logger.Log("FontHandler", $"Loading font family: {fontFamilyName}", LogLevel.INFO);


            var styleGroups = fontFamilyGroup
                .GroupBy(x => x.Style)
                .ToDictionary(g => g.Key, g => g.ToList());

            MsdfFont? regular = null;
            MsdfFont? bold = null;
            MsdfFont? italic = null;
            MsdfFont? italicBold = null;

            foreach (var styleGroup in styleGroups)
            {
                var styleName = styleGroup.Key;
                var resources = styleGroup.Value;

                var jsonResource = resources.FirstOrDefault(r =>
                    r.Extension == "json" || r.Extension == "js")?.FullName;
                var pngResource = resources.FirstOrDefault(r =>
                    r.Extension == "png")?.FullName;

                if (!string.IsNullOrEmpty(jsonResource) && !string.IsNullOrEmpty(pngResource))
                {
                    try
                    {
                        var font = LoadFontFromResources(jsonResource, pngResource, $"{fontFamilyName}.{styleName}");

                        switch (styleName)
                        {
                            case "regular":
                                regular = font;
                                break;
                            case "bold":
                                bold = font;
                                break;
                            case "italic":
                                italic = font;
                                break;
                            case "italicbold":
                                italicBold = font;
                                break;
                            default:
                                Logger.Log("FontHandler", $"Warning: Unknown font style '{styleName}' for font family '{fontFamilyName}'", LogLevel.WARNING);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("FontHandler", $"Failed to load font {fontFamilyName}.{styleName}: {ex.Message}", LogLevel.ERROR);
                    }
                }
            }

            if (regular != null)
            {
                var collection = new MsdfFontCollection(regular, bold, italic, italicBold);
                _fontCollections[fontFamilyName] = collection;
            }
            else
            {
                Logger.Log("FontHandler", $"No regular font found for family '{fontFamilyName}'", LogLevel.FATAL);
            }
        }

    }

    private MsdfFont? LoadFontFromResources(string jsonResourceName, string pngResourceName, string fontName)
    {
        string jsonContent;
        using (var jsonStream = WindowManager.Assembly.GetManifestResourceStream(jsonResourceName))
        {
            if (jsonStream == null)
            {
                Logger.Log("FontHandler", $"JSON resource not found: {jsonResourceName}", LogLevel.ERROR);
                return null;
            }

            using var reader = new StreamReader(jsonStream);
            jsonContent = reader.ReadToEnd();
        }

        byte[] textureData;
        using (var pngStream = WindowManager.Assembly.GetManifestResourceStream(pngResourceName))
        {
            if (pngStream == null)
            {
                Logger.Log("FontHandler", $"PNG resource not found: {pngResourceName}", LogLevel.ERROR);
                return null;
            }

            using var memoryStream = new MemoryStream();
            pngStream.CopyTo(memoryStream);
            textureData = memoryStream.ToArray();
        }

        var texture = new Texture(textureData);

        return new MsdfFont(jsonContent, texture, fontName);
    }

    public MsdfFontCollection? GetFontCollection(string fontFamilyName)
    {
        if (_fontCollections.TryGetValue(fontFamilyName, out var collection))
        {
            return collection;
        }

        return null;
    }

    public MsdfFontCollection? GetDefaultFont()
    {
        if (_fontCollections.Count == 0)
        {
            return null;
        }

        return _fontCollections.Values.First();
    }

    public List<string> GetAllFonts()
    {
        return [.. _fontCollections.Select(x => x.Key)];
    }

    public bool SetCurrentFont(string fontFamilyName)
    {
        if (_fontCollections.ContainsKey(fontFamilyName))
        {
            Settings.CurrentFontName = fontFamilyName;
            return true;
        }

        Logger.Log("FontHandler", $"Font collection '{fontFamilyName}' not found. Available: {string.Join(", ", _fontCollections.Keys)}", LogLevel.ERROR);
        return false;
    }

    public MsdfFontCollection? GetCurrentFont()
    {
        if (!string.IsNullOrEmpty(Settings.CurrentFontName) &&
            _fontCollections.TryGetValue(Settings.CurrentFontName, out var collection))
        {
            return collection;
        }

        // fallback
        return GetDefaultFont();
    }

    public void Dispose()
    {
        foreach (var collection in _fontCollections.Values)
        {
            collection?.Dispose();
        }
        _fontCollections.Clear();
    }
}