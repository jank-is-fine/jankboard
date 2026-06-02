using System.Numerics;

public static partial class TextRenderer
{
    public static void RenderText(
    string text,
    Vector2 position,
    Vector4 color,
    float scale = -1,
    FontType fontType = 0)
    {
        if (!Initialized) { return; }
        if (scale == -1) { scale = Settings.TextSize; }
        if (!_renderData.TryGetValue(fontType, out var renderData)) { return; }
        var targetFont = Font.GetFont(fontType);
        if (targetFont == null) { return; }

        position.Y -= targetFont.Metrics.Ascender * scale;

        float lineHeight = targetFont.Metrics.LineHeight * scale;
        var lines = text.Split('\n');
        var viewportSize = Camera.ViewportSize;

        for (int i = 0; i < lines.Length; i++)
        {
            float lineBottom = position.Y + ((i - 1) * lineHeight);
            float lineTop = position.Y + (i * lineHeight);
            if (lineBottom > viewportSize.Y) { break; }
            if (lineTop < 0) { continue; }

            Vector2 linePos = new(position.X, position.Y + i * lineHeight);

            AddTextLineInstances
            (
                lines[i],
                linePos,
                color,
                scale,
                targetFont.DistanceRange,
                fontType,
                renderData
            );
        }
    }

    private static void AddTextLineInstances(string text, Vector2 position, Vector4 color,
    float scale, float pxRange, FontType fontType, FontRenderData renderData)
    {
        var cursor = position;

        foreach (char c in text)
        {
            if (!Font.TryGetGlyph(c, out var glyph, fontType))
                continue;

            Vector2 size = (glyph!.PlaneBoundsMax - glyph.PlaneBoundsMin) * scale;
            Vector2 pos = cursor + glyph.PlaneBoundsMin * scale;

            renderData.Instances.Add(new GlyphInstance
            {
                Position = pos,
                Scale = size,
                UVBounds = new Vector4(
                    glyph.AtlasBoundsMin.X, glyph.AtlasBoundsMin.Y,
                    glyph.AtlasBoundsMax.X, glyph.AtlasBoundsMax.Y),
                Color = color,
                PxRange = pxRange
            });

            cursor.X += glyph.Advance * scale;
        }
    }

    public static void RenderTextWorld(string text, Vector2 worldPosition, Vector4 color, float scale = -1)
    {
        Vector2 screenPos = Camera.WorldToScreen(worldPosition);
        RenderText(text, screenPos, color, scale * Camera.Zoom);
    }
}