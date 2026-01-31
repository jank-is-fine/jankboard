using System.Drawing;
using System.Numerics;
using System.Reflection;
using Managers;
using Newtonsoft.Json;

public class Settings
{
    [Setting("Margin percent for the toolbar and settingsmenu scene", "", SettingGroupType.Basic_Settings, typeof(float), ignore: true, serialize: false)]
    public const float MARGIN_PERCENT = 0.05f;

    [Setting("Application name", "", SettingGroupType.Basic_Settings, typeof(string), ignore: true, serialize: false)]
    public static string ApplicationName => "Jank Board";

    [Setting("Redo Stack Limit", "", SettingGroupType.Basic_Settings, typeof(int), ignore: true)]
    public static int RedoStackLimit = 32;

    [Setting("FPS Counter", "", SettingGroupType.Basic_Settings, typeof(bool), ignore: true)]
    public static bool FPSCounterActive = false;

    [Setting("Background Color", "", SettingGroupType.Basic_Settings, typeof(Color))]
    public static Color BackgroundColor = Color.FromArgb(255, 23, 32, 56);

    [Setting("Toolbar Color", "", SettingGroupType.Basic_Settings, typeof(Color))]
    public static Color ToolbarBackgroundColor = Color.FromArgb(255, 59, 93, 139);

    [Setting("Background Color", "Button", SettingGroupType.Basic_Settings, typeof(Color))]
    public static Color ButtonBGColor = Color.FromArgb(255, 235, 237, 233);

    [Setting("Button Text Color", "Button", SettingGroupType.Basic_Settings, typeof(Color))]
    public static Color ButtonTextColor = Color.Black;

    [Setting("Background Color", "Inputfield", SettingGroupType.Basic_Settings, typeof(Color))]
    public static Color InputfieldBackgroundColor = Color.FromArgb(255, 168, 181, 178);

    [Setting("Text Color", "Inputfield", SettingGroupType.Basic_Settings, typeof(Color))]
    public static Color InputfieldTextColor = Color.Black;

    [Setting("Current Font", "", SettingGroupType.Basic_Settings, typeof(string))]
    public static string CurrentFontName = "OpenDyslexic";

    [Setting("Text Size", "", SettingGroupType.Font_Settings, typeof(int), ignore: true)]
    public static int TextSize = 15;

    [Setting("Text Color", "", SettingGroupType.Font_Settings, typeof(Color))]
    public static Color TextColor = Color.Black;



    [Setting("Mute Sounds", "", SettingGroupType.Sound_Settings, typeof(bool), ignore: true)]
    public static bool MasterMuted = false;

    [Setting("Sounds Volume", "", SettingGroupType.Sound_Settings, typeof(float), ignore: true)]
    public static float MasterVolume = 0.5f;



    [Setting("Connection Size", "", SettingGroupType.Connection_Settings, typeof(float), ignore: true)]
    public static int ConnectionSize = 6;



    [Setting("Color Blind", "Color-Weakness Correction Filters", SettingGroupType.Accessibility_Settings, typeof(bool))]
    public static bool ColorBlindFilterActive = false;

    [Setting("Protanopia", "Color-Weakness Correction Filters", SettingGroupType.Accessibility_Settings, typeof(bool))]
    public static bool ProtanopiaFilterActive = false;

    [Setting("Tritanopia", "Color-Weakness Correction Filters", SettingGroupType.Accessibility_Settings, typeof(bool))]
    public static bool TritanopiaFilterActive = false;

    [Setting("Deuteranopia", "Color-Weakness Correction Filters", SettingGroupType.Accessibility_Settings, typeof(bool))]
    public static bool DeuteranopiaFilterActive = false;



    [Setting("BG Color", "No Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryBGColor = Color.FromArgb(254, 87, 114, 119);

    [Setting("Text Color", "No Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryTextColor = Color.White;

    [Setting("BG Color", "Priority Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryPriorityBGColor = Color.FromArgb(149, 232, 193, 112);

    [Setting("Text Color", "Priority Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryPriorityTextColor = Color.White;

    [Setting("BG Color", "Done Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryDoneBGColor = Color.FromArgb(255, 87, 114, 119);

    [Setting("Text Color", "Done Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryDoneTextColor = Color.FromArgb(52, 0, 0, 0);

    [Setting("BG Color", "Dropped Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryDroppedBGColor = Color.FromArgb(122, 9, 9, 20);

    [Setting("Text Color", "Dropped Mark", SettingGroupType.Entry_Settings, typeof(Color))]
    public static Color EntryDroppedTextColor = Color.FromArgb(137, 0, 0, 0);



    [Setting("BG Color", "Group", SettingGroupType.Group_Settings, typeof(Color))]
    public static Color GroupBGColor = Color.FromArgb(255, 39, 35, 43);

    [Setting("Text Color", "Group", SettingGroupType.Group_Settings, typeof(Color))]
    public static Color GroupTextColor = Color.White;




    [Setting("BG Color", "Text Label", SettingGroupType.Miscellaneous, typeof(Color))]
    public static Color TextLabelBackgroundColor = Color.FromArgb(255, 23, 32, 56);


    [Setting("BG Color", "Direct", SettingGroupType.Connection_Settings, typeof(Color))]
    public static Color ConnectionDirectColor = Color.FromArgb(255, 168, 181, 178);

    [Setting("BG Color", "Loose", SettingGroupType.Connection_Settings, typeof(Color))]
    public static Color ConnectionLooseColor = Color.FromArgb(118, 232, 193, 112);

    [Setting("BG Color", "Diagonal Slashed", SettingGroupType.Connection_Settings, typeof(Color))]
    public static Color ConnectionDiagonalSlashedColor = Color.FromArgb(126, 199, 207, 204);


    [Setting("Highlight Color", "", SettingGroupType.Highlights, typeof(Color))]
    public static Color HighlightColor = Color.FromArgb(255, 165, 47, 47);

    [Setting("Hoever Highlight Color", "", SettingGroupType.Highlights, typeof(Color))]
    public static Color HoeverHighlightColor = Color.FromArgb(255, 232, 193, 112);

    [Setting("Selection Highlight Color", "", SettingGroupType.Highlights, typeof(Color))]
    public static Color SelectionMeshColor = Color.FromArgb(127, 77, 127, 224);

    [Setting("Marquee Color", "", SettingGroupType.Miscellaneous, typeof(Color))]
    public static Color MarqueeColor = Color.FromArgb(119, 79, 143, 186);


    [Setting("Clipboard command index", "", SettingGroupType.Miscellaneous, typeof(int), ignore: true)]
    public static int ClipboardCommandIndex = -1;


    [Setting("Extension filters for the filebrowser", "", SettingGroupType.Miscellaneous, typeof(ExtensionFilter), ignore: true)]
    public static ExtensionFilter ImageOrGifFilter = new("images/gifs", ["png", "gif", "jpeg", "jpg"]);


    [Setting("Log level", "", SettingGroupType.Miscellaneous, typeof(LogLevel), ignore: true)]
    public static LogLevel LogLevel = LogLevel.WARNING;

    [Setting("Base path for Logs", "", SettingGroupType.Miscellaneous, typeof(string), ignore: true, serialize: false)]
    public static string LogBasePath { get; } = $"Resources{Path.DirectorySeparatorChar}Logs";

    [Setting("Enable VSync", "VSync", SettingGroupType.Miscellaneous, typeof(bool), ignore: true, serialize: false)]
    public static bool VSyncEnabled = false;

    public static void ToggleVSync()
    {
        VSyncEnabled = !VSyncEnabled;
        WindowManager.window.VSync = VSyncEnabled;
    }

    public static Vector4 ColorToVec4(Color Target)
    {
        return new(Target.R / 255.0f, Target.G / 255.0f, Target.B / 255.0f, Target.A / 255.0f);
    }

    public static void LoadFromSave(SettingsSave save)
    {
        var settingsType = typeof(Settings);
        foreach (var kvp in save.SettingsList)
        {
            var field = settingsType.GetField(kvp.Key, BindingFlags.Public | BindingFlags.Static);
            if (field != null && kvp.Value != null)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(kvp.Value);
                    field.SetValue(null, JsonConvert.DeserializeObject(json, field.FieldType));
                }
                catch
                {
                    continue;
                }
            }
        }
        ShaderManager.SetCurrentFont(CurrentFontName);
        WindowManager.window.VSync = VSyncEnabled;
    }
}

public class SettingsSave
{
    public Dictionary<string, object?> SettingsList = [];

    public SettingsSave()
    {
        var settingsType = typeof(Settings);
        var settingsFields = settingsType.GetFields();

        foreach (var field in settingsFields)
        {
            var settingsAttrib = field.GetCustomAttribute<SettingAttribute>();
            if (settingsAttrib != null && !settingsAttrib.Serialize) { continue; }
            SettingsList.Add(field.Name, field.GetValue(null));
        }
    }

}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SettingAttribute(string displayName, string settingSection, SettingGroupType group, Type settingsType, bool ignore = false, bool serialize = true) : Attribute
{
    public string DisplayName { get; } = displayName;
    public string SettingSection { get; } = settingSection;
    public Type SettingsType { get; } = settingsType;
    public SettingGroupType Group { get; } = group;
    public bool Ignore = ignore;
    public bool Serialize = serialize;
}
