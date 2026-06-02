using System.Drawing;
using System.Numerics;

public static partial class TextRenderer
{
    public static void RenderTextParsed
    (
        ParsedText textStyle,
        Vector2 position,
        Vector4 baseColor,
        float? sourceWidth = null,
        bool WorldSpace = false
    )
    {
        if (!Initialized) { return; }
        if (textStyle.lines.Count <= 0) { return; }
        float scale = Settings.TextSize;

        if (WorldSpace)
        {
            if (sourceWidth != null) sourceWidth *= Camera.Zoom;
            scale *= Camera.Zoom;
            position = Camera.WorldToScreen(position);
        }

        var baseFont = Font.GetFont(FontType.REGULAR);
        if (baseFont == null) { return; }

        var Ascender = -baseFont.Metrics.Ascender;

        float lineHeight = baseFont.Metrics.LineHeight;
        var viewportSize = Camera.ViewportSize;

        var currentY = position.Y;

        for (int i = 0; i < textStyle.lines.Count; i++)
        {
            var line = textStyle.lines[i];

            Vector2 linePos = new(position.X, currentY);
            float effectiveScale = line.SizeOverride.HasValue ? (float)line.SizeOverride : scale;

            if (WorldSpace && line.SizeOverride.HasValue)
            {
                effectiveScale *= Camera.Zoom;
            }

            linePos.Y += Ascender * effectiveScale;

            float lineBottom = currentY - (lineHeight * effectiveScale);
            float lineTop = currentY + (lineHeight * effectiveScale);
            if (lineBottom > viewportSize.Y) break;
            if (lineTop < 0) continue;

            var aligmentText = textStyle.lines[i].AlignemtOverride;
            if (aligmentText != null && sourceWidth != null && textStyle.lines[i].TextSizeWidthUnscaled.HasValue)
            {
                var lineWidth = textStyle.lines[i].TextSizeWidthUnscaled * effectiveScale;
                if (lineWidth < sourceWidth)
                {
                    if (aligmentText == TextAnchorPoint.Center_Center)
                    {
                        linePos.X = (float)(linePos.X + (sourceWidth / 2f) - (lineWidth / 2f));
                    }
                    else if (aligmentText == TextAnchorPoint.Right_Center)
                    {
                        linePos.X = (float)(linePos.X + sourceWidth - lineWidth);
                    }
                }
            }

            GenerateLineInstancesParsed
            (
                linePos,
                baseColor,
                effectiveScale,
                baseFont.DistanceRange,
                textStyle.lines[i].lineSegments
            );

            currentY += lineHeight * effectiveScale;
        }
    }

    private static void GenerateLineInstancesParsed(Vector2 position, Vector4 baseColor,
    float scale, float pxRange, List<TextSegment> lineSegments)
    {
        var cursor = position;

        foreach (var segment in lineSegments)
        {
            if (!_renderData.TryGetValue(segment.FontType, out var renderData)) continue;

            var effectiveColor = segment.ColorOverride.HasValue
                ? Settings.ColorToVec4((System.Drawing.Color)segment.ColorOverride)
                : baseColor;

            foreach (var c in segment.Text)
            {
                if (!Font.TryGetGlyph(c, out var glyph, segment.FontType))
                    continue;

                Vector2 size = (glyph!.PlaneBoundsMax - glyph.PlaneBoundsMin) * scale;
                Vector2 pos = cursor + glyph.PlaneBoundsMin * scale;

                renderData.Instances.Add(new GlyphInstance
                {
                    Position = pos,
                    Scale = size,
                    UVBounds = new
                    (
                        glyph.AtlasBoundsMin.X, glyph.AtlasBoundsMin.Y,
                        glyph.AtlasBoundsMax.X, glyph.AtlasBoundsMax.Y
                    ),
                    Color = effectiveColor,
                    PxRange = pxRange
                });

                cursor.X += glyph.Advance * scale;
            }
        }
    }
}