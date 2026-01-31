using System.Drawing;
using System.Numerics;

/// <summary>
/// <para>Provides text measurement and positioning utilities for rendering (formatted) text</para>
/// <para>Calculates bounding boxes, cursor positions, and text widths for (multi-line and formatted) text</para>
/// <para>Supports multiple text anchor points for both screenspace and worldpace positioning</para>
/// </summary>

public static class TextHelper
{
    public static RectangleF GetStringRenderBox(ParsedText parsedText,
        float scale = -1f)
    {
        if (scale == -1) { scale = Settings.TextSize; }
        if (parsedText == null || parsedText.lines.Count < 1) return new RectangleF(0, 0, 0, 0);

        var firstSegment = parsedText.lines[0].lineSegments.FirstOrDefault();
        var font = TextRenderer.Font.GetFont(firstSegment?.FontType ?? FontType.REGULAR);
        if (font == null) return new RectangleF(0, 0, 0, 0);

        float maxWidth = 0;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float currentY = -font.Metrics.Ascender * scale;

        foreach (var line in parsedText.lines)
        {
            var lineBounds = GetStringLineRenderBounds(line.lineSegments, scale);
            float lineWidth = lineBounds.Width;

            if (lineWidth > maxWidth)
                maxWidth = lineWidth;

            float lineTop = currentY + lineBounds.Top;
            float lineBottom = currentY + lineBounds.Bottom;

            minY = Math.Min(minY, lineTop);
            maxY = Math.Max(maxY, lineBottom);

            currentY += font.Metrics.LineHeight * scale;
        }

        if (minY == float.MaxValue)
        {
            return new RectangleF(0, 0, maxWidth, 0);
        }

        return new RectangleF(0, minY, maxWidth, maxY - minY + -font.Metrics.Ascender * scale / 2f);
    }


    public static RectangleF GetStringLineRenderBounds(List<TextSegment> lineSegments,
        float scale = 1f)
    {
        if (lineSegments == null || lineSegments.Count == 0)
            return new RectangleF(0, 0, 0, 0);

        float xAdvance = 0;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var segment in lineSegments)
        {
            var font = TextRenderer.Font.GetFont(segment.FontType);
            if (font == null) continue;

            foreach (char c in segment.Text)
            {
                if (!font.Glyphs.TryGetValue(c, out var glyph))
                    continue;

                float charMinY = glyph.PlaneBoundsMin.Y * scale;
                float charMaxY = glyph.PlaneBoundsMax.Y * scale;

                minY = Math.Min(minY, charMinY);
                maxY = Math.Max(maxY, charMaxY);

                xAdvance += glyph.Advance * scale;
            }
        }

        if (minY == float.MaxValue)
        {
            var font = TextRenderer.Font.GetFont(FontType.REGULAR);
            float height = font?.Metrics.LineHeight * scale / 2f ?? 5f;
            return new RectangleF(0, 0, xAdvance, height);
        }

        return new RectangleF(0, minY, xAdvance, maxY - minY);
    }

    public static RectangleF GetStringLineRenderBounds(string targetLine,
        FontType fontType = FontType.REGULAR,
        float scale = 1f)
    {
        var lineSegments = new List<TextSegment>
        {
            new() { Text = targetLine, FontType = fontType }
        };

        return GetStringLineRenderBounds(lineSegments, scale);
    }


    public static TextMeasurement GetCursorPosition(string text, int cursorIndex, Vector2 startPosition, float scale = -1)
    {
        if (scale == -1) scale = Settings.TextSize;

        var measurement = new TextMeasurement();

        float cursorX = startPosition.X;
        float cursorY = startPosition.Y;
        int currentCharIndex = 0;
        int currentLine = 0;
        string currentLineText = "";

        var font = TextRenderer.Font.GetFont(FontType.REGULAR);

        if (font == null) return measurement;

        measurement.LineHeight = font?.Metrics.LineHeight * scale ?? 16f;

        foreach (var c in text)
        {
            if (currentCharIndex == cursorIndex)
            {
                measurement.CursorPosition = new Vector2(cursorX, cursorY);
                measurement.LineNumber = currentLine;
                measurement.LineText = currentLineText;
                return measurement;
            }

            if (c == '\n')
            {
                cursorX = startPosition.X;
                cursorY -= measurement.LineHeight;
                currentLine++;
                currentLineText = "";
                currentCharIndex++;
                continue;
            }

            if (font!.Glyphs.TryGetValue(c, out var glyph))
            {
                cursorX += glyph.Advance * scale;
            }

            currentLineText += c;
            currentCharIndex++;
        }

        measurement.CursorPosition = new Vector2(cursorX, cursorY);
        measurement.LineNumber = currentLine;
        return measurement;
    }


    public static float GetFormattedTextWidth(string text, float scale = -1)
    {
        if (scale == -1) scale = Settings.TextSize;

        var parsedText = TextFormatParser.ParseText(text);
        return GetFormattedTextWidth(parsedText, scale);
    }


    public static float GetFormattedTextWidth(ParsedText parsedText, float scale = -1)
    {
        if (scale == -1) scale = Settings.TextSize;
        if (parsedText == null || parsedText.lines.Count == 0) return 0;

        float maxWidth = 0;

        foreach (var line in parsedText.lines)
        {
            float lineWidth = 0;
            foreach (var segment in line.lineSegments)
            {
                var font = TextRenderer.Font.GetFont(segment.FontType);
                if (font == null) continue;

                foreach (char c in segment.Text)
                {
                    if (font.Glyphs.TryGetValue(c, out var glyph))
                    {
                        lineWidth += glyph.Advance * scale;
                    }
                }
            }

            if (lineWidth > maxWidth)
                maxWidth = lineWidth;
        }

        return maxWidth;
    }

    public static RectangleF GetStringRenderBox(string targetString,
        FontType fontType = FontType.REGULAR,
        float scale = -1f)
    {
        if (scale == -1) { scale = Settings.TextSize; }

        var font = TextRenderer.Font.GetFont(fontType);
        if (font == null || targetString == null || targetString.Length <= 0) return new RectangleF(0, 0, 0, 0);

        float maxWidth = 0;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        string[] lines = targetString.Split('\n');
        float currentY = -font.Metrics.Ascender * scale;

        foreach (string line in lines)
        {
            var lineBounds = GetStringLineRenderBounds(line, fontType, scale);
            float lineWidth = lineBounds.Width;

            if (lineWidth > maxWidth) { maxWidth = lineWidth; }

            float lineTop = currentY + lineBounds.Top;
            float lineBottom = currentY + lineBounds.Bottom;

            minY = Math.Min(minY, lineTop);
            maxY = Math.Max(maxY, lineBottom);

            currentY += font.Metrics.LineHeight * scale;
        }

        if (minY == float.MaxValue)
        {
            if (lines.Length < 1)
            {
                return new RectangleF(0, 0, 0, font.Metrics.LineHeight * scale);
            }
        }

        if (targetString.Length > 0 && targetString.Last() == '\n')
        {
            return new RectangleF(0, minY, maxWidth, maxY - minY);
        }
        else
        {
            return new RectangleF(0, minY, maxWidth, maxY - minY - font.Metrics.Ascender * scale / 2f);
        }
    }

    public static Vector2 CalculateTextPosition(ParsedText parsedText,
        Vector2 targetPos,
        Vector2 SourceScale,
        TextAnchorPoint textAnchorPoint,
        bool isScreenSpace = false)
    {
        var textBox = GetStringRenderBox(parsedText, Settings.TextSize);
        var relativePos = GetRelativeTextPositionVector(textAnchorPoint, isScreenSpace);

        Vector2 buttonCorner = targetPos - (SourceScale * 0.5f);
        Vector2 sizeDifference = SourceScale - new Vector2(textBox.Width, textBox.Height);

        return buttonCorner + sizeDifference * relativePos;
    }

    public static Vector2 CalculateTextPosition(string targetString,
        Vector2 targetPos,
        Vector2 SourceScale,
        TextAnchorPoint textAnchorPoint,
        bool isScreenSpace = false)
    {
        var parsedText = TextFormatParser.ParseText(targetString);
        return CalculateTextPosition(parsedText, targetPos, SourceScale, textAnchorPoint, isScreenSpace);
    }


    private static Vector2 GetRelativeTextPositionVector(TextAnchorPoint textAnchorPoint, bool isScreenSpace = false)
    {
        if (isScreenSpace)
        {
            return GetPositionVectorScreenSpace(textAnchorPoint);
        }
        else
        {
            return GetPositionVectorWorldSpace(textAnchorPoint);
        }
    }

    private static Vector2 GetPositionVectorScreenSpace(TextAnchorPoint textAnchorPoint)
    {
        return textAnchorPoint switch
        {
            TextAnchorPoint.Left_Top => new Vector2(0.0f, 0.0f),
            TextAnchorPoint.Left_Center => new Vector2(0.0f, 0.5f),
            TextAnchorPoint.Left_Bottom => new Vector2(0.0f, 1.0f),

            TextAnchorPoint.Center_Top => new Vector2(0.5f, 0.0f),
            TextAnchorPoint.Center_Center => new Vector2(0.5f, 0.5f),
            TextAnchorPoint.Center_Bottom => new Vector2(0.5f, 1.0f),

            TextAnchorPoint.Right_Top => new Vector2(1.0f, 0.0f),
            TextAnchorPoint.Right_Center => new Vector2(1.0f, 0.5f),
            TextAnchorPoint.Right_Bottom => new Vector2(1.0f, 1.0f),

            _ => new Vector2(0.0f, 0.0f),
        };
    }

    private static Vector2 GetPositionVectorWorldSpace(TextAnchorPoint textAnchorPoint)
    {
        return textAnchorPoint switch
        {
            TextAnchorPoint.Left_Top => new Vector2(0.0f, 1.0f),
            TextAnchorPoint.Left_Center => new Vector2(0.0f, 0.5f),
            TextAnchorPoint.Left_Bottom => new Vector2(0.0f, 0.0f),

            TextAnchorPoint.Center_Top => new Vector2(0.5f, 1.0f),
            TextAnchorPoint.Center_Center => new Vector2(0.5f, 0.5f),
            TextAnchorPoint.Center_Bottom => new Vector2(0.5f, 0.0f),

            TextAnchorPoint.Right_Top => new Vector2(1.0f, 1.0f),
            TextAnchorPoint.Right_Center => new Vector2(1.0f, 0.5f),
            TextAnchorPoint.Right_Bottom => new Vector2(1.0f, 0.0f),

            _ => new Vector2(0.0f, 0.0f),
        };
    }

    public static float GetTextWidth(string text, FontType fontType = FontType.REGULAR, float scale = -1)
    {
        if (scale == -1) scale = Settings.TextSize;

        float width = 0;
        var font = TextRenderer.Font.GetFont(fontType);
        if (font == null) return 0;

        foreach (char c in text)
        {
            if (font.Glyphs.TryGetValue(c, out var glyph))
            {
                width += glyph.Advance * scale;
            }
        }

        return width;
    }

    public class TextMeasurement
    {
        public Vector2 CursorPosition { get; set; }
        public int LineNumber { get; set; }
        public float LineHeight { get; set; }
        public string LineText { get; set; } = string.Empty;
    }

    public static Color GetContrastColor(Color color)
    {
        // https://24ways.org/2010/calculating-color-contrast and
        // https://stackoverflow.com/questions/11867545/change-text-color-based-on-brightness-of-the-covered-background-area
        var yiq = ((color.R * 299) + (color.G * 587) + (color.B * 114)) / 1000;

        return yiq >= 128 ? Color.Black : Color.White;
    }
}

public enum TextAnchorPoint
{
    Left_Top,
    Left_Center,
    Left_Bottom,

    Center_Top,
    Center_Center,
    Center_Bottom,

    Right_Top,
    Right_Center,
    Right_Bottom
}