using Managers;
using Newtonsoft.Json;
using Rendering.UI;

public class SaveManager
{
    public static string SaveFolder => $"{Settings.ApplicationName} Resources{Path.DirectorySeparatorChar}saves";
    public static string SettingSaveFolder => $"{Settings.ApplicationName} Resources{Path.DirectorySeparatorChar}settings";
    public static string SettingSaveName = "settings.json";
    public static Save? CurrentSave = null;
    public static void SaveToFile()
    {
        if (!Directory.Exists(SaveFolder))
        {
            Directory.CreateDirectory(SaveFolder);
        }

        if (CurrentSave == null)
        {
            //Debug.WriteLine("Current Save is null - cannot save");
            return;
        }

        UpdateSave();
        string SerializedData = JsonConvert.SerializeObject(CurrentSave);

        if (!Directory.Exists(CurrentSave.SavePath))
        {
            CurrentSave.SavePath = $"{Path.Combine(SaveFolder,CurrentSave.SaveName)}.json";
        }

        File.WriteAllText(CurrentSave.SavePath, SerializedData);
    }

    public static bool LoadSettings()
    {
        if (!Directory.Exists(SettingSaveFolder))
        {
            Directory.CreateDirectory(SettingSaveFolder);
        }

        string saveFilePath = Path.Combine(SettingSaveFolder, SettingSaveName);
        if (!File.Exists(saveFilePath))
        {
            Logger.Log("SaveManager", "Settings file not found!  Path: " + saveFilePath, LogLevel.WARNING);

            return false;
        }

        string SaveInString = File.ReadAllText(saveFilePath);
        SettingsSave? ReadSave = JsonConvert.DeserializeObject<SettingsSave>(SaveInString);

        if (ReadSave == null)
        {
            Logger.Log("SaveManager", "Settings file IS found but could not Convert or read!  Path: " + saveFilePath, LogLevel.ERROR);
            return false;
        }

        Settings.LoadFromSave(ReadSave);

        return true;
    }

    public static bool SaveSettingsToDisk()
    {
        if (!Directory.Exists(SettingSaveFolder))
        {
            Directory.CreateDirectory(SettingSaveFolder);
        }

        string saveFilePath = Path.Combine(SettingSaveFolder, SettingSaveName);

        var SettingsSave = new SettingsSave();
        string SerializedData = JsonConvert.SerializeObject(SettingsSave, Formatting.Indented);

        File.WriteAllText(saveFilePath, SerializedData);

        return true;
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
        //TextureHandler.DisposeTextureLoadedFromPath();

        EntryManager.LoadFromSave(TargetSave.Entries);
        ConnectionManager.LoadFromSave(TargetSave.Connections);
        GroupManager.LoadFromSave(TargetSave.Groups);
        ImageManager.LoadFromSave(TargetSave.Images);

        EntryManager.LoadEntryLayer(Guid.Empty, false);
    }

    public static void LoadFromDisk(string FilePath)
    {
        if (!File.Exists(FilePath))
        {
            Logger.Log("SaveManager", "Save File not found! Path: " + FilePath, LogLevel.FATAL);
            return;
        }

        string SaveInString = File.ReadAllText(FilePath);

        Save? ReadSave = JsonConvert.DeserializeObject<Save>(SaveInString);
        if (ReadSave != null)
        {
            LoadFromSave(ReadSave);
        }
        else
        {
            Logger.Log("SaveManager", "Save file IS found but could not Convert or read!  Path: " + FilePath, LogLevel.FATAL);
        }
    }

    public static List<string> GetAllSaves()
    {
        if (!Directory.Exists(SaveFolder)) { return []; }

        return [.. Directory.GetFiles(SaveFolder).Where(x => x.EndsWith(".json"))];
    }

    public static bool DoesSaveExist(string target)
    {
        if (File.Exists($"{SaveFolder}/{target}.json"))
        {
            return true;
        }
        return false;
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