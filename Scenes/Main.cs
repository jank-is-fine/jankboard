using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

public class MainScene : Scene
{
    public override string SceneName => "Main";

    public Toolbar toolbar = new(WindowManager.window);

    //Behold the fucked list aggregation
    public override List<UIObject?> Children =>
    [
        .. toolbar.uIButtons,
        .. ContextMenu.Instance.Options,
        .. ChunkManager.GetObjectsInVisibleArea(Camera.GetVisibleWorldArea())
            .Where(obj => obj.IsVisible && !obj.IsDisposed).ToList(),
        BreadcrumbNav,
        EntryManager.inputField,
        GroupManager.inputField
    ];

    private RenderBatch EntryRenderBatch;
    private RenderBatch GroupRenderBatch;
    private RenderBatch ImageUIRenderBatch;
    private ConnectionRenderBatch ConnectionRenderBatch;

    private bool _IsPanning = false;
    private bool _isDragging;
    private Vector2 _lastMousePosition;
    private Vector2 _dragStart;
    private UIObject? _mouseDownObject;
    private bool _hasExceededDragThreshold;
    private Breadcrumb BreadcrumbNav = new();

    public MainScene()
    {
#pragma warning disable CS8604 // Possible null reference argument.
        EntryRenderBatch = new(ShaderManager.GetShaderByName("nine-slice-batched"), TextureHandler.GetEmbeddedTextureByName("input_square.png"));
        GroupRenderBatch = new(ShaderManager.GetShaderByName("nine-slice-batched"), TextureHandler.GetEmbeddedTextureByName("input_square.png"));
        ImageUIRenderBatch = new(ShaderManager.GetShaderByName("Batched Texture"));
        ConnectionRenderBatch = new(ShaderManager.GetShaderByName("connection-shader"));
#pragma warning restore CS8604 // Possible null reference argument.

        EntryRenderBatch.NineSliceBorder = new(47, 47);
        GroupRenderBatch.NineSliceBorder = new(47, 47);
    }

    public override void Render()
    {
        TextRenderer.Clear();
        OutlineRender.Clear();

        RectangleF visibleArea = Camera.GetVisibleWorldArea();

        var visibleUnsortedObjects = ChunkManager.GetObjectsInVisibleArea(visibleArea)
            .Where(obj => obj.IsVisible && !obj.IsDisposed);

        var VisibleEntries = visibleUnsortedObjects.Where(x => x is EntryUI).OrderBy(x => x.RenderKey);
        var VisibleGroups = visibleUnsortedObjects.Where(x => x is GroupUI).OrderBy(x => x.RenderKey);
        var VisibleImages = visibleUnsortedObjects.Where(x => x is ImageUI).OrderBy(x => x.RenderKey).ToList();

        VisibleImages.AddRange([.. VisibleGroups.SelectMany(x => x.ChildObjects), .. VisibleImages.SelectMany(x => x.ChildObjects)]);

        List<ConnectionUI> VisibleConnections = [.. visibleUnsortedObjects.Where(x => x is ConnectionUI).Cast<ConnectionUI>()];

        var selectedObjects = SelectionManager.GetSelectedTypeOfObject<UIObject>().Where(x => x.IsDraggable);

        VisibleImages.AddRange(selectedObjects.Where(x => x is ImageUI).OrderBy(x => x.RenderKey));

        EntryRenderBatch.AddObjectsToBatch([.. VisibleEntries.Concat(selectedObjects.Where(x => x is EntryUI).OrderBy(x => x.RenderKey))]);
        GroupRenderBatch.AddObjectsToBatch([.. VisibleGroups.Concat(selectedObjects.Where(x => x is GroupUI).OrderBy(x => x.RenderKey))]);
        ConnectionRenderBatch.AddConnectionsToBatch(VisibleConnections);

        foreach (ConnectionUI obj in VisibleConnections)
        {
            obj.Render();
        }

        GroupRenderBatch.ExecuteBatch();

        var SplitImages = VisibleImages.GroupBy(x => x.Texture).ToList();
        foreach (var splitImage in SplitImages)
        {
            ImageUIRenderBatch.AddObjectsToBatch([.. splitImage]);
            ImageUIRenderBatch.texture = splitImage.Key;
            ImageUIRenderBatch.ExecuteBatch();
        }

        ConnectionRenderBatch.ExecuteBatch();
        EntryRenderBatch.ExecuteBatch();

        OutlineRender.AddOutlineToObjects([.. selectedObjects.Where(x => x is not ResizeHandle)], 16f * Camera.Zoom, Settings.HighlightColor);
        OutlineRender.Draw();

        var hoeverObject = UIobjectHandler.CurrentHoeverTarget;

        if (hoeverObject != null && hoeverObject.IsScreenSpace && hoeverObject is not ResizeHandle)
        {
            DrawMainOverlayObjects();

            OutlineRender.Clear();
            OutlineRender.AddOutlineToObject(hoeverObject, 4f, Settings.HoeverHighlightColor);
            OutlineRender.Draw();
            return;

        }
        else if (hoeverObject != null && hoeverObject is not ResizeHandle)
        {
            OutlineRender.Clear();
            OutlineRender.AddOutlineToObject(hoeverObject, 9f * Camera.Zoom, Settings.HoeverHighlightColor);
            OutlineRender.Draw();

            DrawMainOverlayObjects();
            return;
        }
        DrawMainOverlayObjects();
    }

    private void DrawMainOverlayObjects()
    {
        TextRenderer.Clear();
        MarqueeHandler.MarqueeRect.Render();
        EntryManager.inputField?.Render();
        GroupManager.inputField?.Render();
        BreadcrumbNav.Render();
        TextRenderer.Draw();

        TextRenderer.Clear();
        toolbar.Render();
        ContextMenu.Instance.Render();
        TextRenderer.Draw();
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

    private void HandleObjectSelection(UIObject uIObject)
    {
        SelectionOption option = SelectionOption.NONE;

        if (InputDeviceHandler.IsKeyPressed(Key.ControlLeft))
        {
            option = SelectionOption.EXCLUSIVE_ADD;
        }
        else if (InputDeviceHandler.IsKeyPressed(Key.ShiftLeft))
        {
            option = SelectionOption.EXCLUSIVE_REMOVE;
        }

        SelectionManager.Select(uIObject, option);
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

    public override void UnsubActions()
    {
        EntryManager.inputField?.UnsubActions();
        GroupManager.inputField?.UnsubActions();

        if (InputDeviceHandler.primaryMouse != null)
        {
            InputDeviceHandler.primaryMouse.Scroll -= OnMouseWheel;
            InputDeviceHandler.primaryMouse.MouseDown -= OnMouseDown;
            InputDeviceHandler.primaryMouse.MouseUp -= OnMouseUp;
            InputDeviceHandler.primaryMouse.MouseMove -= OnMouseMove;
            InputDeviceHandler.primaryMouse.DoubleClick -= OnDoubleClick;
        }

        if (InputDeviceHandler.primaryKeyboard != null)
        {
            InputDeviceHandler.primaryKeyboard.KeyDown -= OnKeyInput;
            InputDeviceHandler.primaryKeyboard.KeyUp -= OnKeyobardKeyUp;
        }
        EntryManager.LayerLoaded -= BreadcrumbNav.UpdateCrumbNavigation;
        WindowManager.window.FileDrop -= ImageManager.CreateNewImagesPerWindowDrop;
    }

    public override void SubActions()
    {
        //Camera.ResetView();
        EntryManager.inputField?.SubActions();
        GroupManager.inputField?.SubActions();

        if (InputDeviceHandler.primaryMouse != null)
        {
            InputDeviceHandler.primaryMouse.Scroll += OnMouseWheel;
            InputDeviceHandler.primaryMouse.MouseDown += OnMouseDown;
            InputDeviceHandler.primaryMouse.MouseUp += OnMouseUp;
            InputDeviceHandler.primaryMouse.MouseMove += OnMouseMove;
            InputDeviceHandler.primaryMouse.DoubleClick += OnDoubleClick;
        }

        if (InputDeviceHandler.primaryKeyboard != null)
        {
            InputDeviceHandler.primaryKeyboard.KeyDown += OnKeyInput;
            InputDeviceHandler.primaryKeyboard.KeyUp += OnKeyobardKeyUp;
        }

        toolbar.OnFramebufferResize(Camera.ViewportSize);
        EntryManager.LayerLoaded += BreadcrumbNav.UpdateCrumbNavigation;
        BreadcrumbNav.RecalcSize();
        BreadcrumbNav.UpdateCrumbNavigation(EntryManager.CurrentParentEntry);

        WindowManager.window.FileDrop += ImageManager.CreateNewImagesPerWindowDrop;
    }

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

    public override void Dispose()
    {
        EntryManager.Dispose();
        GroupManager.Dispose();
        ConnectionManager.Dispose();
        ImageManager.Dispose();

        OutlineRender.Dispose();
        EntryRenderBatch.Dispose();
        GroupRenderBatch.Dispose();
        ConnectionRenderBatch.Dispose();
        toolbar.Dispose();

        BreadcrumbNav.Dispose();
        UnsubActions();
    }

    public override void RecalcSize()
    {
        EntryManager.RecalcEntrySizes();
        ConnectionManager.RecalcConnectionSizes();
        BreadcrumbNav.RecalcSize();
    }

    public override void RecalcLayout()
    {
        toolbar.HideSubMenus();
        ContextMenu.Instance.Hide();
        BreadcrumbNav.RecalcSize();
    }
}