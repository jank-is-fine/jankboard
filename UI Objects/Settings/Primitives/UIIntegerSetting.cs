using System.Drawing;
using Rendering.UI;

public class UIIntegerSetting : UIImage
{
    public Func<int> Getter;
    public Action<int> Setter;
    public List<Action> OptionalTriggers = [];
    private InputField inputField;
    private int MinVal;
    private int MaxVal;
    private float? MinMaxWidth = null;
    private ParsedText SettingsNameParsed;
    private ParsedText MinMaxValueParsed;
    private Color TextColor = Settings.TextColor;

    public UIIntegerSetting(string settingsName, Func<int> getter, Action<int> setter,
     List<Action>? optionalTriggers, int renderOrder, int minval = 18, int maxval = 42)
    : base(screenSpace: true, nineSlice: true)
    {

        MinVal = minval;
        MaxVal = maxval;
        var currentVal = getter();

        Getter = getter;
        Setter = setter;

        if (optionalTriggers != null)
        {
            OptionalTriggers = optionalTriggers;
        }

        var Charlimit = maxval.ToString().Length;

        inputField = new(adjustTextColor: true)
        {
            AllowedCharacters = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'],
            RenderOrder = renderOrder + 1,
            MaxCharAmount = Charlimit,
            TextureColor = Settings.ButtonBGColor,
        };
        RenderOrder = renderOrder;

        RecalcSize();
        inputField.SetText(currentVal.ToString());
        ChildObjects.AddRange([inputField]);

        SettingsNameParsed = TextFormatParser.ParseText(settingsName);
        MinMaxValueParsed = TextFormatParser.ParseText($"[b]min: {MinVal} | max: {MaxVal}[/b]");
    }

    public void InputFieldContentChanged(string content)
    {
        if (float.TryParse(content, out var parserdFloat))
        {
            if (parserdFloat < MinVal) { return; }
            if (parserdFloat > MaxVal) { return; }
            Setter((int)parserdFloat);
            ExecuteOptionalTriggers();
        }
    }

    public void SliderValueChanged(float val)
    {
        inputField.SetText(val.ToString());
        Setter((int)val);
        ExecuteOptionalTriggers();
    }

    private void ExecuteOptionalTriggers()
    {
        if (OptionalTriggers == null || OptionalTriggers.Count < 1) { return; }
        foreach (var trigger in OptionalTriggers)
        {
            trigger?.Invoke();
        }
    }

    public override void RecalcSize()
    {
        var textBoxSettingsName = TextHelper.GetStringRenderBox(SettingsNameParsed);
        var textBoxMinMax = TextHelper.GetStringRenderBox(MinMaxValueParsed);
        Transform.Scale = new(textBoxSettingsName.Width + textBoxMinMax.Width + 30f, Transform.Scale.Y); //30f for padding
        MinMaxWidth = textBoxMinMax.Width;

        inputField.RecalcSize();

        inputField.Transform.Position = Camera.ScreenToWorld(
            new(Transform.Position.X + 5f - (Transform.Scale.X / 2f) + inputField.Transform.Scale.X / 2f,
                    Transform.Position.Y - 5f + (Transform.Scale.Y / 2f) - (inputField.Transform.Scale.Y / 2f))
        );

        Transform.Scale = new(Transform.Scale.X, inputField.Transform.Scale.Y * 2);
        inputField.SetText(((int)Getter()).ToString());
    }


    public override void Render()
    {
        if (!IsVisible) { return; }

        TextureColor = Settings.ButtonBGColor;
        inputField.TextureColor = Settings.ButtonBGColor;
        TextColor = TextHelper.GetContrastColor(TextureColor);

        base.Render();

        foreach (var child in ChildObjects)
        {
            child.Render();
        }

        TextRenderer.RenderTextParsed(
            SettingsNameParsed,
            new(Transform.Position.X + 5f - Transform.Scale.X / 2f, Transform.Position.Y - Transform.Scale.Y / 2f),
            Settings.ColorToVec4(TextColor),
            Settings.TextSize
        );

        TextRenderer.RenderTextParsed(
            MinMaxValueParsed,
            new(Transform.Position.X + (Transform.Scale.X / 2) - 15f - MinMaxWidth ?? 0f, Transform.Position.Y - Transform.Scale.Y / 2f),
            Settings.ColorToVec4(TextColor),
            Settings.TextSize
        );
    }

    public override void Dispose()
    {
        base.Dispose();
        UnsubActions();
        inputField.Dispose();
    }

    public void UnsubActions()
    {
        inputField.ContentChanged -= InputFieldContentChanged;
        inputField.UnsubActions();
    }

    public void SubActions()
    {
        inputField.ContentChanged += InputFieldContentChanged;
        inputField.SubActions();
    }
}