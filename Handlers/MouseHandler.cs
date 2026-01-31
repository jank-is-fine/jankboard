using Managers;
using Rendering.UI;
using Silk.NET.Input;
using System.Numerics;

/// <summary>
/// <para>Generic mouse handler. Currently only used for Screenspace Scenes</para>
/// <para>Requires explicit subscription/unsubscription to mouse events <see cref="Subscribe"/> and <see cref="Unsubscribe"/></para>
/// <para>Distinguishes between clicks and drags ( 3.0f distance between MouseDown and Mouse up)</para>
/// <para>Does not support multi-selection</para>
/// </summary>

public static class MouseHandler
{
    private static IMouse? _mouse = InputDeviceHandler.primaryMouse;
    private static bool _isSubscribed = false;

    private static bool _isDragging = false;
    private static bool _hasExceededDragThreshold = false;
    private static Vector2 _dragStart;
    private static UIObject? _mouseDownObject;

    private const float DRAG_THRESHOLD = 3.0f;

    public static void Subscribe()
    {
        if (_mouse == null || _isSubscribed) return;

        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;
        _isSubscribed = true;
    }

    public static void Unsubscribe()
    {
        if (_mouse == null || !_isSubscribed) return;

        _mouse.MouseDown -= OnMouseDown;
        _mouse.MouseUp -= OnMouseUp;
        _mouse.MouseMove -= OnMouseMove;
        _isSubscribed = false;
    }

    private static void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            HandleLeftClick(mouse);
        }
    }

    private static void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (_isDragging && !_hasExceededDragThreshold)
        {
            float squaredDistance = Vector2.DistanceSquared(_dragStart, position);
            if (squaredDistance > DRAG_THRESHOLD * DRAG_THRESHOLD)
            {
                _hasExceededDragThreshold = true;

                if (_mouseDownObject != null)
                {
                    SelectionManager.Select(_mouseDownObject, SelectionOption.NONE);
                    SelectionManager.DragStart();
                }
            }
        }

        if (_isDragging && _hasExceededDragThreshold && _mouseDownObject != null)
        {
            Vector2 delta = position - _mouseDownObject.Transform.Position;
            SelectionManager.Dragging(delta);
        }

    }

    private static void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            HandleLeftMouseUp();
        }
    }

    private  static void HandleLeftClick(IMouse mouse)
    {
        _mouseDownObject = UIobjectHandler.GetObjectUnderMouse();
        _dragStart = mouse.Position;
        _hasExceededDragThreshold = false;

        if (_mouseDownObject == null)
        {
            SelectionManager.ClearSelection();
        }

        _isDragging = true;
    }

    private  static void HandleLeftMouseUp()
    {
        if (_isDragging)
        {
            SelectionManager.DragEnd();

            if (_mouseDownObject != null && !_hasExceededDragThreshold)
            {
                var mouseUpObject = UIobjectHandler.GetObjectUnderMouse();
                if (mouseUpObject == _mouseDownObject)
                {
                    UIobjectHandler.HandleClickOnObject(_mouseDownObject);
                    SelectionManager.Select(_mouseDownObject, SelectionOption.NONE);
                }
            }
        }

        ResetDragState();
    }

    private  static void ResetDragState()
    {
        _isDragging = false;
        _hasExceededDragThreshold = false;
        _mouseDownObject = null;
    }

    public static void Dispose()
    {
        Unsubscribe();
    }
}