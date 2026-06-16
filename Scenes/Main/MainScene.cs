using Managers;
using Rendering.UI;
using Silk.NET.Input;

public partial class MainScene : Scene
{
    public override string SceneName => "Main";

    public Toolbar toolbar = new(WindowManager.window);

    private List<UIObject?> _temp = new(2048);
    public override List<UIObject?> Children => _temp;
    private RenderBatch EntryRenderBatch;
    private RenderBatch GroupRenderBatch;
    private RenderBatch ImageUIRenderBatch;
    private ConnectionRenderBatch ConnectionRenderBatch;

    private Breadcrumb BreadcrumbNav = new();

    public MainScene()
    {
        //these should exist, else ShaderManager would've thrown already
        var nineSliceShader = ShaderManager.GetShaderByName("nine-slice-batched");
        var batchedTextureShader = ShaderManager.GetShaderByName("Batched Texture");
        var connectionShader = ShaderManager.GetShaderByName("connection-shader");
        var nineSliceTex = TextureHandler.GetEmbeddedTextureByName("input_square.png");

        EntryRenderBatch = new(nineSliceShader, nineSliceTex);
        GroupRenderBatch = new(nineSliceShader, nineSliceTex);
        ImageUIRenderBatch = new(batchedTextureShader);
        ConnectionRenderBatch = new(connectionShader);


        EntryRenderBatch.NineSliceBorder = new(47, 47);
        GroupRenderBatch.NineSliceBorder = new(47, 47);
    }

    private void Update(double deltaTime)
    {
        _temp.Clear();
        _temp.AddRange(toolbar.uIButtons);
        _temp.AddRange(ContextMenu.Instance.Options);

        _temp.Add(BreadcrumbNav);
        if (BreadcrumbNav.IsVisible)
        {
            _temp.Add(BreadcrumbNav.StateToggle);
        }
        _temp.AddRange(BreadcrumbNav.BreadCrumbs);

        _temp.Add(EntryManager.inputField);
        _temp.Add(GroupManager.inputField);
    }

    private List<UIObject> _tempVisibleUnsortedObjects = new(2048);
    private List<UIObject> _tempVisibleEntries = new(2048);
    private List<UIObject> _tempVisibleGroups = new(2048);
    private List<UIObject> _tempVisibleImages = new(2048);
    private List<IGrouping<Texture?, UIObject>> _tempSplitImages = new(2048);
    private List<UIObject> _tempSelectedObjects = new(2048);
    private List<ConnectionUI> _tempVisibleConnections = new(2048);

    private List<UIObject> _tempChildren = new(2048);

    public override void Render()
    {
        TextRenderer.Clear();
        OutlineRender.Clear();

        _tempVisibleUnsortedObjects.Clear();
        _tempVisibleEntries.Clear();
        _tempVisibleGroups.Clear();
        _tempVisibleImages.Clear();
        _tempSplitImages.Clear();
        _tempSelectedObjects.Clear();
        _tempVisibleConnections.Clear();
        _tempChildren.Clear();

        _tempVisibleUnsortedObjects.AddRange(UIobjectHandler.GetVisibleObjects());

        _tempVisibleEntries.AddRange(_tempVisibleUnsortedObjects.Where(x => x is EntryUI).OrderBy(x => x.RenderKey));
        _tempVisibleGroups.AddRange(_tempVisibleUnsortedObjects.Where(x => x is GroupUI).OrderBy(x => x.RenderKey));
        _tempVisibleImages.AddRange(_tempVisibleUnsortedObjects.Where(x => x is ImageUI).OrderBy(x => x.RenderKey));

        _tempChildren.AddRange(_tempVisibleUnsortedObjects.Where(x => x is not ConnectionUI).SelectMany(x => x.ChildObjects));
        _tempVisibleImages.AddRange(_tempChildren);

        _tempVisibleConnections.AddRange(_tempVisibleUnsortedObjects.Where(x => x is ConnectionUI).Cast<ConnectionUI>());

        _tempSelectedObjects.AddRange
        (
            SelectionManager.GetSelectedTypeOfObject<UIObject>().Where
            (
                x => x.IsDraggable &&
                x.IntersectsWithRect(Camera.GetVisibleWorldArea()) &&
                x is not ResizeHandle
            )
        );

        EntryRenderBatch.AddObjectsToBatch(_tempVisibleEntries.Where(x => x is EntryUI).OrderBy(x => x.RenderKey).Distinct());
        GroupRenderBatch.AddObjectsToBatch(_tempVisibleGroups.Where(x => x is GroupUI).OrderBy(x => x.RenderKey).Distinct());

        GroupRenderBatch.ExecuteBatch();


        _tempSplitImages.AddRange(_tempVisibleImages.GroupBy(x => x.Texture));
        foreach (var splitImage in _tempSplitImages)
        {
            ImageUIRenderBatch.AddObjectsToBatch(splitImage.Distinct());
            ImageUIRenderBatch.texture = splitImage.Key;
            ImageUIRenderBatch.ExecuteBatch();
        }

        ConnectionRenderBatch.AddConnectionsToBatch(_tempVisibleConnections);
        ConnectionRenderBatch.ExecuteBatch();

        EntryRenderBatch.ExecuteBatch();

        OutlineRender.AddOutlineToObjects(_tempSelectedObjects.Where(x => !x.IsScreenSpace), 16f * Camera.Zoom, Settings.HighlightColor);
        OutlineRender.Draw();


        if (UIobjectHandler.CurrentHoeverTarget != null && UIobjectHandler.CurrentHoeverTarget is not ResizeHandle)
        {
            if (UIobjectHandler.CurrentHoeverTarget.IsScreenSpace)
            {
                DrawMainOverlayObjects();

                OutlineRender.Clear();
                OutlineRender.AddOutlineToObject(UIobjectHandler.CurrentHoeverTarget, 4f, Settings.HoeverHighlightColor);
                OutlineRender.Draw();
                return;
            }
            else
            {
                OutlineRender.Clear();
                OutlineRender.AddOutlineToObject(UIobjectHandler.CurrentHoeverTarget, 9f * Camera.Zoom, Settings.HoeverHighlightColor);
                OutlineRender.Draw();

                DrawMainOverlayObjects();
                return;
            }
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

        TextRenderer.Clear();
        RenderManager.modal.Render();
        TextRenderer.Draw();
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
    }


    public override void UnsubActions()
    {
        UndoRedoManager.Clear(ExecutePopActions: false);
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
        WindowManager.window.Update -= Update;
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
        WindowManager.window.Update += Update;
    }
}