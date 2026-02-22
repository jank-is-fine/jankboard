using System.Drawing;
using System.Numerics;

namespace Rendering.UI
{
    public class UIButton : UIImage
    {
        public string ButtonContent { get; private set; } = "";
        ParsedText ParsedText;
        public List<Action> actions = [];
        public bool AdjustTextColor;
        public bool GetButtonColorAuto = true;
        public Color TextColor = Settings.TextColor;
        public TextAnchorPoint TextAnchorPoint;
        private bool _recalcSize;

        public UIButton
        (
            string DisplayText,
            List<Action> targetActions,
            bool recalcSize = true,
            bool screenSpace = true,
            bool nineSlice = true,
            Vector2? NineSliceBorder = null,
            TextAnchorPoint textAnchorPoint = TextAnchorPoint.Center_Center
        )
        : base(null, screenSpace, nineSlice, NineSliceBorder)
        {
            if (nineSlice)
            {
                Texture = TextureHandler.GetEmbeddedTextureByName("button_rectangle_line.png");
            }
            ButtonContent = DisplayText;
            actions = targetActions;
            IsScreenSpace = screenSpace;
            TextureColor = Settings.ButtonBGColor;
            TextAnchorPoint = textAnchorPoint;

            _recalcSize = recalcSize;
            if (recalcSize)
            {
                RecalcSize();
            }

            ParsedText = TextFormatParser.ParseText(DisplayText);
        }

        public override void OnClick(Vector2 pos)
        {
            if (IsSelectable)
            {
                foreach (Action action in actions)
                {
                    action?.Invoke();
                }
                AudioHandler.PlaySound("select_008");
            }
        }

        public override void RecalcSize()
        {
            if (!_recalcSize) { return; }
            ParsedText = TextFormatParser.ParseText(ButtonContent);
            RectangleF textBox = TextHelper.GetStringRenderBox(ParsedText);
            Transform.Scale = new(textBox.Width + _nineSliceBorder.X, textBox.Height + _nineSliceBorder.Y);

            if (AdjustTextColor)
            {
                TextColor = TextHelper.GetContrastColor(TextureColor);
            }
            else
            {
                TextColor = Settings.TextColor;
            }
        }

        public void SetScale(Vector2 Scale)
        {
            Transform.Scale = Scale;
        }

        public void SetText(string targetContent)
        {
            if (ButtonContent == targetContent) { return; }
            ButtonContent = targetContent;
            ParsedText = TextFormatParser.ParseText(targetContent);
        }

        public override void Render()
        {
            if (GetButtonColorAuto)
            {
                TextureColor = Settings.ButtonBGColor;
            }

            base.Render();

            TextRenderer.RenderTextParsed(
                ParsedText,
                TextHelper.CalculateTextPosition(ButtonContent, Transform.Position, Transform.Scale, TextAnchorPoint, IsScreenSpace),
                Settings.ColorToVec4(TextColor),
                Settings.TextSize,
                PxRangeAdjustOnZoom: !IsScreenSpace
            );

        }
    }
}