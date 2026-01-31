using System.Drawing;

/// <summary>
/// <para>Parses formatted text with simple BBCODE-like tags</para>
/// </summary>

// Text-Align center etc is currently not feasable because the uiobject transform sizes are usually calculated based on text renderboxes
// Use TextHelper.TextAnchorPoint instead and set transform sizes manually
public static class TextFormatParser
{
    public static ParsedText ParseText(string text)
    {
        ParsedText textStyle = new();

        var lines = text.Split('\n');
        FontType lastStyle = FontType.REGULAR;
        foreach (var line in lines)
        {
            var parsedLine = ParseTextLine(line, lastStyle);
            if (parsedLine.lineSegments.LastOrDefault() != default)
            {
                lastStyle = parsedLine.lineSegments.Last().FontType;
            }

            textStyle.lines.Add(parsedLine);
        }

        return textStyle;
    }

    private static LineTextStyle ParseTextLine(string line, FontType lastType)
    {
        LineTextStyle lineStyle = new();

        bool bold = false;
        bool italic = false;
        Color? colorOverride = null;

        var buffer = new System.Text.StringBuilder();

        switch (lastType)
        {
            case FontType.ITALIC:
                italic = true;
                break;

            case FontType.ITALIC_BOLD:
                bold = italic = true;
                break;

            case FontType.BOLD:
                bold = true;
                break;

            default:
                break;
        }


        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '[')
            {
                int tagEnd = line.IndexOf(']', i);
                if (tagEnd == -1)
                    break; // malformed, break

                string tag = line.Substring(i + 1, tagEnd - i - 1);

                if (buffer.Length > 0)
                {
                    lineStyle.lineSegments.Add(new TextSegment
                    {
                        Text = buffer.ToString(),
                        FontType = ResolveFontType(bold, italic),
                        ColorOverride = colorOverride
                    });
                    buffer.Clear();
                }

                switch (tag)
                {
                    case "b":
                        bold = true;
                        break;

                    case "/b":
                        bold = false;
                        break;

                    case "i":
                        italic = true;
                        break;

                    case "/i":
                        italic = false;
                        break;

                    case var _ when tag.StartsWith("color=", StringComparison.OrdinalIgnoreCase):
                        var colorValue = tag[6..]; // remove tag
                        colorOverride = ParseColor(colorValue);
                        break;

                    case "/color":
                        colorOverride = null;
                        break;

                    default:
                        buffer.Append('[').Append(tag).Append(']');
                        break;
                }

                i = tagEnd; // jump past tag
            }
            else
            {
                buffer.Append(line[i]);
            }
        }

        // flush remaining
        if (buffer.Length > 0)
        {
            lineStyle.lineSegments.Add(new TextSegment
            {
                Text = buffer.ToString(),
                FontType = ResolveFontType(bold, italic),
                ColorOverride = colorOverride
            });
        }

        return lineStyle;
    }


    private static Color? ParseColor(string HexcodeOrKnownColor)
    {
        if (Enum.TryParse(HexcodeOrKnownColor, true, out KnownColor knownColor))
        {
            return Color.FromKnownColor(knownColor);
        }
        else if (HexcodeOrKnownColor.StartsWith("#") && HexcodeOrKnownColor.Length == 7)
        {
            try
            {
                return ColorTranslator.FromHtml(HexcodeOrKnownColor);
            }
            catch
            {
                Logger.Log("TextFormatParser", $"False hexcode for: {HexcodeOrKnownColor}", LogLevel.INFO);
                return null;
            }
        }


        return null;
    }

    private static FontType ResolveFontType(bool bold, bool italic)
    {
        if (bold && italic) return FontType.ITALIC_BOLD;
        if (bold) return FontType.BOLD;
        if (italic) return FontType.ITALIC;
        return FontType.REGULAR;
    }

}

public class TextSegment
{
    public Color? ColorOverride = null;
    public string Text { get; set; } = string.Empty;
    public FontType FontType { get; set; } = FontType.REGULAR;
}


public class ParsedText
{
    public List<LineTextStyle> lines = [];
}

public class LineTextStyle
{
    public List<TextSegment> lineSegments = [];
}