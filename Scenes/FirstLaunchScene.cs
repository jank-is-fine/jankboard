using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;

public class FirstLaunchScene : Scene
{
    public override string SceneName => "First Launch";

    private UIImage BackgroundImage;

    private UITextLabel PreferencePanelName;
    private UITextLabel QuestionLabel;

    private UIButton YesButton;
    private UIButton NoButton;

    private UIButton SkipButton;


    public record QuestionNode(string Question, List<Action> Yes, List<Action> No);
    private List<QuestionNode> Questions = [];
    private QuestionNode? CurrentQuestion = null;


    public FirstLaunchScene()
    {
        BackgroundImage = new(null, true)
        {
            TextureColor = Color.Black,
            Transform =
            {
                Scale = new Vector2(Camera.ViewportSize.X,Camera.ViewportSize.Y) * 1.15f,
                Position = new(0,0)
            }
        };

        YesButton = new("Yes", [AnswerYes])
        {
            TextureColor = Color.Black,
            TextColor = Color.White,
        };
        YesButton.RecalcSize();

        NoButton = new("No", [AnswerNo])
        {
            TextureColor = Color.Black,
            TextColor = Color.White,
        };
        NoButton.RecalcSize();

        PreferencePanelName = new("Preferences")
        {
            TextureColor = Color.Black,
        };

        QuestionLabel = new()
        {
            TextureColor = Color.Black
        };


        SkipButton = new("Skip", [Skip])
        {
            TextureColor = Color.Black,
            TextColor = Color.White,
        };

        Children.AddRange([YesButton, NoButton, SkipButton]);
        SetupQuestions();
        ShowQuestion(0);
        RecalcSize();
    }

    private void SetupQuestions()
    {
        Questions = [
             new QuestionNode(
                "Do you have difficulty reading?\nOr do you like to use the current Font?",
                [ShowNextQuestion], // we start off with opendylexic
                [() => ShaderManager.SetCurrentFont("OpenSans"),ShowNextQuestion]
                ),

            new QuestionNode(
                "Do you have any color Weaknesses?",
                [ShowNextQuestion],
                [() => ShowQuestion(6)]
                ),

            new QuestionNode(
                "Are you color blind?",
                [() => Settings.ColorBlindFilterActive = true,() => ShowQuestion(6)],
                [ShowNextQuestion]
                ),

           new QuestionNode(
                "Do you have protanopia",
                [() => Settings.ProtanopiaFilterActive = true,ShowNextQuestion],
                [ShowNextQuestion]
                ),

            new QuestionNode(
                "Do you have deuteranopia",
                [() => Settings.DeuteranopiaFilterActive = true,ShowNextQuestion],
                [ShowNextQuestion]
                ),

            new QuestionNode(
                "Do you have tritanopia",
                [() => Settings.TritanopiaFilterActive = true,ShowNextQuestion],
                [ShowNextQuestion]
                ),

            new QuestionNode(
                "Every Setting can be changed, have fun!\nHamburger Hamburger Hamburger",
                [ShowNextQuestion],
                [ShowNextQuestion]
                ),
        ];
    }


    public override void RecalcLayout()
    {
        RecalcSize();
        YesButton.RecalcSize();
        NoButton.RecalcSize();

        Vector2 viewportSize = new(Camera.ViewportSize.X, Camera.ViewportSize.Y);
        float SegmentHeight = viewportSize.Y / 8;

        SkipButton.Transform.Position = new(SkipButton.Transform.Scale.X / 2 + 15f, SkipButton.Transform.Scale.Y / 2 + 10f);

        Vector2 currentPos = new(viewportSize.X / 2, SegmentHeight);

        PreferencePanelName.Transform.Position = currentPos;
        currentPos.Y += SegmentHeight * 2; //3

        QuestionLabel.Transform.Position = currentPos;
        currentPos.Y += SegmentHeight * 3; //6

        float maxWidth = YesButton.Transform.Scale.X / 2 + NoButton.Transform.Scale.X / 2 + 150; // +50 padding in screenspace

        float midPointLeft = viewportSize.X / 2 - maxWidth / 2;

        YesButton.Transform.Position = new(midPointLeft, currentPos.Y);
        NoButton.Transform.Position = new(midPointLeft + YesButton.Transform.Scale.X + 150, currentPos.Y);
    }

    public override void RecalcSize()
    {
        YesButton.RecalcSize();
        NoButton.RecalcSize();
        PreferencePanelName.RecalcSize();
        SkipButton.RecalcSize();
        BackgroundImage.Transform.Scale = new Vector2(Camera.ViewportSize.X, Camera.ViewportSize.Y) * 1.15f;
        BackgroundImage.Transform.Position = new Vector2(Camera.ViewportSize.X / 2, Camera.ViewportSize.Y / 2);
    }

    public void AnswerYes()
    {
        if (CurrentQuestion == null) { return; }

        foreach (var action in CurrentQuestion.Yes)
        {
            try { action.Invoke(); }
            catch (Exception ex)
            {
                Logger.Log("FirstLaunchScene", $"Yes-action failed: {ex}\nStacktrace: {ex.StackTrace}", LogLevel.ERROR);
                Debug.WriteLine($"Question yes-action failed: {ex}");
                RenderManager.ChangeScene("Main Menu");
            }
        }
    }

    public void AnswerNo()
    {
        if (CurrentQuestion == null) { return; }

        foreach (var action in CurrentQuestion.No)
        {
            try { action.Invoke(); }
            catch (Exception ex)
            {
                Logger.Log("FirstLaunchScene", $"No-action failed: {ex}\nStacktrace: {ex.StackTrace}", LogLevel.ERROR);
                Debug.WriteLine($"Question no-action failed: {ex}");
                RenderManager.ChangeScene("Main Menu");
            }
        }
    }

    public void ShowNextQuestion()
    {
        if (CurrentQuestion == null)
        {
            RenderManager.ChangeScene("Main Menu");
            return;
        }

        var index = Questions.IndexOf(CurrentQuestion);
        ShowQuestion(index + 1);
    }


    public void ShowQuestion(int index)
    {
        if (index < 0 || index >= Questions.Count)
        {
            RenderManager.ChangeScene("Main Menu");
            return;
        }

        if (index == Questions.Count - 1)
        {
            YesButton.SetText("Start");
            NoButton.SetText("Start");

            SaveManager.SaveSettingsToDisk();

            YesButton.RecalcSize();
            NoButton.RecalcSize();
        }

        CurrentQuestion = Questions[index];
        QuestionLabel.SetText(CurrentQuestion.Question);
        QuestionLabel.RecalcSize();
        RecalcLayout();
    }


    public override void Render()
    {
        TextRenderer.Clear();

        BackgroundImage?.Render();
        PreferencePanelName?.Render();
        QuestionLabel?.Render();
        YesButton?.Render();
        NoButton?.Render();
        SkipButton?.Render();

        TextRenderer.Draw();
        TextRenderer.Clear();
    }

    public override void SubActions()
    {
        MouseHandler.Subscribe();
    }

    public override void UnsubActions()
    {
        MouseHandler.Unsubscribe();
        Dispose();
    }

    public void Skip()
    {
        Settings.CurrentFontName = "OpenSans";
        RenderManager.ChangeScene("Main Menu");

        // Not needed anymore, most likely never again
        RenderManager.RemoveScene(this);
    }

    public override void Dispose()
    {
        BackgroundImage?.Dispose();
        PreferencePanelName?.Dispose();
        QuestionLabel?.Dispose();
        YesButton?.Dispose();
        NoButton?.Dispose();
        SkipButton?.Dispose();
    }
}