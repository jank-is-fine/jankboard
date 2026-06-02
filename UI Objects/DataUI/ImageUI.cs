using System.Numerics;

namespace Rendering.UI
{
    public class ImageUI : ResizeableUI
    {
        public ImageData ReferenceImage;

        public override long RenderKey
        {
            get => ReferenceImage.SavedRenderKey;
            set
            {
                ReferenceImage.SavedRenderKey = value;
            }
        }

        public ImageUI(ImageData targetImage) : base(CustomSizeEnforcingEnabled: true)
        {
            ReferenceImage = targetImage;
            Texture = TextureHandler.GetTextureOrCreateBasedOnPath(targetImage.ImagePath);

            if (Texture == null)
            {
                TextureHandler.GetEmbeddedTextureByName("default.png");
            }

            Transform.Scale = targetImage.Size;
        }

        public override Vector2? CustomSizeEnforce(Vector2 newSize)
        {
            if (Texture == null || Texture.Width == 0 || Texture.Height == 0)
                return newSize;

            float imageAspectRatio = (float)Texture.Width / Texture.Height;

            float heightBasedOnWidth = newSize.X / imageAspectRatio;
            float widthBasedOnHeight = newSize.Y * imageAspectRatio;

            float widthDifference = Math.Abs(newSize.X - widthBasedOnHeight);
            float heightDifference = Math.Abs(newSize.Y - heightBasedOnWidth);

            bool shouldLockWidth = widthDifference < heightDifference;

            Vector2 aspectCorrectedSize = newSize;

            if (shouldLockWidth)
            {
                aspectCorrectedSize.Y = heightBasedOnWidth;
            }
            else
            {
                aspectCorrectedSize.X = widthBasedOnHeight;
            }

            return aspectCorrectedSize;
        }

        public override void OnDragResizeHandle(int handleIndex)
        {
            base.OnDragResizeHandle(handleIndex);
            UpdateReferenceImage();
        }

        private void UpdateReferenceImage()
        {
            if (ReferenceImage != null)
            {
                ReferenceImage.position = Transform.Position;
                ReferenceImage.Size = Transform.Scale;
            }
        }

        public override void OnDrag()
        {
            ReferenceImage.position = Transform.Position;
            UpdateHandlePositions();
        }


        public override void OnDragEnd()
        {
            ReferenceImage.position = Transform.Position;
            UpdateHandlePositions();
        }
    }
}
