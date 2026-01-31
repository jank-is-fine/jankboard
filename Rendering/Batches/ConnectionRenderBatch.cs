using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.OpenGL;

/// <summary>
/// <para>Specialized <see cref="RenderBatch"/> class for connection/arrow geometry</para>
/// </summary>

public class ConnectionRenderBatch
{
    private VertexArrayObject<float, uint> _batchVao = null!;
    private BufferObject<float> _batchVbo = null!;
    private BufferObject<uint> _batchEbo = null!;
    private List<float> _vertexData = [];
    private List<uint> _indices = [];
    private Shader _batchShader;
    private GL _gl = ShaderManager.gl;
    private bool _isBatchInitialized = false;

    public ConnectionRenderBatch(Shader batchShader)
    {
        _batchShader = batchShader;

        _batchVbo = new BufferObject<float>(_gl, [], BufferTargetARB.ArrayBuffer);
        _batchEbo = new BufferObject<uint>(_gl, [], BufferTargetARB.ElementArrayBuffer);
        _batchVao = new VertexArrayObject<float, uint>(_gl, _batchVbo, _batchEbo);

        // Vertex layout for connections: position(3), color(4)
        _batchVao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 7, 0);   // position (x, y, z)
        _batchVao.VertexAttributePointer(1, 4, VertexAttribPointerType.Float, 7, 3);   // color (r, g, b, a)
        _batchVao.Unbind();

        _isBatchInitialized = true;
    }

    public void AddConnectionsToBatch(List<ConnectionUI> targetConnections, bool clearPreviousBatch = true)
    {
        if (!_isBatchInitialized) return;

        if (clearPreviousBatch)
        {
            _vertexData.Clear();
            _indices.Clear();
        }

        uint vertexOffset = 0;

        foreach (var connection in targetConnections)
        {
            if (connection == null || !connection.IsVisible || !connection.RenderReady)
                continue;

            AddConnectionToBatch(connection, ref vertexOffset);
        }
    }

    private void AddConnectionToBatch(ConnectionUI connection, ref uint vertexOffset)
    {
        var arrowType = connection.ReferenceConnection.arrowType;

        Vector4 color;
        if (UIobjectHandler.CurrentHoeverTarget == connection)
        {
            color = Settings.ColorToVec4(Settings.HighlightColor);
        }
        else
        {
            color = SelectionManager.IsObjectSelected(connection) ? Settings.ColorToVec4(Settings.HighlightColor)
                                             : Settings.ColorToVec4(GetConnectionColor(arrowType));
        }

        var vertices = connection.GetBatchVertices();
        var indices = connection.GetBatchIndices();

        if (vertices == null || indices == null) return;

        foreach (var vertex in vertices)
        {
            _vertexData.Add(vertex.X);
            _vertexData.Add(vertex.Y);
            _vertexData.Add(vertex.Z);
            _vertexData.Add(color.X);
            _vertexData.Add(color.Y);
            _vertexData.Add(color.Z);
            _vertexData.Add(color.W);
        }

        foreach (var index in indices)
        {
            _indices.Add(index + vertexOffset);
        }

        vertexOffset += (uint)vertices.Length;
    }

    public Color GetConnectionColor(ArrowType arrowType)
    {
        return arrowType switch
        {
            ArrowType.Loose => Settings.ConnectionLooseColor,
            ArrowType.LooseDiagonal => Settings.ConnectionDiagonalSlashedColor,
            _ => Settings.ConnectionDirectColor,
        };
    }

    public void UpdateBuffers()
    {
        if (_vertexData.Count > 0)
        {
            _batchVbo.BufferData(_vertexData.ToArray());
            _batchEbo.BufferData(_indices.ToArray());
        }
    }

    public void ExecuteBatch()
    {
        UpdateBuffers();
        DrawBatch();
    }

    private unsafe void DrawBatch()
    {
        if (!_isBatchInitialized || _vertexData.Count == 0) return;

        _batchShader.Use();
        _batchShader.SetUniform("uView", Camera.GetViewMatrix());
        _batchShader.SetUniform("uProjection", Camera.GetProjectionMatrix());

        _batchVao.Bind();
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Count, DrawElementsType.UnsignedInt, null);
        _batchVao.Unbind();

        var error = _gl.GetError();
        if (error != GLEnum.NoError)
        {
            //Logging every frame would create a huge log file - so no logging for now
            Console.WriteLine($"OpenGL Error after batch drawing connections: {error}");
        }
    }

    public void Dispose()
    {
        _batchVao?.Dispose();
        _batchVbo?.Dispose();
        _batchEbo?.Dispose();
    }
}