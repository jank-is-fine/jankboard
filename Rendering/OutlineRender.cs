using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Managers;
using Rendering.UI;
using Silk.NET.OpenGL;

public static class OutlineRender
{
    private static GL gl = null!;
    private static Shader? shader;
    private static BufferObject<OutlineVertex> _vbo = null!;
    private static BufferObject<uint> _ebo = null!;
    private static VertexArrayObject _vao = null!;
    private static List<OutlineVertex> _vertexData = [];
    private static List<uint> _indices = [];
    private static Texture? NoiseTexture;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct OutlineVertex
    {
        public Vector3 Position;
        public Vector2 TexCoord;
        public Vector4 Color;
    }

    public static void Init()
    {
        ShaderManager.TryGetShaderByName("Outline Shader", out shader);
        NoiseTexture = TextureHandler.GetEmbeddedTextureByName("noiseTexture-64x64.png");
        gl = ShaderManager.gl;

        _vbo = new BufferObject<OutlineVertex>(gl, [], BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(gl, [], BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject(gl);

        _vao.Bind();
        _vbo.Bind();
        _ebo.Bind();

        int stride = Marshal.SizeOf<OutlineVertex>();


        _vao.SetVertexAttribute<OutlineVertex>(0, 3, VertexAttribPointerType.Float, stride, 0);
        _vao.SetVertexAttribute<OutlineVertex>(1, 2, VertexAttribPointerType.Float, stride, Marshal.OffsetOf<OutlineVertex>(nameof(OutlineVertex.TexCoord)).ToInt32());
        _vao.SetVertexAttribute<OutlineVertex>(2, 4, VertexAttribPointerType.Float, stride, Marshal.OffsetOf<OutlineVertex>(nameof(OutlineVertex.Color)).ToInt32());

    }


    public static void Clear()
    {
        _vertexData.Clear();
        _indices.Clear();
    }

    public static void AddOutlineToObjects(IEnumerable<UIObject> targetObjects, float outlineThickness, Color color)
    {
        foreach (var obj in targetObjects)
        {
            AddOutlineToObject(obj, outlineThickness, color);
        }
    }

    public static void AddOutlineToObject(UIObject? targetObject, float outlineThickness, Color color)
    {
        if (targetObject is ConnectionUI || targetObject == null) { return; }
        RectangleF bounds = targetObject.Bounds;

        if (!targetObject.IsScreenSpace)
        {
            Vector2 screenMin = Camera.WorldToScreen(new Vector2(bounds.Left, bounds.Top));
            Vector2 screenMax = Camera.WorldToScreen(new Vector2(bounds.Right, bounds.Bottom));
            bounds = new RectangleF(screenMin.X, screenMin.Y, screenMax.X - screenMin.X, screenMax.Y - screenMin.Y);
        }

        Vector4 colorVec = Settings.ColorToVec4(color);
        AddOutlineMesh(bounds, outlineThickness, colorVec);
    }

    private static void AddOutlineMesh(RectangleF bounds, float thickness, Vector4 color)
    {
        float outerLeft = bounds.Left - thickness;
        float outerRight = bounds.Right + thickness;
        float outerTop = bounds.Top + thickness;
        float outerBottom = bounds.Bottom - thickness;

        float innerLeft = bounds.Left;
        float innerRight = bounds.Right;
        float innerTop = bounds.Top;
        float innerBottom = bounds.Bottom;

        uint baseVertex = (uint)_vertexData.Count;

        // Create vertices for the outline "frame" - 8 vertices total
        // Outer vertices (0-3)
        AddVertex(new Vector3(outerLeft, outerTop, 0), new Vector2(0, 0), color);        // 0
        AddVertex(new Vector3(outerRight, outerTop, 0), new Vector2(1, 0), color);       // 1
        AddVertex(new Vector3(outerRight, outerBottom, 0), new Vector2(1, 1), color);    // 2
        AddVertex(new Vector3(outerLeft, outerBottom, 0), new Vector2(0, 1), color);     // 3

        // Inner vertices (4-7)
        AddVertex(new Vector3(innerLeft, innerTop, 0), new Vector2(0.25f, 0.25f), color);    // 4
        AddVertex(new Vector3(innerRight, innerTop, 0), new Vector2(0.75f, 0.25f), color);   // 5
        AddVertex(new Vector3(innerRight, innerBottom, 0), new Vector2(0.75f, 0.75f), color);// 6
        AddVertex(new Vector3(innerLeft, innerBottom, 0), new Vector2(0.25f, 0.75f), color); // 7

        // 12 triangles total
        // Top edge
        _indices.AddRange([
                baseVertex + 0, baseVertex + 1, baseVertex + 5,
                baseVertex + 0, baseVertex + 5, baseVertex + 4
            ]);

        // Right edge
        _indices.AddRange([
                baseVertex + 1, baseVertex + 2, baseVertex + 6,
                baseVertex + 1, baseVertex + 6, baseVertex + 5
            ]);

        // Bottom edge
        _indices.AddRange([
                baseVertex + 2, baseVertex + 3, baseVertex + 7,
                baseVertex + 2, baseVertex + 7, baseVertex + 6
            ]);

        // Left edge
        _indices.AddRange([
            baseVertex + 3, baseVertex + 0, baseVertex + 4,
            baseVertex + 3, baseVertex + 4, baseVertex + 7
            ]);
    }

    private static void AddVertex(Vector3 position, Vector2 texCoord, Vector4 color)
    {
        _vertexData.Add(new()
        {
            Position = position,
            TexCoord = texCoord,
            Color = color
        });
    }

    private static void UpdateBuffers()
    {
        if (_vertexData.Count == 0) return;

        _vbo.BufferData(CollectionsMarshal.AsSpan(_vertexData));
        _ebo.BufferData(CollectionsMarshal.AsSpan(_indices));
    }

    public static unsafe void Draw()
    {
        UpdateBuffers();
        shader?.Use();

        shader?.SetUniform("uNoiseTexture", 0);
        shader?.SetUniform("uProjection", Camera.GetStationalProjectionMatrix());
        shader?.SetUniform("uView", Matrix4x4.Identity);
        shader?.SetUniform("uModel", Matrix4x4.Identity);
        shader?.SetUniform("uTime", (float)WindowManager.window.Time);

        NoiseTexture?.Bind();

        _vao.Bind();
        gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Count, DrawElementsType.UnsignedInt, null);
        _vao.Unbind();
    }

    public static void Dispose()
    {
        _vbo?.Dispose();
        _ebo?.Dispose();
        _vao?.Dispose();
    }


}