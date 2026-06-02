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
        TextSegment? lastStyle = null;

        foreach (var line in lines)
        {
            var parsedLine = ParseTextLine(line, lastStyle);
            if (parsedLine.lineSegments.LastOrDefault() != default)
            {
                lastStyle = parsedLine.lineSegments.Last();
            }

            textStyle.lines.Add(parsedLine);
        }

        return textStyle;
    }

    private static LineTextStyle ParseTextLine(string line, TextSegment? lastStyle)
    {
        LineTextStyle lineStyle = new();

        bool bold = false;
        bool italic = false;
        Color? colorOverride = null;

        if (line.TrimStart().StartsWith("[align=", StringComparison.OrdinalIgnoreCase))
        {
            int tagStart = line.IndexOf('[');
            int tagEnd = line.IndexOf(']', tagStart);
            if (tagEnd != -1)
            {
                string tag = line.Substring(tagStart + 1, tagEnd - tagStart - 1);
                if (tag.StartsWith("align=", StringComparison.OrdinalIgnoreCase))
                {
                    string alignValue = tag[6..];
                    var anchor = ParseAlignment(alignValue);
                    if (anchor != null)
                    {
                        lineStyle.AlignemtOverride = anchor;
                        line = line[(tagEnd + 1)..];
                    }
                }
            }
        }

        if (line.TrimStart().StartsWith("[size=", StringComparison.OrdinalIgnoreCase))
        {
            int tagStart = line.IndexOf('[');
            int tagEnd = line.IndexOf(']', tagStart);
            if (tagEnd != -1)
            {
                string tag = line[1..tagEnd];
                string sizeValue = tag[5..];
                if (float.TryParse(sizeValue, out float size))
                {
                    lineStyle.SizeOverride = size;
                    line = line.Remove(tagStart, tagEnd - tagStart + 1);
                }
            }
        }

        if (line.TrimStart().StartsWith("[align=", StringComparison.OrdinalIgnoreCase))
        {
            int tagStart = line.IndexOf('[');
            int tagEnd = line.IndexOf(']', tagStart);
            if (tagEnd != -1)
            {
                string tag = line.Substring(tagStart + 1, tagEnd - tagStart - 1);
                if (tag.StartsWith("align=", StringComparison.OrdinalIgnoreCase))
                {
                    string alignValue = tag[6..];
                    var anchor = ParseAlignment(alignValue);
                    if (anchor != null)
                    {
                        lineStyle.AlignemtOverride = anchor;
                        line = line[(tagEnd + 1)..];
                    }
                }
            }
        }

        var buffer = new System.Text.StringBuilder();

        if (lastStyle != null && lastStyle != default)
        {
            switch (lastStyle.FontType)
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

            colorOverride = lastStyle.ColorOverride;
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
                        ColorOverride = colorOverride,
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
                ColorOverride = colorOverride,
            });
        }

        lineStyle.TextSizeWidthUnscaled = TextHelper.GetStringLineRenderBounds(lineStyle.lineSegments).Width;

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

    private static TextAnchorPoint? ParseAlignment(string aligmentText)
    {
        return aligmentText.Trim() switch
        {
            "center" => TextAnchorPoint.Center_Center,
            "right" => TextAnchorPoint.Right_Center,
            _ => null,
        };
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
    public TextAnchorPoint? AlignemtOverride = null;
    public float? SizeOverride = null;
    public float? TextSizeWidthUnscaled = null;
    public List<TextSegment> lineSegments = [];
}