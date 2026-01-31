using System.Numerics;
using Managers;

namespace Rendering.UI
{
    public class ImageUI : UIObject
    {
        public ImageData ReferenceImage;
        private List<ResizeHandle> ResizingHandles = [];
        public float MinWidth { get; private set; } = 50;
        public float MinHeight { get; private set; } = 50;
        public override long RenderKey
        {
            get => ReferenceImage.SavedRenderKey;
            set
            {
                ReferenceImage.SavedRenderKey = value;
            }
        }

        public ImageUI(ImageData targetImage)
        {
            ReferenceImage = targetImage;
            Texture = TextureHandler.GetTextureOrCreateBasedOnPath(targetImage.ImagePath);

            if (Texture == null)
            {
                TextureHandler.GetEmbeddedTextureByName("default.png");
            }

            Transform.Scale = targetImage.Size;
            ForceAspectRatio(Transform.Scale);

            ResizingHandles = [
                new(this,0),
                new(this,1),
                new(this,2),
                new(this,3),
            ];

            ResizingHandles[0].DraggingAction += OnDragResizeHandle;
            ResizingHandles[1].DraggingAction += OnDragResizeHandle;
            ResizingHandles[2].DraggingAction += OnDragResizeHandle;
            ResizingHandles[3].DraggingAction += OnDragResizeHandle;

            ChildObjects.AddRange(ResizingHandles);
        }

        public void OnDragResizeHandle(int handleIndex)
        {
            Vector2 currentHandlePosition = ResizingHandles[handleIndex].Transform.Position;

            Vector2? anchorPositionTest = CalculateAnchorPosition(handleIndex);
            if (anchorPositionTest == null) { return; }
            Vector2 anchorPosition = (Vector2)anchorPositionTest;

            Vector2 newSize = CalculateNewSize(currentHandlePosition, anchorPosition);

            newSize = EnforceMinimumSize(newSize);

            newSize = ForceAspectRatio(newSize);

            UpdateTransform(handleIndex, anchorPosition, newSize);
            UpdateHandlePositions();
            UpdateReferenceImage();
            UpdateSpatialPartitioning();
        }

        private Vector2? CalculateAnchorPosition(int handleIndex)
        {
            float centerX = Transform.Position.X;
            float centerY = Transform.Position.Y;
            float halfWidth = Transform.Scale.X / 2;
            float halfHeight = Transform.Scale.Y / 2;

            return handleIndex switch
            {
                0 => (Vector2?)new Vector2(centerX + halfWidth, centerY - halfHeight),
                1 => (Vector2?)new Vector2(centerX - halfWidth, centerY - halfHeight),
                2 => (Vector2?)new Vector2(centerX + halfWidth, centerY + halfHeight),
                3 => (Vector2?)new Vector2(centerX - halfWidth, centerY + halfHeight),
                _ => null,
            };
        }

        private Vector2 CalculateNewSize(Vector2 currentHandlePosition, Vector2 anchorPosition)
        {
            float width = Math.Abs(currentHandlePosition.X - anchorPosition.X);
            float height = Math.Abs(currentHandlePosition.Y - anchorPosition.Y);

            return new Vector2(width, height);
        }

        private Vector2 EnforceMinimumSize(Vector2 size)
        {
            float enforcedWidth = Math.Max(MinWidth, size.X);
            float enforcedHeight = Math.Max(MinHeight, size.Y);

            return new Vector2(enforcedWidth, enforcedHeight);
        }

        private Vector2 ForceAspectRatio(Vector2 newSize)
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

        private void UpdateTransform(int handleIndex, Vector2 anchorPosition, Vector2 newSize)
        {
            (float horizontalDirection, float verticalDirection) = GetResizeDirection(handleIndex);

            float centerX = anchorPosition.X + (horizontalDirection * newSize.X / 2);
            float centerY = anchorPosition.Y + (verticalDirection * newSize.Y / 2);

            Vector2 newCenter = new(centerX, centerY);

            Transform.Scale = newSize;
            Transform.Position = newCenter;
        }

        private (float horizontalDirection, float verticalDirection) GetResizeDirection(int handleIndex)
        {
            return handleIndex switch
            {
                0 => (-1f, 1f),
                1 => (1f, 1f),
                2 => (-1f, -1f),
                3 => (1f, -1f),
                _ => (1f, 1f)
            };
        }


        private void UpdateReferenceImage()
        {
            if (ReferenceImage != null)
            {
                ReferenceImage.position = Transform.Position;
                ReferenceImage.Size = Transform.Scale;
            }
        }

        private void UpdateSpatialPartitioning()
        {
            ChunkManager.RemoveObject(this);
            ChunkManager.AddObject(this);
        }



        private void UpdateHandlePositions()
        {
            ResizingHandles[0].Transform.Position = new
            (
                Transform.Position.X - (Transform.Scale.X / 2),
                Transform.Position.Y + (Transform.Scale.Y / 2)
            );

            ResizingHandles[1].Transform.Position = new
            (
                Transform.Position.X + (Transform.Scale.X / 2),
                Transform.Position.Y + (Transform.Scale.Y / 2)
            );

            ResizingHandles[2].Transform.Position = new
            (
                Transform.Position.X - (Transform.Scale.X / 2),
                Transform.Position.Y - (Transform.Scale.Y / 2)
            );

            ResizingHandles[3].Transform.Position = new
            (
                Transform.Position.X + (Transform.Scale.X / 2),
                Transform.Position.Y - (Transform.Scale.Y / 2)
            );
        }

        public UIObject? HandlerContains(Vector2 worldPos)
        {
            foreach (var handle in ResizingHandles)
            {
                if (handle.ContainsPoint(worldPos))
                {
                    return handle;
                }
            }
            return null;
        }

        public override void OnDrag()
        {
            ReferenceImage.position = Transform.Position;
            RecalcHandlerPos();
        }

        public void RecalcHandlerPos()
        {
            // - ResizingHandles[0].Transform.Scale.X maybe something like so to put it inside or out?
            ResizingHandles[0].Transform.Position = new(Transform.Position.X - Transform.Scale.X / 2, Transform.Position.Y + Transform.Scale.Y / 2);
            ResizingHandles[1].Transform.Position = new(Transform.Position.X + Transform.Scale.X / 2, Transform.Position.Y + Transform.Scale.Y / 2);
            ResizingHandles[2].Transform.Position = Transform.Position - Transform.Scale / 2;
            ResizingHandles[3].Transform.Position = new(Transform.Position.X + Transform.Scale.X / 2, Transform.Position.Y - Transform.Scale.Y / 2);
        }

        public override void OnHoverStart()
        {
            //TextureColor = Color.FromArgb(255 / 2, TextureColor.R, TextureColor.G, TextureColor.B);
        }

        public override void OnHoverEnd()
        {
            //TextureColor = Color.FromArgb(255, TextureColor.R, TextureColor.G, TextureColor.B);
        }


        public override void Dispose()
        {
            foreach (var child in ChildObjects)
            {
                child.Dispose();
            }
            ChildObjects.Clear();

            ResizingHandles[0].DraggingAction -= OnDragResizeHandle;
            ResizingHandles[1].DraggingAction -= OnDragResizeHandle;
            ResizingHandles[2].DraggingAction -= OnDragResizeHandle;
            ResizingHandles[3].DraggingAction -= OnDragResizeHandle;
            IsDisposed = true;
        }

        public override void OnDragEnd()
        {
            ReferenceImage.position = Transform.Position;
        }
    }
}
