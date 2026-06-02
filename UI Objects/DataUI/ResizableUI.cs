using System.Numerics;

namespace Rendering.UI
{
    public class ResizeableUI : UIObject
    {
        private List<ResizeHandle> ResizingHandles = [];
        public float MinWidth { get; protected set; } = 50;
        public float MinHeight { get; protected set; } = 50;

        protected bool CustomSizeEnforcing = false;

        public virtual Vector2? CustomSizeEnforce(Vector2 target)
        {
            return null;
        }

        public ResizeableUI(bool CustomSizeEnforcingEnabled = false)
        {
            CustomSizeEnforcing = CustomSizeEnforcingEnabled;

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

        public virtual void OnDragResizeHandle(int handleIndex)
        {
            Vector2 currentHandlePosition = ResizingHandles[handleIndex].Transform.Position;

            Vector2? anchorPositionTest = CalculateAnchorPosition(handleIndex);
            if (anchorPositionTest == null) { return; }
            Vector2 anchorPosition = (Vector2)anchorPositionTest;

            Vector2 newSize = CalculateNewSize(currentHandlePosition, anchorPosition);

            newSize = EnforceMinimumSize(newSize);

            if (CustomSizeEnforcing)
            {
                Vector2? size = CustomSizeEnforce(newSize);
                if (size != null)
                {
                    newSize = (Vector2)size;
                }
            }

            UpdateTransform(handleIndex, anchorPosition, newSize);
            UpdateHandlePositions();
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

        public void UpdateHandlePositions()
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
    }
}