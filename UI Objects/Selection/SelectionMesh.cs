using System.Drawing;
using System.Numerics;
using Managers;
using Silk.NET.OpenGL;

namespace Rendering.UI
{
    public class SelectionMesh : UIObject
    {
        private List<float> _vertices = [];
        private List<uint> _indices = [];
        private BufferObject<float> _vbo;
        private BufferObject<uint> _ebo;
        private VertexArrayObject<float, uint> _vao;
        private bool _needsUpdate = false;
        private GL gl = ShaderManager.gl;
        private bool IsInScreenSpace = false;

        public override void OnDrag() { }

        public SelectionMesh(bool screenSpace = false)
        {
            Shader = ShaderManager.GetShaderByName("Selection Shader");

            IsInScreenSpace = screenSpace;

            _vbo = new BufferObject<float>(ShaderManager.gl, [], BufferTargetARB.ArrayBuffer);
            _ebo = new BufferObject<uint>(ShaderManager.gl, [], BufferTargetARB.ElementArrayBuffer);
            _vao = new VertexArrayObject<float, uint>(ShaderManager.gl, _vbo, _ebo);

            _vao.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, 4, 0);
            _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 4, 2);
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

                int baseIndex = _vertices.Count / 4;

                _vertices.AddRange([
                xLeft, yTop, 0, 0,
                xRight, yTop, 1, 0,
                xLeft, yBottom, 0, 1,
                xRight, yBottom, 1, 1
            ]);

                _indices.AddRange([
                (uint)baseIndex, (uint)baseIndex + 1, (uint)baseIndex + 2,
                (uint)baseIndex + 1, (uint)baseIndex + 3, (uint)baseIndex + 2
            ]);
            }
        }

        public unsafe override void Render()
        {
            if (_vertices.Count == 0) return;

            if (_needsUpdate)
            {
                _vbo.Bind();
                _vbo.BufferData([.. _vertices]);
                _vbo.Unbind();

                _ebo.Bind();
                _ebo.BufferData([.. _indices]);
                _ebo.Unbind();

                _needsUpdate = false;
            }

            _vao.Bind();
            Shader?.Use();

            Shader?.SetUniform("uModel", Transform.ViewMatrix);

            if (!IsInScreenSpace)
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

            gl.DrawElements(PrimitiveType.Triangles, (uint)_indices.Count,
                           DrawElementsType.UnsignedInt, null);

            _vao.Unbind();
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