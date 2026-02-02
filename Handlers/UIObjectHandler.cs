using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

/// <summary>
/// <para>Exposes a function to get all ChildObjects of a UIObject recursively aswell as retrieving the current UIObject under the mouse</para>
/// <para>Dispatches hover start/end events to UI objects</para>
/// <para>Supports both screen-space and world-space UI elements with proper z-ordering</para>
/// <para>
/// For Worldscreen objects <see cref="UIObject.RenderOrder"/> is used for coarse ordering(Layering) and 
/// <see cref="UIObject.RenderKey"/> for granular ordering
/// </para>
/// <para>Handles click events with special cases for ContextMenu and InputField components</para>
/// <para><see cref="HandleClickOnObject"/> ignores ContextMenu. Hides the ContextMenu on Inputfield clicks</para>
/// <para><see cref="Init"/> needs to be called after window creation</para>
/// </summary>

public static class UIobjectHandler
{
    private static IMouse Mouse = InputDeviceHandler.primaryMouse!;
    private static List<UIObject> elements = [];
    private static List<UIObject> screenSpaceElements = [];
    private static List<UIObject> worldSpaceElements = [];
    public static UIObject? CurrentHoeverTarget { get; private set; } = null;


    public static void Init()
    {
        Mouse.MouseMove += Hoever;
        WindowManager.window.Closing += Dispose;
    }

    private static void Hoever(IMouse mouse, Vector2 vector)
    {
        var HoeverTarget = GetObjectUnderMouse();

        if (HoeverTarget == null || (CurrentHoeverTarget != null && !CurrentHoeverTarget.IsVisible)) { CurrentHoeverTarget?.OnHoverEnd(); CurrentHoeverTarget = null; return; }

        if (CurrentHoeverTarget == HoeverTarget) { return; }
        CurrentHoeverTarget?.OnHoverEnd();
        HoeverTarget?.OnHoverStart();
        CurrentHoeverTarget = HoeverTarget;
    }

    public static void ClearHoeverObject()
    {
        CurrentHoeverTarget = null;
    }

    public static void Dispose()
    {
        WindowManager.window.Closing -= Dispose;
        Mouse.MouseMove -= Hoever;
    }

    public static UIObject? GetObjectUnderMouse(Vector2? targetPos = null)
    {
        var screenPos = targetPos ?? Mouse.Position;
        var worldPos = Camera.ScreenToWorld(screenPos);

        elements.Clear();
        screenSpaceElements.Clear();
        worldSpaceElements.Clear();

        foreach (var o in RenderManager.CurrentScene.Children.Where(x => x != null && x.IsVisible))
        {
            if (o == null) { continue; }
            elements.Add(o);
            elements.AddRange(GetAllChildren(o));
        }

        elements = [.. elements.Where(x => x.IsVisible && x.IsSelectable).OrderByDescending(x => x.RenderOrder)];

        screenSpaceElements = [.. elements.Where(x => x.IsScreenSpace).OrderByDescending(x => x.RenderOrder).Distinct()];
        worldSpaceElements = [.. elements.Where(x => !x.IsScreenSpace).OrderByDescending(x => x.RenderOrder).ThenByDescending(x => x.RenderKey).Distinct()];

        foreach (var obj in screenSpaceElements)
        {
            if (obj.ContainsPoint(screenPos))
            {
                return obj;
            }
        }

        foreach (var obj in worldSpaceElements)
        {
            if (obj.ContainsPoint(worldPos))
            {
                return obj;
            }
        }

        return null;
    }

    public static List<UIObject> GetAllChildren(UIObject target)
    {
        List<UIObject> foundObjects = [];

        if (target.ChildObjects.Count > 0)
        {
            foundObjects.AddRange(target.ChildObjects);

            foreach (var child in target.ChildObjects)
            {
                foundObjects.AddRange(GetAllChildren(child));
            }
        }

        return foundObjects;
    }

    public static void HandleClickOnObject(UIObject target)
    {
        if (target is ContextMenu)
        {
            return;
        }

        if (target is InputField)
        {
            ContextMenu.Instance?.Hide();
            return;
        }

        target.OnClick(Mouse.Position);
        //Toolbar.Instance?.HideSubMenus();
    }
}