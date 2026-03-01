using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

public partial class MainScene : Scene
{
    private bool _IsPanning = false;
    private bool _isDragging;
    private Vector2 _lastMousePosition;
    private Vector2 _dragStart;
    private UIObject? _mouseDownObject;
    private bool _hasExceededDragThreshold;


    private void OnKeyInput(IKeyboard keyboard, Key key, int arg3)
    {
        switch (key)
        {
            case Key.Escape:
                if (EntryManager.IsEditing || GroupManager.IsEditing)
                {
                    EntryManager.CancelEditing();
                    GroupManager.CancelEditing();
                    return;
                }
                RenderManager.ChangeScene("Settings");
                break;
            case Key.R:
                if (EntryManager.IsEditing || GroupManager.IsEditing)
                { return; }
                Camera.ResetView();
                break;

            case Key.Y when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                if (EntryManager.IsEditing || GroupManager.IsEditing)
                { return; }
                UndoRedoManager.Undo();
                break;

            case Key.Z when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                if (EntryManager.IsEditing || GroupManager.IsEditing)
                { return; }
                UndoRedoManager.Redo();
                break;

            case Key.D when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                if (EntryManager.IsEditing || GroupManager.IsEditing)
                { return; }
                DuplicateSelectedData();
                break;

            case Key.C when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                if (EntryManager.IsEditing || GroupManager.IsEditing)
                { return; }
                CopySelectedData();
                break;

            case Key.V when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                if (EntryManager.IsEditing || GroupManager.IsEditing)
                { return; }
                PasteCopiedData();
                break;

            case Key.Space when InputDeviceHandler.primaryMouse?.IsButtonPressed(MouseButton.Left) ?? false:
                StartPanning();
                break;
        }
    }

    private void OnKeyobardKeyUp(IKeyboard keyboard, Key key, int arg3)
    {
        switch (key)
        {
            case Key.Space:
                EndPanning();
                break;
        }
    }

    public void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (_IsPanning)
        {
            Camera.Pan(position - _lastMousePosition);
            _lastMousePosition = position;
            MarqueeHandler.StopMarquee();
            return;
        }

        if (_isDragging && !_hasExceededDragThreshold)
        {
            float distance = Vector2.Distance(_dragStart, position);
            if (distance > 3.0f)
            {
                _hasExceededDragThreshold = true;

                if (_mouseDownObject == null && !MarqueeHandler.isMarqueeActive)
                {
                    MarqueeHandler.StartMarquee(_dragStart);
                }
                else if (_mouseDownObject != null && !SelectionManager.IsObjectSelected(_mouseDownObject))
                {
                    HandleObjectSelection(_mouseDownObject);
                    SelectionManager.DragStart();
                }
            }
        }

        Vector2 mouseWorld = Camera.ScreenToWorld(position);
        Vector2 lastMouseWorld = Camera.ScreenToWorld(_lastMousePosition);

        if (_isDragging && _hasExceededDragThreshold && !EntryManager.IsEditing)
        {
            if (SelectionManager.IsAnyObjectSelected() && !MarqueeHandler.isMarqueeActive && !InputDeviceHandler.IsKeyPressed(Key.AltLeft))
            {
                SelectionManager.Dragging(mouseWorld - lastMouseWorld);
            }
            else if (MarqueeHandler.isMarqueeActive)
            {
                MarqueeHandler.DrawMarquee(mouseWorld);
            }
        }

        _lastMousePosition = position;
    }

    public void OnMouseDown(IMouse mouse, MouseButton button)
    {
        switch (button)
        {
            case MouseButton.Left when InputDeviceHandler.IsKeyPressed(Key.Space):
                StartPanning(mouse.Position);
                break;
            case MouseButton.Left:
                HandleLeftClick(mouse);
                break;
            case MouseButton.Right:
                HandleRightClick(mouse);
                break;
            case MouseButton.Middle:
                StartPanning(mouse.Position);
                break;
        }
    }

    public void OnMouseUp(IMouse mouse, MouseButton button)
    {
        switch (button)
        {
            case MouseButton.Left:
                HandleLeftMouseUp();
                break;
            case MouseButton.Middle:
                EndPanning();
                break;
        }
    }

    private void HandleLeftClick(IMouse mouse)
    {
        _mouseDownObject = UIobjectHandler.GetObjectUnderMouse();
        _dragStart = mouse.Position;
        _hasExceededDragThreshold = false;

        if (_mouseDownObject != null)
        {
            if (InputDeviceHandler.IsKeyPressed(Key.AltLeft))
            {
                ConnectionManager.StartConnection(_mouseDownObject);
            }
        }
        else
        {
            EntryManager.EndEditing();
            GroupManager.EndEditing();
            toolbar?.HideSubMenus();
            ContextMenu.Instance?.Hide();
        }

        _isDragging = true;
        _lastMousePosition = mouse.Position;
    }

    public void OnDoubleClick(IMouse mouse, MouseButton button, Vector2 position)
    {
        if (button != MouseButton.Left) return;

        var uIObject = UIobjectHandler.GetObjectUnderMouse();
        if (uIObject != null)
        {
            if (uIObject is EntryUI entry)
            {
                EntryManager.StartEditing(entry);
                GroupManager.EndEditing();
            }
            else if (uIObject is GroupUI groupUI)
            {
                GroupManager.StartEditing(groupUI);
                EntryManager.EndEditing();
            }
        }
    }

    private void HandleLeftMouseUp()
    {
        SelectionManager.DragEnd();

        if (_IsPanning)
        {
            EndPanning();
        }
        else if (_isDragging)
        {
            var currentObject = UIobjectHandler.GetObjectUnderMouse();

            if (InputDeviceHandler.IsKeyPressed(Key.AltLeft))
            {
                if (InputDeviceHandler.IsKeyPressed(Key.ShiftLeft))
                {
                    ConnectionManager.EndConnectionFromAllSelected(currentObject);
                }
                else
                {
                    ConnectionManager.EndConnection(currentObject);
                }
            }
            else if (MarqueeHandler.isMarqueeActive)
            {
                MarqueeHandler.PerformMarqueeSelection();
            }
            else if (!_hasExceededDragThreshold && _mouseDownObject == null)
            {
                SelectionManager.ClearSelection();
                WindowManager.SettingsMenu.HideColorPicker();
            }
            else if (!MarqueeHandler.isMarqueeActive &&
                    SelectionManager.IsAnyObjectSelected(excludedTypes: [typeof(InputField), typeof(UIButton)]) &&
                    _hasExceededDragThreshold)
            {
                AudioHandler.PlaySound("drop_001");
            }

            _isDragging = false;
            MarqueeHandler.StopMarquee();
        }

        if (_mouseDownObject != null && !_hasExceededDragThreshold)
        {
            var mouseUpObject = UIobjectHandler.GetObjectUnderMouse();
            if (mouseUpObject == _mouseDownObject)
            {
                UIobjectHandler.HandleClickOnObject(_mouseDownObject);

                if (_mouseDownObject is not UIButton)
                {
                    HandleObjectSelection(_mouseDownObject);
                }
            }
        }

        _hasExceededDragThreshold = false;
        _mouseDownObject = null;
    }

    private void HandleRightClick(IMouse mouse)
    {
        if (ContextMenu.Instance.IsVisible)
        {
            ContextMenu.Instance.Hide();
            return;
        }

        var rightClickTarget = UIobjectHandler.GetObjectUnderMouse();
        if (rightClickTarget != null)
        {
            SelectionManager.Select(rightClickTarget, SelectionOption.EXCLUSIVE_ADD);
        }
        else
        {
            SelectionManager.ClearSelection();
        }

        ContextMenu.Instance.Show(mouse.Position);
    }

    private void StartPanning(Vector2? position = null)
    {
        if (position != null)
        {
            _lastMousePosition = (Vector2)position;
        }
        else if (InputDeviceHandler.primaryMouse != null)
        {
            _lastMousePosition = InputDeviceHandler.primaryMouse.Position;
        }

        toolbar.HideSubMenus();
        ContextMenu.Instance?.Hide();
        _IsPanning = true;

        if (InputDeviceHandler.primaryMouse != null)
        {
            InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.Hand;
        }
    }

    private void EndPanning()
    {
        _IsPanning = false;

        if (InputDeviceHandler.primaryMouse != null)
        {
            InputDeviceHandler.primaryMouse.Cursor.StandardCursor = StandardCursor.Default;
        }
    }

    public void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
    {
        ContextMenu.Instance.Hide();

        float prevZoom = Camera.Zoom;
        Camera.Zoom = Math.Clamp(Camera.Zoom * (float)Math.Pow(1.1, scrollWheel.Y), 0.1f, 5f);

        Vector2 mousePos = new(mouse.Position.X, mouse.Position.Y);

        Vector2 beforeZoomWorld = Camera.ScreenToWorld(mousePos, prevZoom);
        Vector2 afterZoomWorld = Camera.ScreenToWorld(mousePos, Camera.Zoom);

        Vector2 worldDelta = beforeZoomWorld - afterZoomWorld;
        Camera.Position += worldDelta;
    }

}