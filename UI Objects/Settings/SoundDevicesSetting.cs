using Rendering.UI;

public class SoundDevicesSetting : UIButtonList
{
    public SoundDevicesSetting(TextAnchorPoint _textAnchorPoint = TextAnchorPoint.Center_Center)
    : base("[b]Current Device[/b]", isScreenSpace: true, ImmidieteClose: true, [], false, textAnchorPoint: _textAnchorPoint)
    {
        AdjustTextColor = true;
        AudioHandler.PlayBackDeviceChanged += RefreshAudioDevices;
    }

    public void RefreshAudioDevices()
    {
        foreach (var child in ChildObjects)
        {
            child.Dispose();
        }
        ChildObjects.Clear();

        var devices = AudioHandler.GetAllPlaybackDevices();
        var currentDevice = AudioHandler.GetCurrentPlaybackDevice();

        if (devices.Length <= 0) { return; }


        foreach (var device in devices)
        {
            bool highlight = currentDevice != null && currentDevice.Value.Id == device.Id;

            UIButton uIButton = new
            (
                device.Name,
                [
                    () => AudioHandler.ChangeAudioDevice(device)
                ],
                textAnchorPoint: TextAnchorPoint.Left_Top
            )
            {
                AdjustTextColor = true,
                GetButtonColorAuto = !highlight,

                TextureColor = highlight ? Settings.HighlightColor : Settings.ButtonBGColor
            };
            uIButton.RecalcSize();
            AddOptionButton(uIButton);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        AudioHandler.PlayBackDeviceChanged -= RefreshAudioDevices;
    }
}