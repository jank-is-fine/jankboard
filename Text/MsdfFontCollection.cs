using static MsdfFont;

/// <summary>
/// <para>Manages a collection of MSDF font variants (regular, bold, italic, italicbold) with fallback
/// to Regular variant which needs to be passed in order to succesfully create a collection</para>
/// </summary>

public class MsdfFontCollection(MsdfFont regularFont, MsdfFont? boldFont = null,
    MsdfFont? italicFont = null, MsdfFont? italicBoldFont = null) : IDisposable
{
    public string FontCollectionName { get; private set; } = string.Empty;
    private MsdfFont Regular = regularFont;
    private MsdfFont? Bold = boldFont;
    private MsdfFont? Italic = italicFont;
    private MsdfFont? ItalicBold = italicBoldFont;

    public FontMetrics Metrics => Regular.Metrics;

    public MsdfFont? GetFont(FontType fontType)
    {
        return fontType switch
        {
            FontType.BOLD => Bold ?? Regular,
            FontType.ITALIC => Italic ?? Regular,
            FontType.ITALIC_BOLD => ItalicBold ?? Regular,
            _ => Regular
        };
    }

    public bool TryGetGlyph(char targetCharacter, out Glyph? glyph, FontType fontType = FontType.REGULAR)
    {
        var targetFont = GetFont(fontType);
        if (targetFont?.Glyphs.TryGetValue(targetCharacter, out var Foundglyph) == true)
        {
            glyph = Foundglyph;
            return true;
        }

        if (fontType != FontType.REGULAR)
        {
            targetFont = GetFont(FontType.REGULAR);
            if (targetFont?.Glyphs.TryGetValue(targetCharacter, out Foundglyph) == true)
            {
                glyph = Foundglyph;
                return true;
            }
        }

        glyph = null;
        return false;
    }

    public void Dispose()
    {
        Regular?.Dispose();
        Bold?.Dispose();
        Italic?.Dispose();
        ItalicBold?.Dispose();
    }
}

public enum FontType
{
    REGULAR,
    BOLD,
    ITALIC,
    ITALIC_BOLD
}