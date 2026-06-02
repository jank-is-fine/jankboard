using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Managers;
using Rendering.UI;
using Silk.NET.OpenGL;

public class ConnectionRenderBatch
{
    private VertexArrayObject _batchVao = null!;
    private BufferObject<ColoredVertex> _batchVbo = null!;
    private BufferObject<uint> _batchEbo = null!;
    private List<ColoredVertex> _vertexData = [];
    private List<uint> _indices = [];
    private Shader _batchShader;
    private GL _gl = ShaderManager.gl;
    private bool _isBatchInitialized = false;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ColoredVertex
    {
        public Vector3 Position;
        public Vector4 Color;
    }

    public ConnectionRenderBatch(Shader batchShader)
    {
        _batchShader = batchShader;

        _batchVbo = new BufferObject<ColoredVertex>(_gl, [], BufferTargetARB.ArrayBuffer);
        _batchEbo = new BufferObject<uint>(_gl, [], BufferTargetARB.ElementArrayBuffer);
        _batchVao = new VertexArrayObject(_gl);

        _batchVao.Bind();
        _batchVbo.Bind();
        _batchEbo.Bind();

        int stride = Marshal.SizeOf<ColoredVertex>();

        _batchVao.SetVertexAttribute<ColoredVertex>(0, 3, VertexAttribPointerType.Float, stride, 0);
        _batchVao.SetVertexAttribute<ColoredVertex>(1, 4, VertexAttribPointerType.Float, stride, Marshal.OffsetOf<ColoredVertex>(nameof(ColoredVertex.Color)).ToInt32());

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

        uint vertexOffset = (uint)_vertexData.Count;

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
            color = SelectionManager.IsObjectSelected(connection)
                ? Settings.ColorToVec4(Settings.HighlightColor)
                : Settings.ColorToVec4(GetConnectionColor(arrowType));
        }

        var vertices = connection.GetBatchVertices();
        var indices = connection.GetBatchIndices();

        if (vertices == null || indices == null) return;

        foreach (var pos in vertices)
        {
            _vertexData.Add(new ColoredVertex
            {
                Position = pos,
                Color = color
            });
        }

        foreach (var idx in indices)
        {
            _indices.Add(idx + vertexOffset);
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
        if (_vertexData.Count == 0) return;

        _batchVbo.BufferData(CollectionsMarshal.AsSpan(_vertexData));
        _batchEbo.BufferData(CollectionsMarshal.AsSpan(_indices));
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
    }

    public void Dispose()
    {
        _batchVao?.Dispose();
        _batchVbo?.Dispose();
        _batchEbo?.Dispose();
    }
}