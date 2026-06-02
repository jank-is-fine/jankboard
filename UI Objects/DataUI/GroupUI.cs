using System.Drawing;
using System.Numerics;

namespace Rendering.UI
{
    public class GroupUI : ResizeableUI
    {
        public Group ReferenceGroup;
        ParsedText ParsedText;
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

        public GroupUI(Group targetGroup) : base(CustomSizeEnforcingEnabled: false)
        {
            ReferenceGroup = targetGroup;
            ParsedText = TextFormatParser.ParseText(ReferenceGroup.GroupName);
        }

        public override void OnDragResizeHandle(int handleIndex)
        {
            base.OnDragResizeHandle(handleIndex);
            UpdateReferenceGroup();
        }

        public void RecalculateMinScaleSize()
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

        private void UpdateReferenceGroup()
        {
            if (ReferenceGroup != null)
            {
                ReferenceGroup.position = Transform.Position;
                ReferenceGroup.Size = Transform.Scale;
            }
        }

        public override void OnDrag()
        {
            ReferenceGroup.position = Transform.Position;
            UpdateHandlePositions();
        }

        public override void RenderText()
        {
            if (_isEditing) { return; }


            Vector2 textPos = new(
                Transform.Position.X - (Transform.Scale.X / 2f) + 18,
                Transform.Position.Y + (Transform.Scale.Y / 2f) - 14
            );

           TextRenderer.RenderTextParsed
           (
               textStyle: ParsedText,
               position: textPos,
               baseColor: Settings.ColorToVec4(Settings.GroupTextColor),
               sourceWidth: Transform.Scale.X - 36,
               WorldSpace: true
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
                RecalculateMinScaleSize();
                OnDragResizeHandle(3);
            }
            _isEditing = false;
        }
    }
}
