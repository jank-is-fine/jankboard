using System.Drawing;
using Rendering.UI;

public class VolumeSetting : UIImage
{
    public Func<float> Getter;
    public Action<float> Setter;
    public Action? OptionalTrigger;
    public UISlider slider { get; protected set; }
    public InputField inputField { get; protected set; }
    private float MinVal;
    private float MaxVal;
    private ParsedText SettingsNameParsed;
    private Color LastBGColor = Settings.ButtonBGColor;
    private Color TextColor = Settings.TextColor;


    public VolumeSetting(string settingsName, Func<float> getter, Action<float> setter, Action? optionalTrigger, int renderOrder, float minval = 0, float maxval = 1)
    : base(screenSpace: true)
    {
        MinVal = minval;
        MaxVal = maxval;
        var currentVal = getter();

        slider = new(MinVal, MaxVal, currentVal, Color.Gray, renderOrder + 1);

        Getter = getter;
        Setter = setter;
        OptionalTrigger = optionalTrigger;

        inputField = new(adjustTextColor: true)
        {
            AllowedCharacters = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'],
            RenderOrder = renderOrder + 1,
            MaxCharAmount = 3,
            TextureColor = Settings.ButtonBGColor,
        };
        RenderOrder = renderOrder;

        Transform.Scale = new(150, 100);

        RecalcSize();
        slider.SetValue(currentVal);
        inputField.SetText(((int)(currentVal * 100f)).ToString());
        ChildObjects.AddRange([slider, .. slider.ChildObjects, inputField]);

        SettingsNameParsed = TextFormatParser.ParseText(settingsName);

    }

    public void InputFieldContentChanged(string content)
    {
        if (float.TryParse(content, out var parserdFloat))
        {
            slider.SetValue(parserdFloat / 100f);
            Setter(parserdFloat / 100f);
            OptionalTrigger?.Invoke();
        }
    }

    public void SliderValueChanged(float val, bool arg2)
    {
        inputField.SetText(((int)(val * 100f)).ToString());
        Setter(val);
        OptionalTrigger?.Invoke();
    }

    public override void RecalcSize()
    {
        inputField.RecalcSize();

        inputField.Transform.Position = Camera.ScreenToWorld(
            new(Transform.Position.X + (Transform.Scale.X / 2) - inputField.Transform.Scale.X / 2,
                    Transform.Position.Y + (Transform.Scale.Y / 2) - (inputField.Transform.Scale.Y / 2))
        );

        slider.Transform.Scale = new(Transform.Scale.X - inputField.Transform.Scale.X, Transform.Scale.Y / 2);
        slider.Transform.Position = new(Transform.Position.X - (Transform.Scale.X / 2) + slider.Transform.Scale.X / 2,
                    Transform.Position.Y + (Transform.Scale.Y / 2) - slider.Transform.Scale.Y / 2
        );

        slider.RecalcSize();
        slider.SetValue(Getter());
        inputField.SetText(((int)(Getter() * 100f)).ToString());
        slider.RecalcSize();

    }


    public override void Render()
    {
        if (!IsVisible) { return; }

        TextureColor = Settings.ButtonBGColor;
        if (LastBGColor != TextureColor)
        {
            TextColor = TextHelper.GetContrastColor(TextureColor);
            LastBGColor = TextureColor;
            inputField.TextureColor = TextureColor;
        }

        base.Render();

        foreach (var child in ChildObjects)
        {
            child.Render();
        }

        TextRenderer.RenderTextParsed(
            SettingsNameParsed,
            new(Transform.Position.X - Transform.Scale.X / 2f, Transform.Position.Y - Transform.Scale.Y / 2f),
            Settings.ColorToVec4(Settings.TextColor),
            Settings.TextSize
        );
    }

    public override void Dispose()
    {
        base.Dispose();
        UnsubActions();
        inputField.Dispose();
        slider.Dispose();
    }

    public void UnsubActions()
    {
        inputField.ContentChanged -= InputFieldContentChanged;
        slider.ValueChangedAction -= SliderValueChanged;
        inputField.UnsubActions();
    }

    public void SubActions()
    {
        inputField.ContentChanged += InputFieldContentChanged;
        slider.ValueChangedAction += SliderValueChanged;
        inputField.SubActions();
    }
}