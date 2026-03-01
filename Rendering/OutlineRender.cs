using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.OpenGL;

public static class OutlineRender
{
    private static GL gl = null!;
    private static Shader? shader;
    private static BufferObject<float> _vbo = null!;
    private static BufferObject<uint> _ebo = null!;
    private static VertexArrayObject<float, uint> _vao = null!;
    private static List<float> _vertexData = [];
    private static List<uint> _indices = [];
    private static Texture? NoiseTexture;

    public static void Init()
    {
        ShaderManager.TryGetShaderByName("Outline Shader", out shader);
        NoiseTexture = TextureHandler.GetEmbeddedTextureByName("noiseTexture-64x64.png");
        gl = ShaderManager.gl;
        _vbo = new BufferObject<float>(gl, [], BufferTargetARB.ArrayBuffer);
        _ebo = new BufferObject<uint>(gl, [], BufferTargetARB.ElementArrayBuffer);
        _vao = new VertexArrayObject<float, uint>(gl, _vbo, _ebo);

        SetupVertexAttributes();
    }

    private static void SetupVertexAttributes()
    {
        _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 9, 0); // Position
        _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 9, 3); // UV
        _vao.VertexAttributePointer(2, 4, VertexAttribPointerType.Float, 9, 5); // Color
    }

    public static void Clear()
    {
        _vertexData.Clear();
        _indices.Clear();
    }

    public static void AddOutlineToObjects(List<UIObject> targetObjects, float outlineThickness, Color color)
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

        uint baseVertex = (uint)(_vertexData.Count / 9);

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
        _vertexData.Add(position.X);
        _vertexData.Add(position.Y);
        _vertexData.Add(position.Z);
        _vertexData.Add(texCoord.X);
        _vertexData.Add(texCoord.Y);
        _vertexData.Add(color.X);
        _vertexData.Add(color.Y);
        _vertexData.Add(color.Z);
        _vertexData.Add(color.W);
    }

    private static unsafe void UpdateBuffers()
    {
        var vertexArray = _vertexData.ToArray();
        var indexArray = _indices.ToArray();

        _vbo.Bind();
        fixed (float* vertexPtr = vertexArray)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertexArray.Length * sizeof(float)), vertexPtr, BufferUsageARB.DynamicDraw);
        }
        _vbo.Unbind();

        _ebo.Bind();
        fixed (uint* indexPtr = indexArray)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexArray.Length * sizeof(uint)), indexPtr, BufferUsageARB.DynamicDraw);
        }
        _ebo.Unbind();
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