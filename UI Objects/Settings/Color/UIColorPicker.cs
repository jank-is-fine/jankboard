using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;

public class UIColorPicker : UIImage
{
    private const int STACK_LIMIT = 5;
    private Stack<Color> UndoStack = [];
    private Stack<Color> RedoStack = [];
    public Color PickedColor;
    private UIHuePicker HuePicker;
    private UIImage PickedColorPreview = new(null, true) { RenderOrder = 1, IsDraggable = false };
    private UIButton CloseButton;
    private UIButton UndoButton;
    private UIButton RedoButton;
    private UISlider _ColorDegreeSlider;
    private UISlider _AlphaSlider;
    public InputField HexInputfield { get; private set; } = null!;
    private Vector2 CharOffset;
    private RectangleF CharacterOffset;
    private List<UIObject> ObjectsToRender = [];
    private float saturation = 1f;
    private float value = 1f;
    private float hue = 0;
    private float alpha = 0;
    private bool updatingFromInternalChange = false;
    public Action<Color>? ColorChanged;

    public UIColorPicker(Color StartingColor, Action closeAction, int renderOrder = 3) : base()
    {
        PickedColorPreview = new(null, true) { RenderOrder = renderOrder + 1, IsDraggable = false };
        _ColorDegreeSlider = new(0, 360, 0, Color.Gray, renderOrder + 1, ContinueslyGetBGColor: false)
        {
            TextureColor = Color.White,
            IsScreenSpace = true,
            Shader = ShaderManager.GetShaderByName("Color-Picker-Degree Shader"),
            RenderOrder = renderOrder + 1
        };

        _AlphaSlider = new(0, 255, 0, Color.Gray, renderOrder + 1, ContinueslyGetBGColor: false)
        {
            TextureColor = Color.White,
            IsScreenSpace = true,
            Shader = ShaderManager.GetShaderByName("Color-Alpha Shader"),
            RenderOrder = renderOrder + 1
        };

        HuePicker = new()
        {
            TextureColor = Color.White,
            IsDraggable = false,
            IsScreenSpace = true,
            RenderOrder = renderOrder + 1
        };

        IsScreenSpace = true;
        PickedColor = StartingColor;
        HexInputfield = new()
        {
            TextureColor = Color.White,
            MaxCharAmount = 6,
            DisallowedCharacters = ['-', '_', ' ', '%'],
            RenderOrder = renderOrder + 1
        };

        SetValue(StartingColor);
        HexInputfield.RecalcSize();

        var CharRectange = TextHelper.GetStringRenderBox("HEX", FontType.REGULAR, Settings.TextSize);
        CharOffset = new(CharRectange.Width, CharRectange.Height);
        TextureColor = Color.White;
        RenderOrder = renderOrder;

        _ColorDegreeSlider.ValueChangedAction += OnDegreeSliderChanged;
        _AlphaSlider.ValueChangedAction += UpdateAlpha;

        HuePicker.ValueChangedAction += OnHuePickerChanged;
        HexInputfield.ContentChanged += OnHexInputChanged;

        var UndoTexture = TextureHandler.GetEmbeddedTextureByName("icon_repeat_light_mirrored.png");
        var RedoTexture = TextureHandler.GetEmbeddedTextureByName("icon_repeat_light.png");

        CloseButton = new("", [closeAction], false, true, false)
        {
            IsDraggable = false,
            RenderOrder = renderOrder + 1,
            Texture = TextureHandler.GetEmbeddedTextureByName("check_square_grey_cross-edited.png")
        };
        //CloseButton.RecalcSize();

        UndoButton = new("", [UndoSelection], false, true, false)
        {
            IsDraggable = false,
            Texture = UndoTexture,
            RenderOrder = renderOrder + 1
        };

        RedoButton = new("", [RedoSelection], false, true, false)
        {
            IsDraggable = false,
            Texture = RedoTexture,
            RenderOrder = renderOrder + 1
        };

        ChildObjects.AddRange([HuePicker, _ColorDegreeSlider, _AlphaSlider, HexInputfield, PickedColorPreview, CloseButton, UndoButton, RedoButton]);

        foreach (var obj in ChildObjects)
        {
            ObjectsToRender.AddRange(UIobjectHandler.GetAllChildren(obj));
            ObjectsToRender.Add(obj);
        }
        ObjectsToRender = [.. ObjectsToRender.OrderBy(x => x.RenderOrder)];

        _ColorDegreeSlider.SetValue(hue);
        HuePicker.SetValue(new Vector2(saturation, value));
        UpdatePickedColor(false);
    }

    public void UpdateAlpha(float Alpha, bool FinalPick)
    {
        alpha = Alpha;
        UpdatePickedColor(FinalPick);
    }

    public void SetValue(Color TargetColor)
    {
        PickedColor = TargetColor;

        var (H, S, V) = RGBtoHSV(TargetColor);
        hue = H;
        saturation = S;
        value = V;
        alpha = TargetColor.A;

        PickedColorPreview.TextureColor = TargetColor;
        HexInputfield.SetText(ColorToHex(TargetColor));
        HuePicker.HueColor = HSVtoRGB(hue, 1f, 1f);
        _ColorDegreeSlider.SetValue(hue);
        _AlphaSlider.SetValue(alpha);
        HuePicker.SetValue(new Vector2(saturation, value));

        _AlphaSlider.TextureColor = Color.FromArgb(1, PickedColor.R, PickedColor.G, PickedColor.B);

        if (!updatingFromInternalChange)
        {
            ClearUndoRedoStack();

            UndoStack.Push(PickedColor);

            if (UndoStack.Count > STACK_LIMIT)
            {
                var list = UndoStack.ToList();
                list.RemoveAt(list.Count - 1);
                UndoStack = new Stack<Color>(list.Reverse<Color>());
            }
        }
    }

    public override void RecalcSize()
    {
        CharacterOffset = TextHelper.GetStringRenderBox("Alpha  ", FontType.REGULAR);
        CharOffset = new(CharacterOffset.Width, CharacterOffset.Height);

        float sliderHeight = CharacterOffset.Height;
        HexInputfield.RecalcSize();


        CloseButton.Transform.Scale = new(sliderHeight, sliderHeight);
        UndoButton.Transform.Scale = new(sliderHeight, sliderHeight);
        RedoButton.Transform.Scale = new(sliderHeight, sliderHeight);

        CloseButton.Transform.Position = new(Transform.Position.X - Transform.Scale.X / 2 + sliderHeight / 2,
                    Transform.Position.Y - Transform.Scale.Y / 2 - CloseButton.Transform.Scale.Y / 2);

        UndoButton.Transform.Position = new(Transform.Position.X + Transform.Scale.X / 2 - (sliderHeight * 1.5f)- 5f,
                   Transform.Position.Y - Transform.Scale.Y / 2 - (UndoButton.Transform.Scale.Y / 2) );

        RedoButton.Transform.Position = new(Transform.Position.X + Transform.Scale.X / 2 - (sliderHeight / 2) + 5f,
                    Transform.Position.Y - Transform.Scale.Y / 2 - (RedoButton.Transform.Scale.Y / 2));


        _ColorDegreeSlider.Transform.Scale = new(Transform.Scale.X - CharOffset.X, sliderHeight);
        _AlphaSlider.Transform.Scale = new(Transform.Scale.X - CharOffset.X, sliderHeight);
        PickedColorPreview.Transform.Scale = new(Transform.Scale.X - 5, Transform.Scale.Y / 6);
        HuePicker.Transform.Scale = new(Transform.Scale.X, Transform.Scale.Y - sliderHeight - HexInputfield.Transform.Scale.Y - PickedColorPreview.Transform.Scale.Y);
        HexInputfield.MinWidth = Transform.Scale.X - CharOffset.X;
        HexInputfield.MinHeight = CharOffset.Y;
        HexInputfield.RecalcSize();

        float Xpos = Transform.Position.X - Transform.Scale.X/2f;
        float Ypos = Transform.Position.Y - Transform.Scale.Y/2f;

        HuePicker.Transform.Position = new(Xpos + HuePicker.Transform.Scale.X / 2, Ypos + (HuePicker.Transform.Scale.Y / 2));
        Ypos += HuePicker.Transform.Scale.Y;

        Xpos += CharOffset.X;

        _ColorDegreeSlider.Transform.Position = new(Xpos + _ColorDegreeSlider.Transform.Scale.X / 2f, Ypos + _ColorDegreeSlider.Transform.Scale.Y / 2f);
        Ypos += _ColorDegreeSlider.Transform.Scale.Y;

        _AlphaSlider.Transform.Position = new(Xpos + _AlphaSlider.Transform.Scale.X / 2, Ypos + _AlphaSlider.Transform.Scale.Y / 2f);
        Ypos += _AlphaSlider.Transform.Scale.Y;

        HexInputfield.Transform.Position = Camera.ScreenToWorld(new(
            Xpos + HexInputfield.Transform.Scale.X / 2,
            Ypos + HexInputfield.Transform.Scale.Y / 2
            ));

        Ypos += HexInputfield.Transform.Scale.Y;


        Xpos -= CharOffset.X;
        PickedColorPreview.Transform.Scale = new(PickedColorPreview.Transform.Scale.X, PickedColorPreview.Transform.Scale.Y / 2.25f);
        PickedColorPreview.Transform.Position = new(Xpos + PickedColorPreview.Transform.Scale.X / 2 + 2.5f, Ypos + (PickedColorPreview.Transform.Scale.Y / 2f));

        _ColorDegreeSlider.RecalcSize();
        _AlphaSlider.RecalcSize();
        HuePicker.RecalSize();

        foreach (var slider in ChildObjects.OfType<UISlider>())
        {
            slider.ClampHandlePos();
        }
    }

    public override void OnClick(Vector2 pos) { }

    private void OnDegreeSliderChanged(float sliderValue, bool FinalPick)
    {
        if (updatingFromInternalChange) return;

        hue = sliderValue;

        Color hueColor = HSVtoRGB(sliderValue, 1f, 1f);
        HuePicker.HueColor = hueColor;

        UpdatePickedColor(FinalPick);
        UpdateHexInput();
    }

    private void OnHuePickerChanged(Vector2 pickerValue, bool FinalPick)
    {
        if (updatingFromInternalChange) return;

        saturation = pickerValue.X;
        value = pickerValue.Y;
        UpdatePickedColor(FinalPick);
        UpdateHexInput();
    }

    private void OnHexInputChanged(string hexString)
    {
        if (updatingFromInternalChange) return;

        if (hexString.Length == 6)
        {
            try
            {
                var color = HexToColor(hexString);
                if (color == null) { return; }

                var (H, S, V) = RGBtoHSV((Color)color);

                updatingFromInternalChange = true;

                hue = H;
                saturation = S;
                value = V;


                _ColorDegreeSlider.SetValue(hue);
                HuePicker.HueColor = HSVtoRGB(hue, 1f, 1f);
                HuePicker.SetValue(new Vector2(saturation, value));
                UpdatePickedColor(true);

                updatingFromInternalChange = false;
            }
            catch
            {
                // Invalid -> ignore
            }
        }
    }

    private void UpdatePickedColor(bool generateUndoAction)
    {
        PickedColor = HSVtoRGB(hue, saturation, value);
        PickedColorPreview.TextureColor = PickedColor;
        _AlphaSlider.TextureColor = Color.FromArgb(1, PickedColor.R, PickedColor.G, PickedColor.B);

        ColorChanged?.Invoke(PickedColor);

        if (!generateUndoAction) { return; }

        if (UndoStack.Count == 0 || UndoStack.Peek() != PickedColor)
        {
            UndoStack.Push(PickedColor);

            if (UndoStack.Count > STACK_LIMIT)
            {
                var list = UndoStack.ToList();
                list.RemoveAt(list.Count - 1);
                UndoStack = new Stack<Color>(list.Reverse<Color>());
            }

            RedoStack.Clear();
        }
    }

    private void UpdateHexInput()
    {
        if (updatingFromInternalChange) return;

        updatingFromInternalChange = true;
        HexInputfield.SetText(ColorToHex(PickedColor));
        updatingFromInternalChange = false;
    }

    private Color HSVtoRGB(float h, float s, float v)
    {
        h = Math.Clamp(h, 0, 360);
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);

        h /= 60f;

        int i = (int)Math.Floor(h);
        float f = h - i;
        float p = v * (1 - s);
        float q = v * (1 - s * f);
        float t = v * (1 - s * (1 - f));

        float r, g, b;

        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
            default: r = v; g = p; b = q; break;
        }

        return Color.FromArgb(
            (int)alpha,
            (int)(r * 255),
            (int)(g * 255),
            (int)(b * 255)
        );
    }

    private (float H, float S, float V) RGBtoHSV(Color color)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        float h = 0;
        float s = max == 0 ? 0 : delta / max;
        float v = max;

        if (delta != 0)
        {
            if (max == r)
                h = (g - b) / delta;
            else if (max == g)
                h = 2 + (b - r) / delta;
            else
                h = 4 + (r - g) / delta;

            h *= 60;
            if (h < 0) h += 360;
        }

        return (h, s, v);
    }

    private string ColorToHex(Color color)
    {
        return $"{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private Color? HexToColor(string hex)
    {
        if (hex.Length != 6)
        {
            //intended. No need for logging
            Debug.WriteLine("Hex string must be 6 characters long!");
            return null;
        }

        try
        {
            int r = Convert.ToInt32(hex[..2], 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return Color.FromArgb(r, g, b);
        }
        catch
        {
            return null;
        }
    }

    public void ClearUndoRedoStack()
    {
        UndoStack.Clear();
        RedoStack.Clear();
    }

    private void UndoSelection()
    {
        if (UndoStack.Count > 1)
        {
            var current = UndoStack.Pop();
            RedoStack.Push(current);

            var previous = UndoStack.Peek();

            updatingFromInternalChange = true;

            PickedColor = previous;

            var (H, S, V) = RGBtoHSV(previous);
            hue = H;
            saturation = S;
            value = V;
            alpha = previous.A;

            PickedColorPreview.TextureColor = previous;
            HexInputfield.SetText(ColorToHex(previous));
            HuePicker.HueColor = HSVtoRGB(hue, 1f, 1f);
            _ColorDegreeSlider.SetValue(hue);
            _AlphaSlider.SetValue(alpha);
            HuePicker.SetValue(new Vector2(saturation, value));
            _AlphaSlider.TextureColor = Color.FromArgb(1, previous.R, previous.G, previous.B);

            updatingFromInternalChange = false;
            ColorChanged?.Invoke(PickedColor);
        }
    }

    private void RedoSelection()
    {
        if (RedoStack.TryPop(out Color lastColor))
        {
            UndoStack.Push(PickedColor);

            updatingFromInternalChange = true;

            PickedColor = lastColor;

            var (H, S, V) = RGBtoHSV(lastColor);
            hue = H;
            saturation = S;
            value = V;
            alpha = lastColor.A;

            PickedColorPreview.TextureColor = lastColor;
            HexInputfield.SetText(ColorToHex(lastColor));
            HuePicker.HueColor = HSVtoRGB(hue, 1f, 1f);
            _ColorDegreeSlider.SetValue(hue);
            _AlphaSlider.SetValue(alpha);
            HuePicker.SetValue(new Vector2(saturation, value));
            _AlphaSlider.TextureColor = Color.FromArgb(1, lastColor.R, lastColor.G, lastColor.B);

            updatingFromInternalChange = false;
            ColorChanged?.Invoke(PickedColor);
        }
    }

    public override void Render()
    {
        if (!IsVisible) { return; }

        base.Render();

        foreach (var obj in ObjectsToRender)
        {
            obj.Render();
        }

        TextRenderer.RenderText(
            "HSV",
             new(_ColorDegreeSlider.Transform.Position.X - _ColorDegreeSlider.Transform.Scale.X / 2 - CharOffset.X,
            _ColorDegreeSlider.Transform.Position.Y - (_ColorDegreeSlider.Transform.Scale.Y / 4) - CharacterOffset.Y),
            Settings.ColorToVec4(Settings.TextColor),
            Settings.TextSize,
            true);

        TextRenderer.RenderText(
            "Alpha",
            new(_AlphaSlider.Transform.Position.X - _AlphaSlider.Transform.Scale.X / 2 - CharOffset.X,
            _AlphaSlider.Transform.Position.Y - (_AlphaSlider.Transform.Scale.Y / 4) - CharacterOffset.Y),
            Settings.ColorToVec4(Settings.TextColor),
            Settings.TextSize,
            true);

        TextRenderer.RenderText(
            "HEX",
            Camera.WorldToScreen(new(HexInputfield.Transform.Position.X - HexInputfield.Transform.Scale.X / 2 - CharOffset.X,
            HexInputfield.Transform.Position.Y + (HexInputfield.Transform.Scale.Y / 4) + CharacterOffset.Y)),
            Settings.ColorToVec4(Settings.TextColor),
            Settings.TextSize,
            true);
    }

    public override void Dispose()
    {
        base.Dispose();
        _ColorDegreeSlider.ValueChangedAction -= OnDegreeSliderChanged;
        _AlphaSlider.ValueChangedAction -= UpdateAlpha;
        HuePicker.ValueChangedAction -= OnHuePickerChanged;
        HexInputfield.ContentChanged -= OnHexInputChanged;
    }
}