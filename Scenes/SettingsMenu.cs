using System.Drawing;
using System.Numerics;
using System.Reflection;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

public class SettingsScene : Scene
{
    public override string SceneName => "Settings";

    private const float SETTING_PADDING = 15f;
    private const float MARGIN_PERCENT = 0.05f;
    private const float COLUMN_PADDING = 25f;
    private const float COLOR_SWATCH_SIZE = 30f;

    private Dictionary<SettingGroupType, List<(string section, UIObject)>> SettingsGroups = [];
    private Dictionary<SettingGroupType, UIButton> ToggleButtons = [];
    private SettingGroupType CurrentlyActive = SettingGroupType.Basic_Settings;
    private UIColorPicker ColorPicker = null!;
    private Action<Color>? _currentColorSetter;

    List
    <(
        string name,
        Func<Color> getter,
        Action<Color> setter,
        Action? optionalTrigger,
        string Section,
        SettingGroupType settingGroupType
    )> colorSettings = GetSettings<Color>();

    List
   <(
       string name,
       Func<float> getter,
       Action<float> setter,
       Action? optionalTrigger,
       string Section,
       SettingGroupType settingGroupType
   )> floatSettings = GetSettings<float>();

    List
    <(
        string name,
        Func<bool> getter,
        Action<bool> setter,
        Action? optionalTrigger,
        string Section,
        SettingGroupType settingGroupType
    )> booleanSettings = GetSettings<bool>();



    private UIButton BackToMainMenu = new("[b]Back to Main Menu[/b]", [SaveManager.SaveToFile, () => RenderManager.ChangeScene("Main Menu")],
                textAnchorPoint: TextAnchorPoint.Center_Center)
    {
        IsDraggable = false
    };

    private UIButton SaveQuit = new("[b]Save and Quit[/b]", [SaveManager.SaveToFile, () => WindowManager.ApplicationQuit()],
                textAnchorPoint: TextAnchorPoint.Center_Center)
    {
        IsDraggable = false
    };

    private UIButton CloseSettings = new("[b]Close Settings[/b]", [RenderManager.LoadLastScene],
                textAnchorPoint: TextAnchorPoint.Center_Center)
    {
        IsDraggable = false
    };

    public SettingsScene()
    {
        ColorPicker = new(Color.Black, HideColorPicker, 3)
        {
            Transform =
            {
                Position = new(Camera.ViewportSize.X/2, Camera.ViewportSize.Y/2),
                Scale = new(300,450)
            },
            IsDraggable = false,
            IsScreenSpace = true,
            IsVisible = false,
        };
        ColorPicker.RecalcSize();
        Children.AddRange([ColorPicker, BackToMainMenu, CloseSettings, SaveQuit]);

        foreach (var groupType in Enum.GetValuesAsUnderlyingType(typeof(SettingGroupType)))
        {
            SettingsGroups.Add((SettingGroupType)groupType, []);
        }

        SetupUI();
    }

    private void SetupUI()
    {
        foreach (var group in Enum.GetValues(typeof(SettingGroupType)))
        {
            string? groupName = group.ToString();
            if (groupName == null) { continue; }
            groupName = groupName.Replace('_', ' ');

            UIButton toggleButton = new(
               "",
                [() => SetCurrentlyActive((SettingGroupType)group), HideColorPicker],
                textAnchorPoint: TextAnchorPoint.Center_Center
            )
            {
                IsDraggable = false
            };
            toggleButton.SetText($"[b]{groupName}[/b]");
            ToggleButtons.Add((SettingGroupType)group, toggleButton);
        }

        booleanSettings.AddRange([
            (
                "Disable all Sound",
                () => Settings.MasterMuted,
                (_) => Settings.MasterMuted = !Settings.MasterMuted,
                () => AudioHandler.UpdateSoundSettings(),
                "Mute",
                SettingGroupType.Sound_Settings
            ),

            (
                "Activate FPS Counter",
                () => Settings.FPSCounterActive,
                (_) => Settings.FPSCounterActive = !Settings.FPSCounterActive,
                null,
                "Debug",
                SettingGroupType.Miscellaneous
            ),

            (
                "Enable VSync",
                () => Settings.VSyncEnabled,
                (_) => Settings.ToggleVSync(),
                null,
                "Debug",
                SettingGroupType.Miscellaneous
            )

        ]);

        floatSettings.AddRange([
            (
                "Master Volume",
                () => Settings.MasterVolume,
                x => Settings.MasterVolume = x,
                () => AudioHandler.UpdateSoundSettings(),
                "Slider",
                SettingGroupType.Sound_Settings
            ),

        ]);

        foreach (var (name, getter, setter, optionalTrigger, section, settingGroupType) in colorSettings)
        {
            UIObject settingUI = CreateColorSetting(name, getter, setter, settingGroupType, section);
            Children.Add(settingUI);
        }

        foreach (var (name, getter, setter, optionalTrigger, section, settingGroupType) in booleanSettings)
        {
            var settingUI = CreateBooleanSetting(name, getter, setter, optionalTrigger, settingGroupType, section);
            Children.Add(settingUI);
        }

        foreach (var (name, getter, setter, optionalTrigger, section, settingGroupType) in floatSettings)
        {
            var settingUI = CreateFloatSetting(name, getter, setter, optionalTrigger, settingGroupType, section);
            Children.Add(settingUI);
        }

        RecalcLayout();

        FontFamilySetting Fontsetting = new()
        {
            IsVisible = false,
            IsDraggable = false,
            TextAnchorPoint = TextAnchorPoint.Center_Center
        };

        UIIntegerSetting FontSizeSetting = new
        (
            "[b]Font size[/b]",
            getter: () => Settings.TextSize,
            setter: x => Settings.TextSize = x,
            optionalTriggers: [RecalcLayout, WindowManager.RecalcSize],
            renderOrder: 3,
            minval: 14
        )
        {
            IsVisible = false,
            Transform =
            {
                Scale = new(150, 150)
            },
            IsDraggable = false
        };

        UIIntegerSetting UndoStackLimitSetting = new
        (
            "[b]Undo stack size[/b]",
            getter: () => Settings.RedoStackLimit,
            setter: x => Settings.RedoStackLimit = x,
            optionalTriggers: [UndoRedoManager.Clear],
            renderOrder: 3,
            4,
            500
        )
        {
            IsVisible = false,
            Transform =
            {
                Scale = new(150, 120)
            },
            IsDraggable = false
        };
        UndoStackLimitSetting.RecalcSize();


        UIIntegerSetting ConnectionSize = new
        (
            "[b]Connection Size[/b]",
            getter: () => Settings.ConnectionSize,
            setter: x => Settings.ConnectionSize = x,
            optionalTriggers: [ConnectionManager.UpdateAllConnections],
            renderOrder: 3,
            5,
            500
        )
        {
            IsVisible = false,
            Transform =
            {
                Scale = new(150, 120)
            },
            IsDraggable = false
        };
        ConnectionSize.RecalcSize();

        SettingsGroups[SettingGroupType.Font_Settings]?.Add(("", FontSizeSetting));
        SettingsGroups[SettingGroupType.Font_Settings]?.Add(("", Fontsetting));

        //Prepend did not work -_-
        SettingsGroups[SettingGroupType.Miscellaneous]?.Insert(0, ("", UndoStackLimitSetting));

        SettingsGroups[SettingGroupType.Connection_Settings]?.Add(("", ConnectionSize));

        Children.AddRange([FontSizeSetting, Fontsetting, UndoStackLimitSetting, ConnectionSize]);


        Children.AddRange([.. ToggleButtons.Values.Where(x => x != null).Cast<UIObject>()]);
        Children = [.. Children.Where(x => x != null).OrderBy(x => x!.RenderOrder).Distinct()];
    }

    private SettingsColorUI CreateColorSetting(string name, Func<Color> getter, Action<Color> setter, SettingGroupType groupType, string section)
    {
        var setting = new SettingsColorUI($"[b]{name}[/b]", getter)
        {
            TextureColor = Settings.ButtonBGColor,
            IsVisible = false,
            IsDraggable = false,
            TextAnchorPoint = TextAnchorPoint.Center_Center
        };

        setting.actions.Add(() =>
        {
            ShowColorPicker(getter(), newColor =>
            {
                setting.ColorSwatch.TextureColor = newColor;
                setter(newColor);
            });
        });

        SettingsGroups[groupType]?.Add((section, setting));

        setting.RecalcSize();
        return setting;
    }

    private UIBooleanSetting CreateBooleanSetting(string name, Func<bool> getter, Action<bool> setter, Action? optionalTrigger, SettingGroupType groupType, string section)
    {
        UIBooleanSetting setting = new($"[b]{name}[/b]", getter, setter, optionalTrigger, 1)
        {
            IsVisible = false,
            IsDraggable = false
        };

        SettingsGroups[groupType]?.Add((section, setting));

        setting.RecalcSize();
        return setting;
    }

    private VolumeSetting CreateFloatSetting(string name, Func<float> getter, Action<float> setter, Action? optionalSetting, SettingGroupType groupType, string section)
    {
        VolumeSetting setting = new($"[b]{name}[/b]", getter, setter, optionalSetting, 2)
        {
            IsVisible = false,
            IsDraggable = false
        };

        SettingsGroups[groupType]?.Add((section, setting));

        setting.slider.SetValue(getter());
        setting.inputField.SetText(((int)getter()).ToString());
        setting.slider.RecalcSize();

        return setting;
    }

    private static List<(string name, Func<T> getter, Action<T> setter, Action? optionalTrigger, string Section, SettingGroupType group)>
        GetSettings<T>()
    {
        var result = new List<(string name, Func<T> getter, Action<T> setter, Action? optionalTrigger, string Section, SettingGroupType group)>();
        var settingsType = typeof(Settings);
        var fields = settingsType.GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<SettingAttribute>();
            if (attribute != null && field.FieldType == typeof(T))
            {
                if (attribute.Ignore) { continue; }

                result.Add((
                    attribute.DisplayName,
                    () => (T)field.GetValue(null)!,
                    value => field.SetValue(null, value),
                    null,
                    attribute.SettingSection,
                    attribute.Group
                ));
            }
        }

        return result;
    }

    private void SetCurrentlyActive(SettingGroupType target)
    {
        foreach (var setting in Children.OfType<SettingsColorUI>().ToList())
        {
            setting.IsVisible = false;
        }

        foreach (var setting in Children.OfType<UIBooleanSetting>().ToList())
        {
            setting.IsVisible = false;
        }

        foreach (var setting in Children.OfType<VolumeSetting>().ToList())
        {
            setting.IsVisible = false;
        }

        foreach (var setting in Children.OfType<UIIntegerSetting>().ToList())
        {
            setting.IsVisible = false;
        }

        foreach (var setting in Children.OfType<FontFamilySetting>().ToList())
        {
            setting.IsVisible = false;
            setting.CloseList();
        }


        CurrentlyActive = target;
        //UpdateToggleHighlight();
        RecalcLayout();
    }

    private List<UITextLabel> SettingSectionLabels = [];

    public override void RecalcLayout()
    {
        RecalcSize();
        var viewportSize = Camera.ViewportSize;
        var marginY = viewportSize.Y * MARGIN_PERCENT;

        LayoutToggleButtons();

        foreach (UITextLabel label in SettingSectionLabels)
        {
            label.IsVisible = false;
            label.Dispose();
            Children.Remove(label);
        }
        SettingSectionLabels.Clear();

        var settings = SettingsGroups[CurrentlyActive];
        if (settings.Count > 0)
        {
            var maxSettingSize = LayoutHelper.CalculateMaxSize([.. settings.Select(x => x.Item2).Where(x => x is not VolumeSetting && x is not UIIntegerSetting)]);

            foreach (var s in settings.Select(x => x.Item2))
            {
                if (s is UIBooleanSetting)
                {
                    s.RecalcSize();
                    s.Transform.Scale = new(s.Transform.Scale.X, maxSettingSize.Y);
                    continue;
                }

                if (s is UIIntegerSetting)
                {
                    s.RecalcSize();
                    continue;
                }

                if (s is VolumeSetting)
                {
                    // Consider volume setting should set its own min size in the recalc function
                    // this needs sane min values for the sliders
                    s.Transform.Scale = new(maxSettingSize.X, maxSettingSize.Y * 2); // double the size so that the text is fitting aswell
                    continue;
                }

                s.Transform.Scale = maxSettingSize;
            }

            float gridStartX = ToggleButtons.First().Value.Transform.Scale.X + (viewportSize.X * MARGIN_PERCENT);
            float availableWidth = viewportSize.X - gridStartX - COLOR_SWATCH_SIZE - maxSettingSize.X / 2f;

            var settingSection = settings.GroupBy(x => x.section);

            foreach (var section in settingSection)
            {
                if (!string.IsNullOrEmpty(section.Key) && section.Key != "")
                {
                    UITextLabel textlabel = new(isNineSlice: false);
                    textlabel.SetText($"[b]{section.Key}[/b]");
                    textlabel.RecalcSize();
                    textlabel.Transform.Position = new(gridStartX + textlabel.Transform.Scale.X / 2, marginY + textlabel.Transform.Scale.Y / 2);
                    marginY += textlabel.Transform.Scale.Y + SETTING_PADDING;
                    SettingSectionLabels.Add(textlabel);
                    Children.Add(textlabel);
                }

                var SettingGroup = section.GroupBy(x => x.Item2.GetType());

                foreach (var group in SettingGroup)
                {
                    LayoutHelper.Grid(
                        group.Select(x => x.Item2).ToList(),
                        new(gridStartX, marginY),
                        availableWidth,
                        new(SETTING_PADDING + COLOR_SWATCH_SIZE, SETTING_PADDING)
                    );

                    marginY = group.Last().Item2.Transform.Position.Y + (group.Last().Item2.Transform.Scale.Y / 2) + SETTING_PADDING;
                }
                marginY += SETTING_PADDING * 2f;
            }

            foreach (var setting in settings.Select(x => x.Item2))
            {
                setting.IsVisible = true;

                if (setting is SettingsColorUI colorSetting)
                {
                    colorSetting.PositionSwatch();
                }
                else if (setting is UIBooleanSetting uIBoolean)
                {
                    uIBoolean.PositionCheckbox();
                }
                else if (setting is VolumeSetting or UIIntegerSetting)
                {
                    setting.RecalcSize();
                }
                else
                {
                    setting.RecalcSize();
                }
            }
        }

        var bottomSize = LayoutHelper.CalculateMaxSize([BackToMainMenu, SaveQuit, CloseSettings]);
        BackToMainMenu.Transform.Scale = bottomSize;
        SaveQuit.Transform.Scale = bottomSize;
        CloseSettings.Transform.Scale = bottomSize;

        float startX = viewportSize.X / 2 - (
            BackToMainMenu.Transform.Scale.X +
            SaveQuit.Transform.Scale.X +
            CloseSettings.Transform.Scale.X +
            2f * COLUMN_PADDING
        ) / 2f;

        LayoutHelper.Horizontal(
            [BackToMainMenu, SaveQuit, CloseSettings],
            new(startX, viewportSize.Y - (viewportSize.Y * MARGIN_PERCENT) - bottomSize.Y / 2f),
            COLUMN_PADDING
        );
    }

    private void LayoutToggleButtons()
    {
        if (ToggleButtons.Count == 0)
            return;

        Vector2 maxScale = LayoutHelper.CalculateMaxSize([.. ToggleButtons.Values]);

        foreach (var button in ToggleButtons)
        {
            button.Value.SetScale(new Vector2(maxScale.X, button.Value.Transform.Scale.Y));
        }

        Vector2 startPos = new(
            SETTING_PADDING,
            Camera.ViewportSize.Y * MARGIN_PERCENT
        );


        LayoutHelper.Vertical([.. ToggleButtons.Values], startPos, SETTING_PADDING);
    }


    public void ShowColorPicker(Color currentColor, Action<Color> onColorSelected)
    {
        ColorPicker.SetValue(currentColor);
        ColorPicker.IsVisible = true;
        _currentColorSetter = onColorSelected;
        ColorPicker.RecalcSize();
    }

    public void ColorInputChanged(Color newColor)
    {
        _currentColorSetter?.Invoke(newColor);
    }

    public void HideColorPicker()
    {
        ColorPicker.IsVisible = false;
        _currentColorSetter = null;
    }

    public override void RecalcSize()
    {
        ColorPicker.Transform.Position = new(Camera.ViewportSize.X / 2, Camera.ViewportSize.Y / 2);
        ColorPicker.RecalcSize();
    }

    public override void Render()
    {
        TextRenderer.Clear();
        foreach (var obj in Children.Where(x => x != null).Except([ColorPicker]).Where(x => x!.IsVisible))
        {
            if (obj == null) { continue; }
            obj.Render();
        }
        TextRenderer.Draw();

        TextRenderer.Clear();
        ColorPicker.Render();
        TextRenderer.Draw();

        var hoeverObject = UIobjectHandler.CurrentHoeverTarget;

        if (hoeverObject != null)
        {
            if (hoeverObject is UIButton or UIBooleanSetting or InputField)
            {
                OutlineRender.Clear();
                OutlineRender.AddOutlineToObject(hoeverObject, 4f, Settings.HoeverHighlightColor);
                OutlineRender.Draw();
            }
        }
    }

    private void OnKeyInput(IKeyboard keyboard, Key key, int arg3)
    {
        if (key == Key.Escape)
        {
            RenderManager.ChangeScene("Main");
        }
    }

    public override void UnsubActions()
    {
        SaveManager.SaveSettingsToDisk();
        MouseHandler.Unsubscribe();

        foreach (var obj in Children.OfType<InputField>())
        {
            obj.UnsubActions();
        }

        foreach (var obj in Children.OfType<VolumeSetting>())
        {
            obj.UnsubActions();
        }

        foreach (var obj in Children.OfType<UIIntegerSetting>())
        {
            obj.UnsubActions();
        }

        foreach (var obj in Children.OfType<UIButtonList>())
        {
            obj.CloseList();
        }


        HideColorPicker();
        ColorPicker.ColorChanged -= ColorInputChanged;

        if (InputDeviceHandler.primaryKeyboard != null)
        {
            InputDeviceHandler.primaryKeyboard.KeyDown -= OnKeyInput;
        }
        ColorPicker.HexInputfield.UnsubActions();
    }

    public override void SubActions()
    {
        MouseHandler.Subscribe();

        Camera.ResetView();
        foreach (var obj in Children.OfType<InputField>())
        {
            obj.SubActions();
        }

        foreach (var obj in Children.OfType<VolumeSetting>())
        {
            obj.SubActions();
        }

        foreach (var obj in Children.OfType<UIIntegerSetting>())
        {
            obj.SubActions();
        }

        foreach (var obj in Children.OfType<UIButtonList>())
        {
            obj.CloseList();
        }

        ColorPicker.RecalcSize();
        ColorPicker.ColorChanged += ColorInputChanged;
        ColorPicker.HexInputfield.SubActions();

        if (InputDeviceHandler.primaryKeyboard != null)
        {
            InputDeviceHandler.primaryKeyboard.KeyDown += OnKeyInput;
        }

        //UpdateToggleHighlight();
        RecalcLayout();
    }

    public override void Dispose()
    {
        foreach (var obj in Children)
        {
            if (obj == null) { continue; }
            obj.Dispose();
        }
        ColorPicker.ColorChanged -= ColorInputChanged;
        UnsubActions();
    }
}

public enum SettingGroupType
{
    Basic_Settings,
    Font_Settings,
    Sound_Settings,
    Entry_Settings,
    Group_Settings,
    Connection_Settings,
    Accessibility_Settings,
    Miscellaneous,
    Highlights
}


