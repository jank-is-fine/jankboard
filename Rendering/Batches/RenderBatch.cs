using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.OpenGL;

/// <summary>
/// <para>Basic batch rendering system that combines multiple UI objects into a single draw call</para>
/// </summary>

public class RenderBatch
{
    private VertexArrayObject<float, uint> _batchVao = null!;
    private BufferObject<float> _batchVbo = null!;
    private BufferObject<uint> _batchEbo = null!;
    private List<float> _vertexData = [];
    private List<uint> _indices = [];
    private Shader _batchShader;
    private GL _gl = ShaderManager.gl;
    public Texture? texture { get; set; } = null;
    private bool _isBatchInitialized = false;
    public Vector2 NineSliceBorder { get; set; } = Vector2.Zero;
    private List<UIObject> TextRenderTargetObjects = [];
    private Texture? defaultTexture = TextureHandler.GetEmbeddedTextureByName("default.png");

    public RenderBatch(Shader BatchShader, Texture? tex = null)
    {
        texture = tex;
        _batchShader = BatchShader;

        _batchVbo = new BufferObject<float>(_gl, [], BufferTargetARB.ArrayBuffer);
        _batchEbo = new BufferObject<uint>(_gl, [], BufferTargetARB.ElementArrayBuffer);
        _batchVao = new VertexArrayObject<float, uint>(_gl, _batchVbo, _batchEbo);

        // Vertex layout: position(3), texcoord(2), color(4), dimensions(2), border(2)
        _batchVao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 13, 0);   // position
        _batchVao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 13, 3);   // texcoord
        _batchVao.VertexAttributePointer(2, 4, VertexAttribPointerType.Float, 13, 5);   // color
        _batchVao.VertexAttributePointer(3, 2, VertexAttribPointerType.Float, 13, 9);   // dimensions
        _batchVao.VertexAttributePointer(4, 2, VertexAttribPointerType.Float, 13, 11);  // border
        _batchVao.Unbind();

        _isBatchInitialized = true;
    }


    public void AddObjectsToBatch(List<UIObject> targetObjects, bool ClearPreviousBatch = true)
    {
        if (!_isBatchInitialized) return;

        if (ClearPreviousBatch)
        {
            _vertexData.Clear();
            _indices.Clear();
            TextRenderTargetObjects.Clear();
            TextRenderTargetObjects.AddRange(targetObjects);
        }

        uint vertexOffset = 0;

        foreach (var obj in targetObjects)
        {
            if (obj == null || !obj.IsVisible)
                continue;

            AddObjectToBatch(obj, ref vertexOffset);
            TextRenderTargetObjects.Add(obj);
        }
    }

    public void UpdateBuffers()
    {
        if (_vertexData.Count > 0)
        {
            _batchVbo.BufferData(_vertexData.ToArray());
            _batchVbo.Unbind();

            _batchEbo.BufferData(_indices.ToArray());
            _batchEbo.Unbind();
        }
    }

    private void AddObjectToBatch(UIObject TargetObject, ref uint vertexOffset)
    {
        Vector2 pos = TargetObject.Transform.Position;
        Vector2 scale = TargetObject.Transform.Scale;
        Vector2 border = NineSliceBorder;

        Vector4 color = Settings.ColorToVec4(TargetObject.TextureColor);

        // Calculate the quad vertices
        float left = pos.X - scale.X / 2;
        float right = pos.X + scale.X / 2;
        float top = pos.Y + scale.Y / 2;
        float bottom = pos.Y - scale.Y / 2;

        // Create vertices for a simple quad
        AddVertex(new Vector3(left, top, 0), new Vector2(0, 0), color, scale, border);
        AddVertex(new Vector3(right, top, 0), new Vector2(1, 0), color, scale, border);
        AddVertex(new Vector3(right, bottom, 0), new Vector2(1, 1), color, scale, border);
        AddVertex(new Vector3(left, bottom, 0), new Vector2(0, 1), color, scale, border);

        // Add indices - 2 triangles
        _indices.Add(vertexOffset + 0);
        _indices.Add(vertexOffset + 1);
        _indices.Add(vertexOffset + 2);
        _indices.Add(vertexOffset + 0);
        _indices.Add(vertexOffset + 2);
        _indices.Add(vertexOffset + 3);

        vertexOffset += 4;
    }

    private void AddVertex(Vector3 position, Vector2 texCoord, Vector4 color, Vector2 dimensions, Vector2 border)
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
        _vertexData.Add(dimensions.X);
        _vertexData.Add(dimensions.Y);
        _vertexData.Add(border.X);
        _vertexData.Add(border.Y);
    }

    private void RenderText()
    {
        TextRenderer.Clear();
        foreach (var obj in TextRenderTargetObjects)
        {
            obj.RenderText();
        }
        TextRenderer.Draw();
        TextRenderer.Clear();
    }

    public void ExecuteBatch()
    {
        UpdateBuffers();
        DrawBatch();
        RenderText();
    }

    private unsafe void DrawBatch()
    {
        if (!_isBatchInitialized || _vertexData.Count == 0) { return; }

        _batchShader?.Use();

        _batchShader?.SetUniform("uTexture0", 0);
        _batchShader?.SetUniform("uView", Camera.GetViewMatrix());
        _batchShader?.SetUniform("uProjection", Camera.GetProjectionMatrix());

        if (texture == null)
        {
            defaultTexture?.Bind();
        }
        else
        {
            texture?.Bind();
        }


        _batchVao?.Bind();
        _gl?.DrawElements(PrimitiveType.Triangles, (uint)_indices.Count, DrawElementsType.UnsignedInt, null);
        _batchVao?.Unbind();

        var error = _gl?.GetError();
        if (error != GLEnum.NoError)
        {
            //Logging every frame would create a huge log file - so no logging for now
            Console.WriteLine($"OpenGL Error after batch drawing: {error}");
        }
    }

    public void Dispose()
    {
        _batchVao?.Dispose();
        _batchVbo?.Dispose();
        _batchEbo?.Dispose();
    }
}