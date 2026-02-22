using System.Drawing;
using Rendering.UI;

public class UITextLabel : UIImage
{
    private string Text;
    private ParsedText ParsedText;
    public float MinWidth = 40f;
    public float MinHeight = 10f;
    public bool GetBGColorAuto = true;
    private Color TextColor = Color.Black;
    private Color LastBGColor;
    private const float WIDTH_MARGIN = 10f;
    public TextAnchorPoint TextAnchorPoint;

    public UITextLabel(string DisplayText = " ", bool IsScreenSpace = true, bool isNineSlice = true, TextAnchorPoint textAnchorPoint = TextAnchorPoint.Left_Top)
    : base(null, IsScreenSpace, nineSlice: isNineSlice)
    {
        Text = DisplayText;
        TextureColor = Settings.TextLabelBackgroundColor;
        LastBGColor = TextureColor;
        IsDraggable = false;
        TextAnchorPoint = textAnchorPoint;

        ParsedText = TextFormatParser.ParseText(DisplayText);
        RecalcSize();
        UpdateTextColor();
        LastBGColor = TextureColor;
    }

    public void SetText(string targetString)
    {
        Text = targetString;
        ParsedText = TextFormatParser.ParseText(targetString);
    }

    public override void RecalcSize()
    {
        RectangleF textBox = TextHelper.GetStringRenderBox(ParsedText);
        Transform.Scale = new(Math.Max(textBox.Width, MinWidth) + WIDTH_MARGIN, Math.Max(textBox.Height, MinHeight));
        ParsedText = TextFormatParser.ParseText(Text);
    }

    public void UpdateTextColor()
    {
        TextColor = TextHelper.GetContrastColor(TextureColor);
    }

    public override void Render()
    {
        if (IsDisposed || !IsVisible)
        {
            return;
        }

        if (GetBGColorAuto)
        {
            TextureColor = Settings.TextLabelBackgroundColor;
        }

        if (TextureColor != LastBGColor)
        {
            UpdateTextColor();
            LastBGColor = TextureColor;
        }

        base.Render();

        if (IsScreenSpace)
        {
            TextRenderer.RenderTextParsed(
                ParsedText,
                TextHelper.CalculateTextPosition(Text, Transform.Position, Transform.Scale, TextAnchorPoint, IsScreenSpace),
                Settings.ColorToVec4(TextColor),
                Settings.TextSize
            );
        }
        else
        {
            TextRenderer.RenderTextWorldParsed(
                ParsedText,
                TextHelper.CalculateTextPosition(Text, Transform.Position, Transform.Scale, TextAnchorPoint, IsScreenSpace),
                Settings.ColorToVec4(TextColor),
                Settings.TextSize
            );
        }
    }
}