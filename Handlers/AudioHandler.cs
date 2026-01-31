using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Structs;


public static class AudioHandler
{
    public static Dictionary<string, SoundObject> SoundObjects { get; private set; } = [];
    private static MiniAudioEngine engine = new();
    private static AudioPlaybackDevice? playbackDevice;
    public static readonly AudioFormat format = AudioFormat.DvdHq;

    public static void Init()
    {
        engine.UpdateAudioDevicesInfo();
        var defaultDevice = engine.PlaybackDevices.First(x => x.IsDefault);

        playbackDevice = engine.InitializePlaybackDevice(defaultDevice, format);
        playbackDevice.Start();
        GetAllSounds();

        UpdateSoundSettings();
    }

    public static void GetAllSounds()
    {
        foreach (var soundObj in SoundObjects)
        {
            soundObj.Value?.soundPlayer?.Dispose();
        }
        SoundObjects.Clear();

        var audioResources = ResourceHelper.ManifestResources
            .Where(x =>
                x.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                x.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                x.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in audioResources)
        {
            try
            {
                var audioBytes = ResourceHelper.LoadEmbeddedBytes(resourceName);
                if (audioBytes is null || audioBytes.Length == 0)
                {
                    Logger.Log("AudioHandler", $"Failed to load audio: {resourceName}:\nGot empty Bytes from the ResourceHelper", LogLevel.ERROR);
                    continue;
                }

                string key = resourceName;
                var parts = key.Split('.');
                if (parts.Length >= 2)
                {
                    // get only the file name without extension
                    key = $"{parts[^2]}";
                }
                //Debug.WriteLine(key);

                var createdSoundObject = new SoundObject(audioBytes, engine, format);

                playbackDevice?.MasterMixer.AddComponent(createdSoundObject.soundPlayer);

                SoundObjects[key] = createdSoundObject;
            }
            catch (Exception ex)
            {
                Logger.Log("AudioHandler", $"Error loading audio {resourceName}:\n{ex}", LogLevel.ERROR);
            }
        }
    }

    public static SoundPlayer? PlaySound(string soundName)
    {
        if (Settings.MasterMuted || Settings.MasterVolume <= 0f) { return null; }
        if (SoundObjects.TryGetValue(soundName, out var sound))
        {
            var player = sound.soundPlayer;
            player.IsLooping = false;

            player.Stop(); // this should reset if it is already playing
            player.Play();

            return player;
        }
        
        Logger.Log("AudioHandler", $"Sound: {soundName} not found!", LogLevel.WARNING);
        return null;
    }

    public static void UpdateSoundSettings()
    {
        if (playbackDevice == null) { return; }
        var clampedVolume = Math.Clamp(Settings.MasterVolume, 0f, 1f);
        playbackDevice.MasterMixer.Volume = clampedVolume;
    }


    public static void Dispose()
    {
        foreach (var so in SoundObjects)
        {
            so.Value?.soundPlayer?.Dispose();
        }

        playbackDevice?.Dispose();
        engine?.Dispose();
    }

}

public class SoundObject
{
    public SoundPlayer soundPlayer;

    public SoundObject(byte[] audioBytes, MiniAudioEngine engine, AudioFormat format)
    {
        var ms = new MemoryStream(audioBytes, false);
        var provider = new StreamDataProvider(engine, format, ms);
        soundPlayer = new SoundPlayer(engine, format, provider);
    }
}
