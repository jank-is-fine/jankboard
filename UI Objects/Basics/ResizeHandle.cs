using System.Drawing;
using Managers;
using Silk.NET.Input;

namespace Rendering.UI
{
    public class ResizeHandle
    : UIImage
    {
        public int Index = 0;
        public Action<int>? DraggingAction;
        private UIObject Parent;

        public ResizeHandle(UIObject targetParent, int index, bool screenSpace = false) : base(null, screenSpace)
        {
            Parent = targetParent;
            Index = index;

            switch (index)
            {
                case 0:
                    Texture = TextureHandler.GetEmbeddedTextureByName("corner-top-left.png");
                    break;

                case 1:
                    Texture = TextureHandler.GetEmbeddedTextureByName("corner-top-right.png");
                    break;

                case 2:
                    Texture = TextureHandler.GetEmbeddedTextureByName("corner-bottom-left.png");
                    break;

                case 3:
                    Texture = TextureHandler.GetEmbeddedTextureByName("corner-bottom-right.png");
                    break;
            }

            RenderOrder = 2;
            Transform.Scale = new(25f, 25f);
        }

        public override void OnDrag()
        {
            DraggingAction?.Invoke(Index);
            SelectionManager.Deselect(Parent);

            if (InputDeviceHandler.primaryMouse == null) { return; }
            switch (Index)
            {
                case 0:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NwseResize;
                    break;
                case 1:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NeswResize;
                    break;
                case 2:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NeswResize;
                    break;
                case 3:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NwseResize;
                    break;
            }

        }

        public override void OnDragEnd()
        {
            if (InputDeviceHandler.primaryMouse == null) { return; }
            InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.Default;
            AudioHandler.PlaySound("drop_002");
        }

        public override void OnHoverStart()
        {
            //base.OnHoverStart();
            if (InputDeviceHandler.primaryMouse == null) { return; }

            switch (Index)
            {
                case 0:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NwseResize;
                    break;
                case 1:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NeswResize;
                    break;
                case 2:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NeswResize;
                    break;
                case 3:
                    InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.NwseResize;
                    break;
            }

            TextureColor = Settings.HighlightColor;
        }

        public override void OnHoverEnd()
        {
            base.OnHoverEnd();
            if (InputDeviceHandler.primaryMouse == null) { return; }
            InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.Default;
            TextureColor = Color.White;
        }
    }
}