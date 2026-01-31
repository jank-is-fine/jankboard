using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

public static class MarqueeHandler
{
    public static Vector2 _DragStart;
    public static Vector2 _DragEnd;
    public static bool isMarqueeActive;
    public static MarqueeUI MarqueeRect = new();

    public static void StartMarquee(Vector2 pos)
    {
        //SelectionManager.ClearSelection();
        _DragStart = Camera.ScreenToWorld(pos);
        _DragEnd = _DragStart;
        isMarqueeActive = true;
    }

    public static void StopMarquee()
    {
        isMarqueeActive = false;
        MarqueeRect.IsVisible = false;
    }

    public static void DrawMarquee(Vector2 end)
    {
        if (MarqueeRect == null)
        {
            Logger.Log("MarqueeHandler", "Marquee rect does not exist!", LogLevel.FATAL);
            return;
        }

        MarqueeRect.Start = _DragStart;
        MarqueeRect.End = end;
        MarqueeRect.IsVisible = true;
        _DragEnd = end;
    }

    public static void PerformMarqueeSelection()
    {
        float left = MathF.Min(_DragStart.X, _DragEnd.X);
        float right = MathF.Max(_DragStart.X, _DragEnd.X);
        float top = MathF.Min(_DragStart.Y, _DragEnd.Y);
        float bottom = MathF.Max(_DragStart.Y, _DragEnd.Y);

        RectangleF marqueeRect = new(left, top, right - left, bottom - top);

        var visibleObjects = ChunkManager.GetObjectsInVisibleArea(Camera.GetVisibleWorldArea())
            .Where(obj => obj.IsVisible && obj.IsSelectable)
            .Where(obj => obj.IntersectsWithRect(marqueeRect))
            .ToList();

        SelectionOption option = SelectionOption.NONE;

        if (InputDeviceHandler.IsKeyPressed(Key.ControlLeft))
        {
            option = SelectionOption.EXCLUSIVE_ADD;
        }
        else if (InputDeviceHandler.IsKeyPressed(Key.ShiftLeft))
        {
            option = SelectionOption.EXCLUSIVE_REMOVE;
        }

        if (visibleObjects.Count > 0)
        {
            SelectionManager.Select(visibleObjects, option);
        }
        else
        {
            SelectionManager.ClearSelection();
        }

        isMarqueeActive = false;
        MarqueeRect.IsVisible = false;
    }
}