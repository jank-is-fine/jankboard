using System.Numerics;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

public static partial class TextRenderer
{
    private static GL _gl = null!;
    public static MsdfFontCollection Font { get; private set; } = null!;
    private static Shader _shader = null!;
    private static BufferObject<Vector2> _quadVBO = null!;
    private static BufferObject<uint> _quadEBO = null!;
    private static readonly Vector2[] _quadVertices =
    [
        new(0, 0),
        new(1, 0),
        new(1, 1),
        new(0, 1)
    ];
    private static readonly uint[] _quadIndices = [0, 1, 2, 0, 2, 3];

    private static bool Initialized = false;

    private static Dictionary<FontType, FontRenderData> _renderData = [];

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct GlyphInstance
    {
        public Vector2 Position;
        public Vector2 Scale;
        public Vector4 UVBounds;
        public Vector4 Color;
        public float PxRange;
    }

    private class FontRenderData : IDisposable
    {
        public VertexArrayObject VAO;
        public BufferObject<GlyphInstance> InstanceVBO;
        public List<GlyphInstance> Instances = new(2048);

        public FontRenderData(GL gl)
        {
            VAO = new VertexArrayObject(gl);
            VAO.Bind();

            _quadVBO.Bind();
            VAO.SetVertexAttribute<GlyphInstance>(0, 2, VertexAttribPointerType.Float, 0, 0);

            InstanceVBO = new BufferObject<GlyphInstance>(gl, [], BufferTargetARB.ArrayBuffer);
            InstanceVBO.Bind();
            int stride = Marshal.SizeOf<GlyphInstance>();

            // pos (vec2)
            VAO.SetVertexAttribute<GlyphInstance>(1, 2, VertexAttribPointerType.Float, stride, 0);
            VAO.VertexAttribDivisor(1, 1);

            // Size (vec2)
            VAO.SetVertexAttribute<GlyphInstance>(2, 2, VertexAttribPointerType.Float, stride,
                Marshal.OffsetOf<GlyphInstance>(nameof(GlyphInstance.Scale)).ToInt32());
            VAO.VertexAttribDivisor(2, 1);

            // UVBounds (vec4)
            VAO.SetVertexAttribute<GlyphInstance>(3, 4, VertexAttribPointerType.Float, stride,
                Marshal.OffsetOf<GlyphInstance>(nameof(GlyphInstance.UVBounds)).ToInt32());
            VAO.VertexAttribDivisor(3, 1);

            // Color (vec4)
            VAO.SetVertexAttribute<GlyphInstance>(4, 4, VertexAttribPointerType.Float, stride,
                Marshal.OffsetOf<GlyphInstance>(nameof(GlyphInstance.Color)).ToInt32());
            VAO.VertexAttribDivisor(4, 1);

            // PxRange (float)
            VAO.SetVertexAttribute<GlyphInstance>(5, 1, VertexAttribPointerType.Float, stride,
                Marshal.OffsetOf<GlyphInstance>(nameof(GlyphInstance.PxRange)).ToInt32());
            VAO.VertexAttribDivisor(5, 1);

            VAO.Unbind();
        }

        public void Dispose()
        {
            VAO?.Dispose();
            InstanceVBO?.Dispose();
        }
    }

    public static void Init(GL gl, MsdfFontCollection font, Shader shader)
    {
        _gl = gl;
        Font = font;
        _shader = shader;

        _quadVBO = new BufferObject<Vector2>(gl, _quadVertices, BufferTargetARB.ArrayBuffer);
        _quadEBO = new BufferObject<uint>(gl, _quadIndices, BufferTargetARB.ElementArrayBuffer);

        foreach (FontType fontType in Enum.GetValues<FontType>())
        {
            if (Font.GetFont(fontType) != null)
            {
                _renderData[fontType] = new FontRenderData(gl);
            }
        }

        Initialized = true;
    }

    public static void SetFont(MsdfFontCollection msdfFont)
    {
        Font = msdfFont;
    }

    public static void Clear()
    {
        if (!Initialized) { return; }
        foreach (var data in _renderData.Values)
        {
            data.Instances.Clear();
        }
    }


    private static void UpdateInstanceBuffer(FontRenderData renderData)
    {
        if (renderData.Instances.Count == 0) return;
        renderData.InstanceVBO.Bind();
        renderData.InstanceVBO.BufferData(CollectionsMarshal.AsSpan(renderData.Instances));
    }

    public unsafe static void Draw()
    {
        if (!Initialized) return;
        _shader.Use();

        var projection = Camera.GetStationalProjectionMatrix();
        _shader.SetUniform("uProjection", projection);
        _shader.SetUniform("uView", Matrix4x4.Identity);

        foreach (var kvp in _renderData)
        {
            var renderData = kvp.Value;
            if (renderData.Instances.Count == 0) { continue; }

            UpdateInstanceBuffer(renderData);

            var font = Font.GetFont(kvp.Key);
            font?.AtlasTexture?.Bind();

            renderData.VAO.Bind();
            _quadEBO.Bind();
            _gl.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null, (uint)renderData.Instances.Count);
            renderData.VAO.Unbind();
        }
    }

    public static void Dispose()
    {
        foreach (var data in _renderData.Values) data.Dispose();
        _renderData.Clear();
        _quadVBO?.Dispose();
        _quadEBO?.Dispose();
        Initialized = false;
    }
}