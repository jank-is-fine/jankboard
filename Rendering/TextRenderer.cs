using System.Numerics;
using Silk.NET.OpenGL;

public static partial class TextRenderer
{
    private static GL _gl = null!;
    public static MsdfFontCollection Font { get; private set; } = null!;
    private static Shader _shader = null!;
    private static BufferObject<float> _vbo = null!;
    private static BufferObject<uint> _ebo = null!;
    private static VertexArrayObject<float, uint> _vao = null!;
    private static bool Initialized = false;
    private static Dictionary<FontType, BatchData> _batches = [];

    // consider adding Cache 

    private class BatchData
    {
        public List<float> VertexData = [];
        public List<uint> Indices = [];
        public uint VertexCount = 0;

        public void AddBatchData(BatchData batchData)
        {
            VertexData.AddRange(batchData.VertexData);
            Indices.AddRange(batchData.Indices);
        }

        public void AddBatchData(List<BatchData> batches)
        {
            foreach (BatchData batchData in batches)
            {
                AddBatchData(batchData);
            }
        }
    }

    public static void SetFont(MsdfFontCollection msdfFont)
    {
        Font = msdfFont;
    }

    public static void Init(GL gl, MsdfFontCollection font, Shader shader)
    {
        _gl = gl;
        Font = font;
        _shader = shader;

        _vbo = new BufferObject<float>(_gl, [], BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(_gl, [], BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);

        // Initialize batches for all font types
        foreach (FontType fontType in Enum.GetValues<FontType>())
        {
            _batches[fontType] = new BatchData();
        }

        SetupVertexAttributes();
        Initialized = true;
    }

    private static void SetupVertexAttributes()
    {
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 10, 0); // Position
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 10, 3); // UV
        _vao.VertexAttributePointer(2, 4, VertexAttribPointerType.Float, 10, 5); // Color
        _vao.VertexAttributePointer(3, 1, VertexAttribPointerType.Float, 10, 9); // PxRange
    }

    public static void Clear()
    {
        if (!Initialized) { return; }
        foreach (var batch in _batches.Values)
        {
            batch.VertexData.Clear();
            batch.Indices.Clear();
            batch.VertexCount = 0;
        }
    }

    public static void RenderText(
    string text,
    Vector2 position,
    Vector4 color,
    float scale = -1,
    bool adjustBase = false,
    FontType fontType = 0,
    bool PxRangeAdjustOnZoom = false)
    {
        if (!Initialized) { return; }
        if (scale == -1) { scale = Settings.TextSize; }


        var batch = _batches[fontType];
        if (batch == null) { return; }

        var targetFont = Font.GetFont(fontType);
        if (targetFont == null) { return; }

        if (adjustBase)
        {
            position.Y -= targetFont.Metrics.Ascender * scale;
        }

        float lineHeight = targetFont.Metrics.LineHeight * scale;
        float PxRange = PxRangeAdjustOnZoom ? Camera.Zoom * targetFont.DistanceRange : targetFont.DistanceRange;

        var lines = text.Split('\n');

        var lineInfos = new List<(string text, uint vertexOffset, Vector2 position)>();
        uint currentOffset = batch.VertexCount;

        var viewportSize = Camera.ViewportSize;

        for (int i = 0; i < lines.Length; i++)
        {
            var lineBottom = position.Y + ((i - 1) * lineHeight);
            var lineTop = position.Y + (i * lineHeight);

            if (lineBottom > viewportSize.Y)
            {
                break;
            }

            if (lineTop < 0)
            {
                continue;
            }

            Vector2 linePos = new(
                position.X,
                position.Y + (i * lineHeight)
            );


            lineInfos.Add((lines[i], currentOffset, linePos));

            // next offset
            currentOffset += (uint)(lines[i].Length * 4);
        }

        var results = new BatchData[lineInfos.Count];
        Parallel.For(0, lineInfos.Count, i =>
        {
            var info = lineInfos[i];
            results[i] = GetTextLineGeometry(
                info.text,
                info.position,
                color,
                scale,
                PxRange,
                fontType,
                info.vertexOffset
            );
        });

        foreach (var result in results)
        {
            batch.AddBatchData(result);
        }

        batch.VertexCount = currentOffset;
    }

    //TODO culling is not feasable because of the vertex offsets - find a better way 
    private static BatchData GetTextLineGeometry(
        string text,
        Vector2 position,
        Vector4 color,
        float scale,
        float PxRange,
        FontType fontType,
        uint vertexOffset)
    {
        var cursor = position;
        BatchData batchdata = new();

        uint vertexIndex = vertexOffset;

        foreach (var c in text)
        {
            if (!Font.TryGetGlyph(c, out var glyph, fontType))
                continue;

            float nextX = cursor.X + glyph!.Advance * scale;

            AddCharacterQuadToBatch(batchdata, glyph!, cursor, color, scale, vertexIndex, PxRange);
            vertexIndex += 4;

            cursor.X = nextX;
        }

        batchdata.VertexCount = vertexIndex - vertexOffset;
        return batchdata;
    }


    public static void RenderTextParsed(
        ParsedText textStyle,
        Vector2 position,
        Vector4 color,
        float scale = -1,
        bool adjustBase = true,
        bool PxRangeAdjustOnZoom = true)
    {
        if (!Initialized) { return; }
        if (textStyle.lines.Count <= 0) { return; }
        if (scale == -1) { scale = Settings.TextSize; }

        var baseFont = Font.GetFont(FontType.REGULAR);
        if (baseFont == null) { return; }

        if (adjustBase)
        {
            position.Y -= baseFont.Metrics.Ascender * scale;
        }

        float lineHeight = baseFont.Metrics.LineHeight * scale;
        float PxRange = PxRangeAdjustOnZoom ? Camera.Zoom * baseFont.DistanceRange : baseFont.DistanceRange;

        var tasks = new List<Task<List<(FontType, BatchData)>>>();

        var viewportSize = Camera.ViewportSize;


        for (int i = 0; i < textStyle.lines.Count; i++)
        {
            int lineIndex = i;
            var lineBottom = position.Y + ((i - 1) * lineHeight);
            var lineTop = position.Y + (i * lineHeight);

            if (lineBottom > viewportSize.Y)
            {
                break;
            }

            if (lineTop < 0)
            {
                continue;
            }

            Vector2 linePos = new(
                position.X,
                position.Y + (i * lineHeight)
            );

            tasks.Add(Task.Run(() =>
                GenerateSingleLineGeometryParsed(
                    linePos,
                    color,
                    scale,
                    PxRange,
                    textStyle.lines[lineIndex].lineSegments
                )
            ));
        }

        if (tasks.Count > 0)
        {
            Task.WaitAll([.. tasks]);
        }

        foreach (var task in tasks)
        {
            var lineGeometry = task.Result;
            foreach (var (fontType, batchData) in lineGeometry)
            {
                if (_batches.TryGetValue(fontType, out var targetBatch))
                {
                    uint vertexCount = targetBatch.VertexCount;
                    for (int i = 0; i < batchData.Indices.Count; i++)
                    {
                        batchData.Indices[i] += vertexCount;
                    }

                    targetBatch.VertexData.AddRange(batchData.VertexData);
                    targetBatch.Indices.AddRange(batchData.Indices);
                    targetBatch.VertexCount += batchData.VertexCount;
                }
            }
        }
    }

    //TODO culling is not feasable because of the vertex offsets - find a better way 
    private static List<(FontType, BatchData)> GenerateSingleLineGeometryParsed
    (
        Vector2 position,
        Vector4 baseColor,
        float scale,
        float PxRange,
        List<TextSegment> lineSegments
    )
    {
        var results = new List<(FontType, BatchData)>();
        var cursor = position;
        var geometryByFontType = new Dictionary<FontType, BatchData>();

        foreach (var segment in lineSegments)
        {
            if (!geometryByFontType.TryGetValue(segment.FontType, out var batchData))
            {
                batchData = new BatchData();
                geometryByFontType[segment.FontType] = batchData;
            }

            var effectiveColor = segment.ColorOverride.HasValue
                ? Settings.ColorToVec4((System.Drawing.Color)segment.ColorOverride)
                : baseColor;

            foreach (var c in segment.Text)
            {
                if (!Font.TryGetGlyph(c, out var glyph, segment.FontType))
                    continue;

                float nextX = cursor.X + glyph!.Advance * scale;

                uint vertexOffset = (uint)(batchData.VertexData.Count / 10);
                AddCharacterQuadToBatch(
                    batchData,
                    glyph!,
                    cursor,
                    effectiveColor,
                    scale,
                    vertexOffset,
                    PxRange);


                cursor.X = nextX;
            }
        }

        foreach (var kvp in geometryByFontType)
        {
            kvp.Value.VertexCount = (uint)(kvp.Value.VertexData.Count / 10);
            results.Add((kvp.Key, kvp.Value));
        }

        return results;
    }

    public static void RenderTextWorldParsed(ParsedText parsedText, Vector2 worldPosition, Vector4 color, float scale = 1.0f, bool pxRangeAdjustOnZoom = false)
    {
        if (!Initialized) { return; }
        Vector2 screenPos = Camera.WorldToScreen(worldPosition);
        RenderTextParsed(parsedText, screenPos, color, scale * Camera.Zoom, adjustBase: true, PxRangeAdjustOnZoom: pxRangeAdjustOnZoom);
    }

    public static void RenderTextWorld(string text, Vector2 worldPosition, Vector4 color, float scale = -1, bool pxRangeAdjustOnZoom = false)
    {
        if (!Initialized) { return; }
        Vector2 screenPos = Camera.WorldToScreen(worldPosition);
        RenderText(text, screenPos, color, scale * Camera.Zoom, true, PxRangeAdjustOnZoom: pxRangeAdjustOnZoom);
    }

    private static void AddCharacterQuadToBatch(BatchData batch, MsdfFont.Glyph glyph, Vector2 position,
    Vector4 color, float scale, uint baseVertex, float PxRange)
    {
        var min = position + glyph.PlaneBoundsMin * scale;
        var max = position + glyph.PlaneBoundsMax * scale;

        // Add vertices
        AddVertexToBatch(batch, new Vector3(min.X, min.Y, 0), new Vector2(glyph.AtlasBoundsMin.X, glyph.AtlasBoundsMin.Y), color, PxRange);
        AddVertexToBatch(batch, new Vector3(max.X, min.Y, 0), new Vector2(glyph.AtlasBoundsMax.X, glyph.AtlasBoundsMin.Y), color, PxRange);
        AddVertexToBatch(batch, new Vector3(max.X, max.Y, 0), new Vector2(glyph.AtlasBoundsMax.X, glyph.AtlasBoundsMax.Y), color, PxRange);
        AddVertexToBatch(batch, new Vector3(min.X, max.Y, 0), new Vector2(glyph.AtlasBoundsMin.X, glyph.AtlasBoundsMax.Y), color, PxRange);

        // Add indices for two triangles
        batch.Indices.AddRange(baseVertex, baseVertex + 1, baseVertex + 2, baseVertex, baseVertex + 2, baseVertex + 3);
    }

    private static void AddVertexToBatch(BatchData batch, Vector3 position, Vector2 texCoord, Vector4 color, float PxRange)
    {
        batch.VertexData.AddRange(position.X, position.Y, position.Z);
        batch.VertexData.AddRange(texCoord.X, texCoord.Y);
        batch.VertexData.AddRange(color.X, color.Y, color.Z, color.W);

        batch.VertexData.Add(PxRange);
    }

    private static unsafe void UpdateBatchBuffers(FontType fontType)
    {
        var batch = _batches[fontType];
        if (batch.VertexData.Count == 0) return;

        var vertexArray = batch.VertexData.ToArray();
        var indexArray = batch.Indices.ToArray();

        _vbo.Bind();
        fixed (float* vertexPtr = vertexArray)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexArray.Length * sizeof(float)), vertexPtr, BufferUsageARB.DynamicDraw);
        }
        _vbo.Unbind();

        _ebo.Bind();
        fixed (uint* indexPtr = indexArray)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexArray.Length * sizeof(uint)), indexPtr, BufferUsageARB.DynamicDraw);
        }
        _ebo.Unbind();
    }

    public static unsafe void Draw()
    {
        if (!Initialized) { return; }
        _shader.Use();

        var projection = Camera.GetStationalProjectionMatrix();
        _shader.SetUniform("uProjection", projection);
        _shader.SetUniform("uView", Matrix4x4.Identity);
        _shader.SetUniform("uModel", Matrix4x4.Identity);

        foreach (var fontType in _batches.Keys)
        {
            var batch = _batches[fontType];
            if (batch.VertexData.Count == 0) continue;

            UpdateBatchBuffers(fontType);

            var font = Font.GetFont(fontType);
            font?.AtlasTexture?.Bind();

            _vao.Bind();
            _gl.DrawElements(PrimitiveType.Triangles, (uint)batch.Indices.Count, DrawElementsType.UnsignedInt, null);
            _vao.Unbind();
        }
    }

    public static void Dispose()
    {
        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();
    }
}
