using Managers;
using Newtonsoft.Json;
using Rendering.UI;

public class SaveManager
{
    private static string BaseFolder => $"{Settings.ApplicationName} Resources";
    public static string SaveFolder => Path.Combine(BaseFolder, "saves");
    private static string AutoSaveFolder => Path.Combine(SaveFolder, "auto_saves");
    private static string SettingSaveFolder => Path.Combine(BaseFolder, "settings");
    private const string SettingSaveName = "settings.json";
    private static Save? CurrentSave = null;

    public static void SaveToFile(bool autoSave = false)
    {
        if (CurrentSave == null) return;

        string targetFolder = autoSave ? AutoSaveFolder : SaveFolder;
        Directory.CreateDirectory(targetFolder);

        UpdateSave();
        string savePath = Path.Combine(targetFolder, $"{CurrentSave.SaveName}.json");

        if (!autoSave)
        {
            CurrentSave.SavePath = savePath;
        }

        string serializedData = JsonConvert.SerializeObject(CurrentSave);
        WriteTextToFile(savePath, serializedData, $"Save (Auto-Save: {autoSave})");
    }

    public static bool LoadSettings()
    {
        Directory.CreateDirectory(SettingSaveFolder);
        string filePath = Path.Combine(SettingSaveFolder, SettingSaveName);

        if (!File.Exists(filePath))
        {
            Logger.Log("SaveManager", $"Settings file not found: {filePath}", LogLevel.WARNING);
            return false;
        }

        string? content = ReadTextFromPath(filePath);
        if (string.IsNullOrEmpty(content)) { return false; }

        var settings = JsonConvert.DeserializeObject<SettingsSave>(content);
        if (settings == null)
        {
            Logger.Log("SaveManager", $"Failed to deserialize settings: {filePath}", LogLevel.ERROR);
            return false;
        }

        Settings.LoadFromSave(settings);
        return true;
    }

    public static void SaveSettingsToDisk()
    {
        Directory.CreateDirectory(SettingSaveFolder);
        string filePath = Path.Combine(SettingSaveFolder, SettingSaveName);

        var settings = new SettingsSave();
        string serialized = JsonConvert.SerializeObject(settings, Formatting.Indented);

        WriteTextToFile(filePath, serialized, "Settings");
    }

    private static void UpdateSave()
    {
        if (CurrentSave != null)
        {
            var AllElementsInCurrentLayer = ChunkManager.GetAllObjects();
            if (AllElementsInCurrentLayer != null && AllElementsInCurrentLayer.Count() > 0)
            {
                List<UIObject> SortedElementsinLayer = [.. AllElementsInCurrentLayer
                .Where(x => !x.IsScreenSpace).OrderBy(x => x.RenderKey)];
                for (int i = 0; i < SortedElementsinLayer.Count; i++)
                {
                    SortedElementsinLayer[i].RenderKey = i;
                }
            }

            CurrentSave.Entries = EntryManager.GetAllEntries;
            CurrentSave.Connections = [.. ConnectionManager.GetAllConnections];
            CurrentSave.Groups = GroupManager.GetAllGroups;
            CurrentSave.Images = ImageManager.GetAllImages;
        }
    }

    public static void LoadFromSave(Save TargetSave)
    {
        CurrentSave = TargetSave;

        EntryManager.LoadFromSave(TargetSave.Entries);
        ConnectionManager.LoadFromSave(TargetSave.Connections);
        GroupManager.LoadFromSave(TargetSave.Groups);
        ImageManager.LoadFromSave(TargetSave.Images);

        EntryManager.LoadEntryLayer(Guid.Empty, false);
        RenderManager.ChangeScene("Main");
    }

    public static void LoadSaveFromDisk(string targetFilePath, bool ignoreAutoSave = false)
    {
        if (!File.Exists(targetFilePath))
        {
            HandleMissingFile(targetFilePath, ignoreAutoSave);
            return;
        }

        if (!ignoreAutoSave && Path.GetDirectoryName(targetFilePath) != AutoSaveFolder)
        {
            if (TryHandleNewerAutoSave(targetFilePath)) return;
        }

        LoadSaveFile(targetFilePath);
    }

    private static void HandleMissingFile(string targetFilePath, bool ignoreAutoSave)
    {
        Logger.Log("SaveManager", $"Save file not found: {targetFilePath}. Searching for auto save...", LogLevel.ERROR);

        if (ignoreAutoSave) return;

        string autoSavePath = Path.Combine(AutoSaveFolder, Path.GetFileName(targetFilePath));

        if (!File.Exists(autoSavePath))
        {
            Logger.Log("SaveManager", $"Neither save nor auto-save found: {targetFilePath}", LogLevel.FATAL);
            return;
        }

        Logger.Log("SaveManager", $"Save not found but auto-save exists: {autoSavePath}", LogLevel.WARNING);

        RenderManager.modal.ShowModal(
            Question: "File not found. Auto save exists. Load auto save?",
            OnConfirmActions: [() => LoadSaveFromDisk(autoSavePath, true)],
            OnCancelActions: []
        );
    }

    private static bool TryHandleNewerAutoSave(string targetFilePath)
    {
        string autoSavePath = Path.Combine(AutoSaveFolder, Path.GetFileName(targetFilePath));

        if (!File.Exists(autoSavePath)) return false;

        DateTime saveTime = File.GetLastWriteTime(targetFilePath);
        DateTime autoSaveTime = File.GetLastWriteTime(autoSavePath);

        if (autoSaveTime <= saveTime) return false;

        RenderManager.modal.ShowModal(
            Question: "The auto save is newer. Load auto save instead?",
            OnConfirmActions: [() => LoadSaveFromDisk(autoSavePath, true)],
            OnCancelActions: [() => LoadSaveFromDisk(targetFilePath, true)]
        );

        return true;
    }

    private static void LoadSaveFile(string filePath)
    {
        try
        {
            string? content = ReadTextFromPath(filePath);
            if (string.IsNullOrEmpty(content)) { return; }

            Save? save = JsonConvert.DeserializeObject<Save>(content);

            if (save != null)
            {
                LoadFromSave(save);
                Logger.Log("SaveManager", $"Save loaded: {filePath}", LogLevel.INFO);
            }
            else
            {
                Logger.Log("SaveManager", $"Failed to deserialize save: {filePath}", LogLevel.FATAL);
            }
        }
        catch (Exception e)
        {
            Logger.Log("SaveManager", $"Error loading save: {filePath}\n{e.Message}", LogLevel.FATAL);
        }
    }

    public static void DeleteSave(string saveName)
    {
        if (!DoesSaveExist(saveName)) { return; }

        try
        {
            File.Delete(Path.Combine(SaveFolder, $"{saveName}.json"));
        }
        catch (Exception e)
        {
            Logger.Log
            (
                "SaveManager",
                $"Could not delete Save.\nPath: {Path.Combine(SaveFolder, $"{saveName}.json")}\nError: {e.Message}",
                LogLevel.ERROR
            );
        }
    }

    public static bool DoesSaveExist(string target)
    {
        string path = Path.Combine(SaveFolder, $"{target}.json");
        return File.Exists(path);
    }

    public static List<string> GetAllSaves()
    {
        if (!Directory.Exists(SaveFolder)) { return []; }

        return [.. Directory.GetFiles(SaveFolder).Where(x => x.EndsWith(".json"))];
    }

    private static string? ReadTextFromPath(string filePath)
    {
        try
        {
            return File.ReadAllText(filePath);
        }
        catch (Exception e)
        {
            Logger.Log("SaveManager", $"Failed to read file: {filePath}\nError: {e.Message}", LogLevel.ERROR);
            return null;
        }
    }

    private static bool WriteTextToFile(string filePath, string content, string context = "Save")
    {
        try
        {
            File.WriteAllText(filePath, content);
            return true;
        }
        catch (Exception e)
        {
            Logger.Log("SaveManager",
                $"Could not write {context}. Path: {filePath}\nException: {e.Message}",
                LogLevel.FATAL);
            return false;
        }
    }
}

public class Save
{
    public string SaveName = "New Save";
    public string SavePath = "saves/Save.json";
    public List<Entry> Entries = [];
    public List<Connection> Connections = [];
    public List<Group> Groups = [];
    public List<ImageData> Images = [];
}