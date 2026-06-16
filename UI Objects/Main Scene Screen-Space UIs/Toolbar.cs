using System.Drawing;
using Managers;
using Rendering.UI;
using Silk.NET.Maths;
using Silk.NET.Windowing;

public class Toolbar : UIImage
{
    private bool minimized = false;
    private List<UIButton> AlluIButtons = [];
    public List<UIButton> uIButtons => minimized ? [StateToggle] : AlluIButtons;
    float maxHeight = 0;
    private const float Button_Margin = 5f;


    #region Toolbar Buttons

    UIButton CreateNewEntry = new
    (
        "[b]Entry[/b]",
        [() => EntryManager.CreateNewEntry(null)],
        textAnchorPoint: TextAnchorPoint.Center_Center
    )
    { IsDraggable = false };

    UIButton CreateNewGroup = new
    (
        "[b]Group[/b]",
        [() => GroupManager.CreateNewGroup(null)],
        textAnchorPoint: TextAnchorPoint.Center_Center
    )
    { IsDraggable = false };

    UIButton CreateNewImages = new
    (
        "[b]Image/GIF[/b]",
        [() => ImageManager.CreateNewImages(FileBrowserManager.OpenFileBrowser([Settings.ImageOrGifFilter]))],
        textAnchorPoint: TextAnchorPoint.Center_Center
    )
    { IsDraggable = false };


    UIButton ResetView = new
    (
        "[b]Reset View[/b]",
        [Camera.ResetView],
        textAnchorPoint: TextAnchorPoint.Center_Center
    )
    {
        IsDraggable = false,
        IsVisible = true,
        IsScreenSpace = true,
        RenderOrder = 51
    };


    UIButton Save = new
    (
        " ",
        [() => SaveManager.SaveToFile()],
        nineSlice: false,
        recalcSize: false
    )
    {
        IsScreenSpace = true,
        IsDraggable = false,
        RenderOrder = 51,
        Texture = TextureHandler.GetEmbeddedTextureByName("save.png"),
        Transform =
        {
            Scale = new(32f,32f)
        }
    };

    UIButton SettingsButton = new
    (
        " ",
        [() => RenderManager.ChangeScene("Settings")],
        nineSlice: false,
        recalcSize: false
    )
    {
        IsScreenSpace = true,
        IsDraggable = false,
        RenderOrder = 51,
        Texture = TextureHandler.GetEmbeddedTextureByName("gear.png"),
        Transform =
        {
            Scale = new(32f,32f)
        }
    };

    Texture? MinimizeTexture = TextureHandler.GetEmbeddedTextureByName("check_square_grey_cross-edited.png");
    Texture? MaximizeTexture = TextureHandler.GetEmbeddedTextureByName("down.png");
    UIButton StateToggle;


    UIButtonList CreateList;

    #endregion Toolbar Buttons

    public Toolbar(IWindow window) : base(nineSlice: true)
    {
        IsSelectable = false;
        IsVisible = true;
        RenderOrder = 50;


        CreateList = new("[b]Create[/b]", true, true, [CreateNewEntry, CreateNewGroup, CreateNewImages], false, textAnchorPoint: TextAnchorPoint.Center_Center)
        {
            IsDraggable = false,
            IsVisible = true,
            IsScreenSpace = true,
            RenderOrder = 51
        };

        StateToggle = new
        (
            " ",
            [],
            nineSlice: false,
            recalcSize: false
        )
        {
            IsScreenSpace = true,
            IsDraggable = false,
            RenderOrder = 51,
            Texture = MinimizeTexture,
            Transform =
            {
                Scale = new(32f,32f)
            }
        };

        StateToggle.actions.AddRange
        (
            [
                () => minimized = !minimized,
                () => StateToggle.Texture = minimized ? MaximizeTexture : MinimizeTexture
            ]
        );

        uIButtons.AddRange([ResetView, CreateList, Save, SettingsButton, StateToggle]);
        window.FramebufferResize += OnFramebufferResize;
        IsScreenSpace = true;
        TextureColor = Color.Gray;

        OnFramebufferResize(Camera.ViewportSize);
    }

    public void OnFramebufferResize(Vector2D<int> d)
    {
        foreach (var button in uIButtons)
        {
            button.RecalcSize();
            if (maxHeight < button.Transform.Scale.Y) { maxHeight = button.Transform.Scale.Y; }
        }

        var viewportSize = Camera.ViewportSize;
        var marginX = viewportSize.X * Settings.MARGIN_PERCENT;
        var marginY = Math.Max(viewportSize.Y * Settings.MARGIN_PERCENT, 50f);

        Transform.Scale = new(Camera.ViewportSize.X - marginX, maxHeight + Button_Margin * 2f);
        Transform.Position = new(Camera.ViewportSize.X / 2f, marginY + Button_Margin * 1.5f);

        var bounds = Bounds;

        foreach (var button in uIButtons.Except([SettingsButton, Save]))
        {
            button.SetScale(new(button.Transform.Scale.X, maxHeight));
        }

        LayoutHelper.Horizontal
        (
            [.. uIButtons.Except([SettingsButton, Save, StateToggle])],
            new(Button_Margin + bounds.Left, bounds.Top + Button_Margin),
            Button_Margin * 2f
        );

        SettingsButton.SetScale(new(maxHeight, maxHeight));
        StateToggle.SetScale(new(maxHeight, maxHeight));
        Save.Transform.Scale = new(maxHeight, maxHeight);

        LayoutHelper.HorizontalReverse
        (
            [StateToggle, SettingsButton, Save],
            new(bounds.Right - Button_Margin, bounds.Top + Button_Margin),
            Button_Margin * 2f
        );
    }

    public override void Render()
    {
        if (minimized)
        {
            StateToggle.Render();
            return;
        }

        TextureColor = Settings.ToolbarBackgroundColor;

        base.Render();

        foreach (var btn in uIButtons)
        {
            btn.Render();
        }
    }

    public void HideSubMenus()
    {
        foreach (var btn in uIButtons)
        {
            if (btn is UIButtonList uIListButton)
            {
                uIListButton.CloseList();
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        foreach (var child in ChildObjects)
        {
            child?.Dispose();
        }
    }
}