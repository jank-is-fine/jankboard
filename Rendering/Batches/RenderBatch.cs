using System.Numerics;
using System.Runtime.InteropServices;
using Managers;
using Rendering.UI;
using Silk.NET.OpenGL;

/// <summary>
/// <para>Basic batch rendering system that combines multiple UI objects into a single draw call</para>
/// </summary>

public class RenderBatch
{
    private GL _gl = ShaderManager.gl;
    private Shader _batchShader;
    private Texture? _texture;
    private Texture? _defaultTexture = TextureHandler.GetEmbeddedTextureByName("default.png");
    private List<UIObject> _textRenderTargetObjects = [];

    private static readonly Vector2[] QuadPositions =
    [
        new(0, 0),
        new(1, 0),
        new(1, 1),
        new(0, 1)
    ];
    private static readonly Vector2[] QuadUVs =
    [
        new(0, 1),
        new(1, 1),
        new(1, 0),
        new(0, 0)
    ];
    private static readonly uint[] QuadIndices = [0, 1, 2, 0, 2, 3];

    private BufferObject<Vector2> _quadPosVBO = null!;
    private BufferObject<Vector2> _quadUvVBO = null!;
    private BufferObject<uint> _quadEBO = null!;
    private BufferObject<UIObjectInstance> _instanceVBO = null!;
    private VertexArrayObject _vao = null!;

    private List<UIObjectInstance> _instances = [];

    public Vector2 NineSliceBorder { get; set; } = Vector2.Zero;
    public Texture? texture
    {
        get => _texture;
        set => _texture = value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct UIObjectInstance
    {
        public Vector2 Position;
        public Vector2 Scale;
        public Vector4 Color;
        public Vector2 Border;
    }

    public RenderBatch(Shader batchShader, Texture? tex = null)
    {
        _batchShader = batchShader;
        _texture = tex;

        _quadPosVBO = new BufferObject<Vector2>(_gl, QuadPositions, BufferTargetARB.ArrayBuffer);
        _quadUvVBO = new BufferObject<Vector2>(_gl, QuadUVs, BufferTargetARB.ArrayBuffer);
        _quadEBO = new BufferObject<uint>(_gl, QuadIndices, BufferTargetARB.ElementArrayBuffer);
        _instanceVBO = new BufferObject<UIObjectInstance>(_gl, [], BufferTargetARB.ArrayBuffer);

        _vao = new VertexArrayObject(_gl);
        _vao.Bind();

        _quadPosVBO.Bind();
        _vao.SetVertexAttribute<Vector2>(0, 2, VertexAttribPointerType.Float, 0, 0);

        _quadUvVBO.Bind();
        _vao.SetVertexAttribute<Vector2>(1, 2, VertexAttribPointerType.Float, 0, 0);

        _instanceVBO.Bind();
        int instanceStride = Marshal.SizeOf<UIObjectInstance>();

        // Position (vec2)
        _vao.SetVertexAttribute<UIObjectInstance>(2, 2, VertexAttribPointerType.Float, instanceStride, 0);
        _vao.VertexAttribDivisor(2, 1);

        // Scale (vec2)
        _vao.SetVertexAttribute<UIObjectInstance>(3, 2, VertexAttribPointerType.Float, instanceStride,
            Marshal.OffsetOf<UIObjectInstance>(nameof(UIObjectInstance.Scale)).ToInt32());
        _vao.VertexAttribDivisor(3, 1);

        // Color (vec4)
        _vao.SetVertexAttribute<UIObjectInstance>(4, 4, VertexAttribPointerType.Float, instanceStride,
            Marshal.OffsetOf<UIObjectInstance>(nameof(UIObjectInstance.Color)).ToInt32());
        _vao.VertexAttribDivisor(4, 1);

        // Border (vec2)
        _vao.SetVertexAttribute<UIObjectInstance>(5, 2, VertexAttribPointerType.Float, instanceStride,
            Marshal.OffsetOf<UIObjectInstance>(nameof(UIObjectInstance.Border)).ToInt32());
        _vao.VertexAttribDivisor(5, 1);

        _vao.Unbind();
    }

    public void AddObjectsToBatch(IEnumerable<UIObject> targetObjects, bool clearPreviousBatch = true)
    {
        if (clearPreviousBatch)
        {
            _instances.Clear();
            _textRenderTargetObjects.Clear();
        }
        _textRenderTargetObjects.AddRange(targetObjects);

        foreach (var obj in targetObjects)
        {
            if (obj == null || !obj.IsVisible) continue;
            AddObjectToBatch(obj);
        }
    }

    private void AddObjectToBatch(UIObject obj)
    {
        Vector2 pos = obj.Transform.Position;
        Vector2 scale = obj.Transform.Scale;
        Vector4 color = Settings.ColorToVec4(obj.TextureColor);

        _instances.Add(new UIObjectInstance
        {
            Position = pos,
            Scale = scale,
            Color = color,
            Border = NineSliceBorder
        });
    }

    private void UpdateInstanceBuffer()
    {
        if (_instances.Count == 0) return;
        _instanceVBO.BufferData(CollectionsMarshal.AsSpan(_instances));
    }

    public void ExecuteBatch()
    {
        UpdateInstanceBuffer();
        DrawBatch();
        RenderText();
    }

    private void RenderText()
    {
        TextRenderer.Clear();
        foreach (var obj in _textRenderTargetObjects)
        {
            obj.RenderText();
        }
        TextRenderer.Draw();
        TextRenderer.Clear();
    }

    private unsafe void DrawBatch()
    {
        if (_instances.Count == 0) return;

        _batchShader.Use();

        _batchShader.SetUniform("uProjection", Camera.GetProjectionMatrix());
        _batchShader.SetUniform("uView", Camera.GetViewMatrix());
        _batchShader.SetUniform("uTexture0", 0);

        var tex = _texture ?? _defaultTexture;
        tex?.Bind();

        _vao.Bind();
        _quadEBO.Bind();
        _gl.DrawElementsInstanced(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null, (uint)_instances.Count);
        _vao.Unbind();
    }

    public void Dispose()
    {
        _quadPosVBO?.Dispose();
        _quadUvVBO?.Dispose();
        _quadEBO?.Dispose();
        _instanceVBO?.Dispose();
        _vao?.Dispose();
    }
}