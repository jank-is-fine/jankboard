using System.Drawing;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

public partial class MainScene : Scene
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

        OutlineRender.AddOutlineToObjects([.. selectedObjects.Where(x => x is not ResizeHandle && !x.IsScreenSpace)], 16f * Camera.Zoom, Settings.HighlightColor);
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
        BreadcrumbNav.RecalcSize();
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
}