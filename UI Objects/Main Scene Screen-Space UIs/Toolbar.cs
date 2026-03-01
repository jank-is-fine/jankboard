using System.Drawing;
using Managers;
using Rendering.UI;
using Silk.NET.Maths;
using Silk.NET.Windowing;

public class Toolbar : UIImage
{
    public List<UIButton> uIButtons = [];
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

        uIButtons.AddRange([ResetView, CreateList, Save, SettingsButton]);
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
            [.. uIButtons.Except([SettingsButton, Save])],
            new(Button_Margin + bounds.Left, bounds.Top + Button_Margin),
            Button_Margin * 2f
        );

        SettingsButton.SetScale(new(maxHeight, maxHeight));
        Save.Transform.Scale = new(maxHeight, maxHeight);

        SettingsButton.Transform.Position = new
        (
            -Button_Margin + bounds.Right - SettingsButton.Transform.Scale.X / 2f,
            bounds.Top + (SettingsButton.Transform.Scale.Y / 2f) + Button_Margin
        );

        Save.Transform.Position = new
        (
            SettingsButton.Bounds.Left - (Save.Transform.Scale.X / 2f) - Button_Margin * 2f,
            SettingsButton.Transform.Position.Y
        );
    }

    public override void Render()
    {
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
        
        foreach(var child in ChildObjects)
        {
            child?.Dispose();
        }
    }
}