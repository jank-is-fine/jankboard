using System.Numerics;
using System.Runtime.InteropServices;
using Managers;
using Silk.NET.OpenGL;

namespace Rendering.UI
{
    public class SelectionMesh : UIObject
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SelectionVertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
        }

        private List<SelectionVertex> _vertices = [];
        private List<uint> _indices = [];
        private BufferObject<SelectionVertex> _vbo;
        private BufferObject<uint> _ebo;
        private VertexArrayObject _vao;
        private bool _needsUpdate = false;

        public SelectionMesh(bool screenSpace = false)
        {
            Shader = ShaderManager.GetShaderByName("Selection Shader");

            _vbo = new BufferObject<SelectionVertex>(ShaderManager.gl, [], BufferTargetARB.ArrayBuffer);
            _ebo = new BufferObject<uint>(ShaderManager.gl, [], BufferTargetARB.ElementArrayBuffer);
            _vao = new VertexArrayObject(ShaderManager.gl);

            _vao.Bind();
            _vbo.Bind();
            _ebo.Bind();

            int stride = Marshal.SizeOf<SelectionVertex>();
            _vao.SetVertexAttribute<SelectionVertex>(0, 3, VertexAttribPointerType.Float, stride, 0);
            _vao.SetVertexAttribute<SelectionVertex>(1, 2, VertexAttribPointerType.Float, stride,
                Marshal.OffsetOf<SelectionVertex>(nameof(SelectionVertex.TexCoord)).ToInt32());

            _vao.Unbind();
        }

        public void UpdateSelection(string[] lines, int selectionStart, int selectionEnd,
                                   Vector2 localTextStartPos, float lineHeight)
        {
            _vertices.Clear();
            _indices.Clear();
            GenerateSelectionMesh(lines, selectionStart, selectionEnd, localTextStartPos, lineHeight);
            _needsUpdate = true;
        }

        private void GenerateSelectionMesh(string[] lines, int selectionStart, int selectionEnd,
                                           Vector2 localTextStartPos, float lineHeight)
        {
            var lineBounds = CalculatePerLineBounds(lines, selectionStart, selectionEnd);

            foreach (var bound in lineBounds)
            {
                float yTop = localTextStartPos.Y - (bound.LineIndex * lineHeight);
                float yBottom = yTop - lineHeight;
                float xLeft = localTextStartPos.X + bound.StartX;
                float xRight = localTextStartPos.X + bound.EndX;

                int baseIndex = _vertices.Count;

                _vertices.Add(new SelectionVertex { Position = new Vector3(xLeft, yTop, 0), TexCoord = new Vector2(0, 0) });
                _vertices.Add(new SelectionVertex { Position = new Vector3(xRight, yTop, 0), TexCoord = new Vector2(1, 0) });
                _vertices.Add(new SelectionVertex { Position = new Vector3(xLeft, yBottom, 0), TexCoord = new Vector2(0, 1) });
                _vertices.Add(new SelectionVertex { Position = new Vector3(xRight, yBottom, 0), TexCoord = new Vector2(1, 1) });

                _indices.Add((uint)baseIndex);
                _indices.Add((uint)baseIndex + 1);
                _indices.Add((uint)baseIndex + 2);
                _indices.Add((uint)baseIndex + 1);
                _indices.Add((uint)baseIndex + 3);
                _indices.Add((uint)baseIndex + 2);
            }
        }

        private List<LineBound> CalculatePerLineBounds(string[] lines, int selectionStart, int selectionEnd)
        {
            var bounds = new List<LineBound>();
            int charsProcessed = 0;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                int lineStart = charsProcessed;
                int lineEnd = charsProcessed + line.Length;

                if (selectionEnd > lineStart && selectionStart < lineEnd)
                {
                    int lineSelectionStart = Math.Max(0, selectionStart - lineStart);
                    int lineSelectionEnd = Math.Min(line.Length, selectionEnd - lineStart);

                    if (lineSelectionStart < lineSelectionEnd)
                    {
                        float startX = TextHelper.GetStringLineRenderBounds(line[..lineSelectionStart], FontType.REGULAR, Settings.TextSize).Width;
                        float endX = TextHelper.GetStringLineRenderBounds(line[..lineSelectionEnd], FontType.REGULAR, Settings.TextSize).Width;
                        bounds.Add(new LineBound(lineIndex, startX, endX));
                    }
                }
                charsProcessed += line.Length + 1;
            }
            return bounds;
        }

        public unsafe override void Render()
        {
            if (_vertices.Count == 0) return;

            if (_needsUpdate)
            {
                _vbo.BufferData(CollectionsMarshal.AsSpan(_vertices));
                _ebo.BufferData(CollectionsMarshal.AsSpan(_indices));
                _needsUpdate = false;
            }

            _vao.Bind();
            Shader?.Use();

            Shader?.SetUniform("uModel", Transform.ViewMatrix);
            if (!IsScreenSpace)
            {
                Shader?.SetUniform("uView", Camera.GetViewMatrix());
                Shader?.SetUniform("uProjection", Camera.GetProjectionMatrix());
            }
            else
            {
                Shader?.SetUniform("uView", Camera.GetStationalViewMatrix());
                Shader?.SetUniform("uProjection", Camera.GetStationalProjectionMatrix());
            }
            Shader?.SetUniform("uColor", Settings.ColorToVec4(Settings.SelectionMeshColor));

            ShaderManager.gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Count, DrawElementsType.UnsignedInt, null);
            _vao.Unbind();
        }

        public override void Dispose()
        {
            _vao?.Dispose();
            _ebo?.Dispose();
            _vbo?.Dispose();
        }
    }

    public struct LineBound(int lineIndex, float startX, float endX)
    {
        public int LineIndex { get; } = lineIndex;
        public float StartX { get; } = startX;
        public float EndX { get; } = endX;
    }
}