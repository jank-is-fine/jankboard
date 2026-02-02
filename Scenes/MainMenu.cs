using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;

public class MainMenuScene : Scene
{
    public override string SceneName => "Main Menu";
    private InputField NewSaveInputfield = null!;
    private UIButton CreateNewSaveButton;
    private UIButton ShowSettingsButton;
    private UIScrollableObjectList SaveList;
    private UITextLabel ErrorLabel = new(textAnchorPoint: TextAnchorPoint.Center_Center, isNineSlice: false);
    private UITextLabel SavesLabel = new(textAnchorPoint: TextAnchorPoint.Center_Center, isNineSlice: false);


    public MainMenuScene()
    {
        CreateNewSaveButton = new
        (
            "Create",
            [CreateNewSave],
            nineSlice: true,
            NineSliceBorder: new(16, 16),
            textAnchorPoint: TextAnchorPoint.Center_Center
        )
        {
            Texture = TextureHandler.GetEmbeddedTextureByName("button_rectangle_line.png")
        };
        CreateNewSaveButton.RecalcSize();
        ErrorLabel.SetText("[b]Please Enter your desired name for the save[/b]");
        SavesLabel.SetText("[b]Saves[/b]");


        ShowSettingsButton = new
        (
            " ",
            [() => RenderManager.ChangeScene("Settings")],
            textAnchorPoint: TextAnchorPoint.Center_Center,
            recalcSize: false,
            nineSlice: false
        )
        {
            Transform =
            {
                Scale = new(64,64)
            },
            Texture = TextureHandler.GetEmbeddedTextureByName("gear.png")
        };

        /* YIQ Testing
        SavesLabel.TextureColor = Color.FromArgb(Random.Shared.Next(128, 255),
                Random.Shared.Next(255), Random.Shared.Next(255), Random.Shared.Next(255));
        */

        NewSaveInputfield = new(nineSlice: true, NineSliceBorder: new(16, 16), "New Save name")
        {
            IsDraggable = false,
            Texture = TextureHandler.GetEmbeddedTextureByName("button_rectangle_line.png")
        };
        NewSaveInputfield.DisallowedCharacters.AddRange(Path.GetInvalidFileNameChars());
        NewSaveInputfield.DisallowedCharacters.Add('\n');
        NewSaveInputfield.MaxCharAmount = 25;



        SaveList = new UIScrollableObjectList(10f)
        {
            TextureColor = Color.FromArgb(128, 143, 143, 143),
            IsScreenSpace = true,
            IsDraggable = false
        };

        Children.AddRange([NewSaveInputfield, CreateNewSaveButton, SaveList, ErrorLabel, SavesLabel, ShowSettingsButton]);

        RefreshSaveFiles();
        RecalcLayout();
    }

    public void RefreshSaveFiles()
    {
        SaveList.ClearList();

        List<string> saves = SaveManager.GetAllSaves();
        foreach (string save in saves)
        {
            var newSaveButton = new UIButton($"  {Path.GetFileNameWithoutExtension(save)}", [() => LoadSave(save)], textAnchorPoint: TextAnchorPoint.Left_Center)
            {
                IsDraggable = false,
            };
            SaveList.AddItem(newSaveButton);
        }

        SaveList.RecalcLayout();
    }

    public override void RecalcLayout()
    {
        var viewportSize = Camera.ViewportSize;
        var marginX = viewportSize.X * Settings.MARGIN_PERCENT;
        var marginY = Math.Max(viewportSize.Y * Settings.MARGIN_PERCENT, 50f);

        ErrorLabel.RecalcSize();
        ErrorLabel.Transform.Scale = new(viewportSize.X - marginX * 2f, ErrorLabel.Transform.Scale.Y);
        ErrorLabel.Transform.Position = new(
            marginX + ErrorLabel.Transform.Scale.X / 2f,
            marginY + ErrorLabel.Transform.Scale.Y / 4f + 5f
        );

        marginY += ErrorLabel.Transform.Scale.Y;
        CreateNewSaveButton.RecalcSize();

        NewSaveInputfield.MinWidth = viewportSize.X - marginX * 2f - CreateNewSaveButton.Transform.Scale.X - 10f;
        NewSaveInputfield.RecalcSize();

        CreateNewSaveButton.SetScale(new(CreateNewSaveButton.Transform.Scale.X, NewSaveInputfield.Transform.Scale.Y));

        NewSaveInputfield.Transform.Position = Camera.ScreenToWorld(new Vector2(
            marginX + NewSaveInputfield.Transform.Scale.X / 2f,
            marginY + NewSaveInputfield.Transform.Scale.Y / 2f
        ));

        marginY += NewSaveInputfield.Transform.Scale.Y;

        CreateNewSaveButton.Transform.Position = new(
            marginX + NewSaveInputfield.Transform.Scale.X + CreateNewSaveButton.Transform.Scale.X / 2f + 10f,
            Camera.WorldToScreen(NewSaveInputfield.Transform.Position).Y
        );

        marginY += CreateNewSaveButton.Transform.Scale.Y / 2f;

        SavesLabel.RecalcSize();
        marginY += SavesLabel.Transform.Scale.Y;
        marginY += 20f;


        SaveList.Transform.Scale = new(viewportSize.X - marginX * 2f,
                                       viewportSize.Y - marginY - (viewportSize.Y * Settings.MARGIN_PERCENT));

        SaveList.Transform.Position = new(
            marginX + SaveList.Transform.Scale.X / 2f,
           marginY + (SaveList.Transform.Scale.Y / 2f) - 5f
        );

        SavesLabel.Transform.Position = new(
           SaveList.Transform.Position.X - (SaveList.Transform.Scale.X / 2f) + (SavesLabel.Transform.Scale.X / 2f),
           SaveList.Transform.Position.Y - (SavesLabel.Transform.Scale.Y / 2f) - (SaveList.Transform.Scale.Y / 2f) - 5f);

        SaveList.RecalcLayout();

        ShowSettingsButton.Transform.Scale = new(32f, 32f);
        ShowSettingsButton.Transform.Position = new(
            viewportSize.X - 12f - ShowSettingsButton.Transform.Scale.X / 2f,
            12f + ShowSettingsButton.Transform.Scale.Y / 2f
        );
    }

    public void LoadSave(string save)
    {
        SaveManager.LoadFromDisk(save);
        RenderManager.ChangeScene("Main");
    }

    public void CreateNewSave()
    {
        string TargetNameString = NewSaveInputfield.Content.Trim();
        if (string.IsNullOrEmpty(TargetNameString) ||
            SaveManager.DoesSaveExist(TargetNameString) ||
            Path.GetInvalidFileNameChars().Any(TargetNameString.Contains))
        {
            // Show Error - invalid filename
            ShowNewSaveInputError();
            return;
        }

        Save newSave = new()
        {
            SaveName = TargetNameString,
            SavePath = $"{SaveManager.SaveFolder}/{TargetNameString}.json"
        };
        SaveManager.LoadFromSave(newSave);
        RenderManager.ChangeScene("Main");
    }

    public void NewSaveInputChanged(string newString)
    {
        if (string.IsNullOrEmpty(newString) ||
            SaveManager.DoesSaveExist(newString) ||
            Path.GetInvalidFileNameChars().Any(newString.Contains))
        {
            ShowNewSaveInputError();
        }
        else
        {
            RestoreNewSaveInputError();
        }
    }

    private void ShowNewSaveInputError()
    {
        if (string.IsNullOrEmpty(NewSaveInputfield.Content))
        {
            RestoreNewSaveInputError();
        }
        else
        {
            NewSaveInputfield.TextureColor = Color.FromArgb(255, 255, 200, 200);
            ErrorLabel.TextureColor = Color.FromArgb(255, 255, 200, 200);
            ErrorLabel.SetText("[b]Save already exists[/b]");
        }
    }

    private void RestoreNewSaveInputError()
    {
        NewSaveInputfield.TextureColor = Settings.InputfieldBackgroundColor;
        ErrorLabel.TextureColor = Settings.TextLabelBackgroundColor;
        ErrorLabel.SetText("[b]Please Enter your desired name for the save[/b]");
    }

    public override void Render()
    {
        TextRenderer.Clear();
        foreach (var obj in Children)
        {
            if (obj == null) { continue; }
            obj.Render();
            foreach (var child in UIobjectHandler.GetAllChildren(obj).Where(x => x.IsVisible))
            {
                child.Render();
            }
        }
        TextRenderer.Draw();

        var hoeverObject = UIobjectHandler.CurrentHoeverTarget;

        if (hoeverObject != null && hoeverObject.IsScreenSpace)
        {
            if (hoeverObject is UIButton)
            {
                OutlineRender.Clear();
                OutlineRender.AddOutlineToObject(hoeverObject, 4f, Settings.HoeverHighlightColor);
                OutlineRender.Draw();
            }
        }
    }

    public override void UnsubActions()
    {
        MouseHandler.Unsubscribe();
        SaveList.UnsubActions();
        NewSaveInputfield.UnsubActions();
        NewSaveInputfield.ContentChanged -= NewSaveInputChanged;
        NewSaveInputfield.SubmitAction -= CreateNewSave;
    }

    public override void SubActions()
    {
        MouseHandler.Subscribe();
        Camera.ResetView();
        SaveList.SubActions();
        NewSaveInputfield.SubActions();
        NewSaveInputfield.ContentChanged += NewSaveInputChanged;
        NewSaveInputfield.SubmitAction += CreateNewSave;
        RefreshSaveFiles();
        RecalcLayout();
    }

    public override void Dispose()
    {
        foreach (var obj in Children)
        {
            if (obj == null) { continue; }
            obj.Dispose();
        }
        UnsubActions();
    }

    public override void RecalcSize()
    {
        RecalcLayout();
    }
}