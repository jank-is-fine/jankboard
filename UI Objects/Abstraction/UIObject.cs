using System.Drawing;
using System.Numerics;

namespace Rendering.UI
{
    /// <summary>
    /// <para>Base class for all UI objects.</para>
    /// <para>All objects that needs to be drawn on screen should inherit from this class</para>
    /// </summary>
    public abstract class UIObject
    {
        public Transform Transform { get; } = new();
        public List<UIObject> ChildObjects { get; set; } = [];

        public Shader? Shader { get; set; } = null!;
        public Texture? Texture { get; set; }
        public virtual Color TextureColor { get; set; } = Color.White;

        public int RenderOrder { get; set; } = 0;
        public virtual long RenderKey { get; set; } = long.MaxValue;

        public bool IsVisible { get; set; } = true;
        public bool IsSelectable { get; set; } = true;
        public bool IsDraggable { get; set; } = true;
        public bool IsScreenSpace = false;
        public bool IsDisposed = false;
        
        public virtual RectangleF Bounds
        {
            get
            {
                var size = Transform.Scale;
                var pos = Transform.Position;
                return new RectangleF(pos.X - size.X * 0.5f, pos.Y - size.Y * 0.5f, size.X, size.Y);
            }
        }

        public virtual bool ContainsPoint(Vector2 point)
        {
            return Bounds.Contains(point.X, point.Y);
        }

        public virtual bool IntersectsWithRect(RectangleF rect)
        {
            return Bounds.IntersectsWith(rect);
        }


        public virtual void RecalcSize() { }
        public virtual void Render() { }
        public virtual void RenderText() { }
        public virtual void OnClick(Vector2 pos) { }
        public virtual void OnDragStart() { }
        public virtual void OnDrag() { }
        public virtual void OnDragEnd() { }
        public virtual void OnHoverStart() { }
        public virtual void OnHoverEnd() { }
        public abstract void Dispose();
    }
}