using System.Numerics;
using Managers;
using Silk.NET.OpenGL;

namespace Rendering.UI
{
    public class UIImage : UIObject
    {
        public VertexArrayObject<float, uint> _vao;
        private readonly GL _gl;
        protected GL Gl => _gl;
        public bool _nineSlice { get; private set; }
        public Vector2 _nineSliceBorder { get; private set; } = new(32, 16);
        public Action? DragStartAction;
        public Action? DragAction;
        public Action? DragEndAction;

        public UIImage(Texture? texture = null, bool screenSpace = false, bool nineSlice = false, Vector2? NineSliceBorder = null)
        {
            _gl = ShaderManager.gl;

            if (nineSlice)
            {
                Shader = ShaderManager.GetShaderByName("nine-slice");
                _nineSlice = true;
                _nineSliceBorder = NineSliceBorder ?? new(37, 16);
            }
            else
            {
                Shader = ShaderManager.GetShaderByName("Default Shader");
            }

            if (texture == null && !nineSlice)
            {
                Texture = TextureHandler.GetEmbeddedTextureByName("default.png");
            }
            else if (texture == null && nineSlice)
            {
                Texture = TextureHandler.GetEmbeddedTextureByName("button_rectangle_line.png");
            }
            else
            {
                Texture = texture;
            }

            _vao = new VertexArrayObject<float, uint>(_gl, ShaderManager.Vbo, ShaderManager.Ebo);
            _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            IsScreenSpace = screenSpace;
        }

        public override void Render()
        {
            if (!IsVisible) return;

            _vao.Bind();
            Shader?.Use();

            Shader?.SetUniform("uTexture0", 0);
            if (IsScreenSpace)
            {
                Shader?.SetUniform("uView", Camera.GetStationalViewMatrix());
                Shader?.SetUniform("uProjection", Camera.GetStationalProjectionMatrix());
            }
            else
            {
                Shader?.SetUniform("uView", Camera.GetViewMatrix());
                Shader?.SetUniform("uProjection", Camera.GetProjectionMatrix());
            }
            Shader?.SetUniform("uModel", Transform.ViewMatrix);
            Shader?.SetUniform("uColor", Settings.ColorToVec4(TextureColor));

            if (_nineSlice)
            {
                Shader?.SetUniform("uDimensions", Transform.Scale);
                Shader?.SetUniform("uBorderSize", _nineSliceBorder);
            }

            Texture?.Bind();

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            // Check for OpenGL errors
            var error = _gl.GetError();
            if (error != GLEnum.NoError)
            {
                // No logging here, to prevent huge log files
                //Console.WriteLine($"OpenGL Error after drawing: {error}");
            }
        }

        public override void Dispose()
        {
            _vao.Dispose();
            IsDisposed = true;
            IsVisible = false;
        }

        public override void OnDragStart()
        {
            DragStartAction?.Invoke();
        }

        public override void OnDrag()
        {
            DragAction?.Invoke();
        }

        public override void OnDragEnd()
        {
            DragEndAction?.Invoke();
        }
    }
}