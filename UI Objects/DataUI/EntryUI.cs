using System.Drawing;
using System.Numerics;

namespace Rendering.UI
{
    public class EntryUI : ResizeableUI
    {
        public Entry ReferenceEntry;
        ParsedText ParsedText;
        public bool _isEditing { get; private set; } = false;
        public override Color TextureColor => GetBackgroundColor();
        public override long RenderKey
        {
            get => ReferenceEntry.SavedRenderKey;
            set
            {
                ReferenceEntry.SavedRenderKey = value;
            }
        }

        public EntryUI(Entry targetEntry) : base()
        {
            ReferenceEntry = targetEntry;
            ParsedText = TextFormatParser.ParseText(ReferenceEntry.Content);

            RecalculateMinScaleSize();
        }

        public override void OnDragResizeHandle(int handleIndex)
        {
            base.OnDragResizeHandle(handleIndex);
            UpdateReferenceEntry();
        }

        private void UpdateReferenceEntry()
        {
            if (ReferenceEntry != null)
            {
                ReferenceEntry.position = Transform.Position;
                ReferenceEntry.Size = Transform.Scale;
            }
        }


        public override void OnDrag()
        {
            ReferenceEntry.position = Transform.Position;
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
                baseColor: Settings.ColorToVec4(GetTextColor()),
                sourceWidth: Transform.Scale.X - 36,
                WorldSpace: true
            );
        }

        public void RecalculateMinScaleSize()
        {
            RectangleF textBox = TextHelper.GetStringRenderBox(ParsedText);

            MinWidth = textBox.Width + 36;
            MinHeight = textBox.Height + 32;
        }


        public void StartEditing()
        {
            _isEditing = true;
            AudioHandler.PlaySound("question_001");
        }

        public void EndEditing(string? targetContent = null)
        {
            if (targetContent != null)
            {
                ReferenceEntry.Content = targetContent;
                ParsedText = TextFormatParser.ParseText(targetContent);
                RecalculateMinScaleSize();
                OnDragResizeHandle(3);
            }

            _isEditing = false;
        }

        private Color GetBackgroundColor()
        {
            return ReferenceEntry.mark switch
            {
                EntryMark.NONE => Settings.EntryBGColor,
                EntryMark.DROPPED => Settings.EntryDroppedBGColor,
                EntryMark.DONE => Settings.EntryDoneBGColor,
                EntryMark.PRIORITY => Settings.EntryPriorityBGColor,
                _ => Settings.EntryBGColor,
            };
        }

        private Color GetTextColor()
        {
            return ReferenceEntry.mark switch
            {
                EntryMark.NONE => Settings.EntryTextColor,
                EntryMark.DROPPED => Settings.EntryDroppedTextColor,
                EntryMark.DONE => Settings.EntryDoneTextColor,
                EntryMark.PRIORITY => Settings.EntryPriorityTextColor,
                _ => Settings.EntryTextColor,
            };
        }

    }
}