/// <summary>
/// <para>A basic FPS counter which averages the last 60 Frame times (currently only adjustable in code)</para>
/// <para>Renders the current FPS in the Top-Left</para>
/// <para>Update should be called (only) once every Update tick (WindowManager does this, see <see cref="Managers.WindowManager.OnUpdate"/></para>
/// <para>Can be toggled via the <see cref="Settings.FPSCounterActive"/> setting></para>
/// </summary>

public static class FPSCounter
{
    public static double CurrentFPS { get; private set; }
    public static double AverageFrameTime { get; private set; }
    private static readonly int _sampleCount = 60;
    private static readonly Queue<double> _frameTimes = new(60);
    private static double _frameTimeSum;

    public static void Update(double deltaTime)
    {
        if (!Settings.FPSCounterActive) { return; }
        _frameTimes.Enqueue(deltaTime);
        _frameTimeSum += deltaTime;

        if (_frameTimes.Count > _sampleCount)
        {
            _frameTimeSum -= _frameTimes.Dequeue();
        }

        AverageFrameTime = _frameTimeSum / _frameTimes.Count;
        CurrentFPS = 1.0 / AverageFrameTime;

        //Debug.WriteLine(CurrentFPS);
    }

    public static void Render()
    {
        if (!Settings.FPSCounterActive) { return; }

        TextRenderer.Clear();

        TextRenderer.RenderText($"FPS: {(int)CurrentFPS}",
                               new(0, 0),
                               Settings.ColorToVec4(TextHelper.GetContrastColor(Settings.BackgroundColor)),
                               Settings.TextSize,
                               true
                               );

        TextRenderer.Draw();
        //Debug.WriteLine(CurrentFPS);
    }
}

