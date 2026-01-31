using System.Numerics;
using Managers;
using Silk.NET.OpenGL;

namespace Rendering.UI
{
    public class MarqueeUI : UIObject
    {
        private readonly VertexArrayObject<float, uint> _vao;
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        private GL gl = ShaderManager.gl;

        public MarqueeUI()
        {
            Shader = ShaderManager.GetShaderByName("Default Shader");
            TextureColor = Settings.MarqueeColor;
            _vao = new VertexArrayObject<float, uint>(ShaderManager.gl, ShaderManager.Vbo, ShaderManager.Ebo);

            _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);

            IsSelectable = false;
            RenderOrder = 10000;
            IsVisible = false;

            Texture = TextureHandler.GetEmbeddedTextureByName("default.png");
        }

        public override void Render()
        {
            if (!IsVisible) return;

            float width = Math.Max(Math.Abs(End.X - Start.X), 1.0f);
            float height = Math.Max(Math.Abs(End.Y - Start.Y), 1.0f);
            Vector2 center = (Start + End) * 0.5f;

            Transform.Position = center;
            Transform.Scale = new Vector2(width, height);

            _vao.Bind();
            Shader?.Use();
            Texture?.Bind();
            Shader?.SetUniform("uModel", Transform.ViewMatrix);
            Shader?.SetUniform("uView", Camera.GetViewMatrix());
            Shader?.SetUniform("uProjection", Camera.GetProjectionMatrix());
            Shader?.SetUniform("uColor",  Settings.ColorToVec4(Settings.MarqueeColor));

            gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        }

        public new bool ContainsPoint(Vector2 worldPoint) => false;
        public override void OnClick(Vector2 pos) { }

        public override void Dispose()
        {
            _vao?.Dispose();
        }

        public override void OnDragStart()
        {
        }

        public override void OnDragEnd()
        {
        }
    }
}