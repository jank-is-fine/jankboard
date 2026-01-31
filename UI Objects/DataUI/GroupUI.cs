using System.Drawing;
using System.Numerics;
using Managers;

namespace Rendering.UI
{
    public class GroupUI : UIObject
    {
        public Group ReferenceGroup;
        ParsedText ParsedText;
        private List<ResizeHandle> ResizingHandles = [];
        public float MinWidth { get; private set; } = 50;
        public float MinHeight { get; private set; } = 20;
        public override Color TextureColor => Settings.GroupBGColor;
        public bool _isEditing { get; private set; } = false;
        public override long RenderKey
        {
            get => ReferenceGroup.SavedRenderKey;
            set
            {
                ReferenceGroup.SavedRenderKey = value;
            }
        }


        public GroupUI(Group targetGroup)
        {
            ReferenceGroup = targetGroup;

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
            ParsedText = TextFormatParser.ParseText(ReferenceGroup.GroupName);
        }

        public void OnDragResizeHandle(int handleIndex)
        {
            Vector2 currentHandlePosition = ResizingHandles[handleIndex].Transform.Position;

            Vector2? anchorPositionTest = CalculateAnchorPosition(handleIndex);
            if (anchorPositionTest == null) { return; }
            Vector2 anchorPosition = (Vector2)anchorPositionTest;

            Vector2 newSize = CalculateNewSize(currentHandlePosition, anchorPosition);

            newSize = EnforceMinimumSize(newSize);

            UpdateTransform(handleIndex, anchorPosition, newSize);
            RecalcHandlerPos();
            UpdateReferenceGroup();
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

        public void RecalcMinSize()
        {
            var textBoxSize = TextHelper.GetStringRenderBox(ParsedText);
            MinWidth = Math.Max(MinWidth, textBoxSize.Width);
            MinHeight = Math.Max(MinHeight, textBoxSize.Height);

            //FIXME buggy so i had to use magic numbers here 
            //The issue  it is because of the nine-slice border?
            //TODO fix in the batcher 
            MinWidth += 35f;
            MinHeight += 35f;
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

        private void UpdateReferenceGroup()
        {
            if (ReferenceGroup != null)
            {
                ReferenceGroup.position = Transform.Position;
                ReferenceGroup.Size = Transform.Scale;
            }
        }

        private void UpdateSpatialPartitioning()
        {
            ChunkManager.RemoveObject(this);
            ChunkManager.AddObject(this);
        }

        public override void OnDrag()
        {
            ReferenceGroup.position = Transform.Position;
            RecalcHandlerPos();
        }

        public void RecalcHandlerPos()
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

        public override void RenderText()
        {
            if (_isEditing) { return; }
            // Calculate text position
            Vector2 textPos = new(
                Transform.Position.X - (Transform.Scale.X / 2f) + 17,
                Transform.Position.Y + (Transform.Scale.Y / 2f) - 16
            );


            TextRenderer.RenderTextWorldParsed(
                ParsedText,
                textPos,
                Settings.ColorToVec4(Settings.GroupTextColor),
                Settings.TextSize
            );

        }

        public void StartEditing()
        {
            _isEditing = true;
            AudioHandler.PlaySound("scratch_001");
        }

        public void EndEdit(string? targetContent = null)
        {
            if (targetContent != null)
            {
                ParsedText = TextFormatParser.ParseText(targetContent);
                ReferenceGroup.GroupName = targetContent;
                RecalcMinSize();
            }
            _isEditing = false;
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
