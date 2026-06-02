using System.Numerics;
using Managers;
using Silk.NET.OpenGL;

namespace Rendering.UI
{
    public class MarqueeUI : UIImage
    {
        public Vector2 Start { get; set; }
        public Vector2 End { get; set; }
        private GL gl = ShaderManager.gl;

        public MarqueeUI()
        {
            Shader = ShaderManager.GetShaderByName("Default Shader");
            TextureColor = Settings.MarqueeColor;

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

            base.Render();
        }

        public new bool ContainsPoint(Vector2 worldPoint) => false;
        public override void OnClick(Vector2 pos) { }

        public override void OnDragStart()
        {
        }

        public override void OnDragEnd()
        {
        }
    }
}