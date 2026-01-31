using System.Drawing;
using System.Numerics;
using Rendering.UI;

public class UIBooleanSetting : UIImage
{
    private string SettingsName;
    public Func<bool> Getter;
    public Action<bool> Setter;
    public Action? OptionalTrigger;
    private UIImage CheckBoxImage;
    private Texture? TrueTexture = TextureHandler.GetEmbeddedTextureByName("check_square_grey_checkmark.png");
    private Texture? FalseTexture = TextureHandler.GetEmbeddedTextureByName("check_square_grey_cross.png");
    private TextAnchorPoint textAnchorPoint;
    private ParsedText parsedText;

    public UIBooleanSetting(string settingsName, Func<bool> getter, Action<bool> setter, Action? optionalTrigger, int renderOrder, TextAnchorPoint textAnchor = TextAnchorPoint.Center_Center)
     : base(screenSpace: true, nineSlice: true)
    {
        SettingsName = settingsName;
        Getter = getter;
        Setter = setter;
        RenderOrder = renderOrder;
        OptionalTrigger = optionalTrigger;
        textAnchorPoint = textAnchor;

        CheckBoxImage = new(TrueTexture, true, false)
        {
            RenderOrder = renderOrder + 1,
            IsSelectable = false,
            IsDraggable = false
        };

        ChildObjects.Add(CheckBoxImage);
        parsedText = TextFormatParser.ParseText(settingsName);
    }

    public override void RecalcSize()
    {
        RectangleF textBox = TextHelper.GetStringRenderBox(SettingsName, FontType.BOLD, Settings.TextSize);
        Transform.Scale = new(textBox.Width + Transform.Scale.Y, textBox.Height);
        CheckBoxImage.Transform.Scale = new(Transform.Scale.Y, Transform.Scale.Y);
        PositionCheckbox();
    }

    public void PositionCheckbox()
    {
        CheckBoxImage.Transform.Scale = new(Transform.Scale.Y, Transform.Scale.Y);
        CheckBoxImage.Transform.Position = new(Transform.Position.X + Transform.Scale.X / 2 + CheckBoxImage.Transform.Scale.X / 2, Transform.Position.Y);
    }

    public override void OnClick(Vector2 pos)
    {
        base.OnClick(pos);
        Setter?.Invoke(!Getter());
        OptionalTrigger?.Invoke();
        AudioHandler.PlaySound("toggle_001");
    }

    public override void Render()
    {
        if (!IsVisible || Getter == null) return;
        
        TextureColor = Settings.ButtonBGColor;
        CheckBoxImage.TextureColor = Settings.ButtonBGColor;

        base.Render();
        CheckBoxImage.Texture = Getter() ? TrueTexture : FalseTexture;
        CheckBoxImage.Render();
        TextRenderer.RenderTextParsed(
            parsedText,
            TextHelper.CalculateTextPosition(SettingsName, Transform.Position, Transform.Scale, textAnchorPoint, IsScreenSpace),
            Settings.ColorToVec4(TextHelper.GetContrastColor(TextureColor)),
            Settings.TextSize
        );
    }

}