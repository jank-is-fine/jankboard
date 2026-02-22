using System.Drawing;
using System.Numerics;
using Rendering.UI;

public class ConfirmationModal
: UIImage
{
    private UITextLabel QuestionLabel = new(isNineSlice: false) { GetBGColorAuto = false };
    private UIButton ConfirmButton = new("Yes", []);
    private UIButton CancelButton = new("Cancel", []);

    public ConfirmationModal() : base(nineSlice: true, screenSpace: true)
    {
        ChildObjects.AddRange([QuestionLabel, ConfirmButton, CancelButton]);
        TextureColor = Settings.HighlightColor;
        IsVisible = false;
        IsDraggable = false;
        IsSelectable = true;
    }

    public void ShowModal(string Question, List<Action> OnConfirmActions, List<Action>? OnCancelActions = null, string? ConfirmText = null, string? CancelText = null)
    {
        QuestionLabel.TextureColor = TextureColor;
        QuestionLabel.SetText(Question);

        if (ConfirmText != null) { ConfirmButton.SetText(ConfirmText); }
        if (CancelText != null) { CancelButton.SetText(CancelText); }

        if (OnCancelActions != null)
        {
            CancelButton.actions = OnCancelActions;
        }
        else
        {
            CancelButton.actions.Clear();
        }

        ConfirmButton.actions = OnConfirmActions;

        CancelButton.actions.Add(HideModal);
        ConfirmButton.actions.Add(HideModal);

        ConfirmButton.RenderOrder = RenderOrder + 1;
        CancelButton.RenderOrder = RenderOrder + 1;


        RecalcSize();
        IsVisible = true;
    }

    private void ClearChildren()
    {
        foreach (var child in ChildObjects)
        {
            child.Dispose();
        }

        ChildObjects.Clear();
    }

    public void HideModal()
    {
        IsVisible = false;
    }

    public override void RecalcSize()
    {
        foreach (var child in ChildObjects)
        {
            child.RecalcSize();
        }

        Vector2 MinSizeNeeded = Vector2.Zero;
        MinSizeNeeded.Y += QuestionLabel.Transform.Scale.Y;
        MinSizeNeeded.Y += ConfirmButton.Transform.Scale.Y;
        MinSizeNeeded.Y += CancelButton.Transform.Scale.Y;

        MinSizeNeeded.X += ConfirmButton.Transform.Scale.X + CancelButton.Transform.Scale.X;
        MinSizeNeeded.X = float.Max(MinSizeNeeded.X, QuestionLabel.Transform.Scale.X);

        MinSizeNeeded.X *= 1.1f; // 10% padding, 5 on each side
        MinSizeNeeded.Y *= 1.1f; // 10% padding again

        Transform.Scale = MinSizeNeeded;

        Transform.Position = new(Camera.ViewportSize.X / 2f, Camera.ViewportSize.Y / 2f);

        QuestionLabel.Transform.Position = new
        (
            Transform.Position.X,
            Transform.Position.Y - Transform.Scale.Y / 2f * 0.95f + (QuestionLabel.Transform.Scale.Y / 2f)
        );

        ConfirmButton.Transform.Position = new
        (
            Transform.Position.X + (Transform.Scale.X / 2f * 0.9f) - ConfirmButton.Transform.Scale.X / 2f,
            Transform.Position.Y + (Transform.Scale.Y / 2f * 0.9f) - ConfirmButton.Transform.Scale.Y / 2f
        );

        CancelButton.Transform.Position = new
        (
            Transform.Position.X - (Transform.Scale.X / 2f * 0.9f) + CancelButton.Transform.Scale.X / 2f,
            Transform.Position.Y + (Transform.Scale.Y / 2f * 0.9f) - CancelButton.Transform.Scale.Y / 2f
        );
    }

    public override void Render()
    {
        if (!IsVisible) { return; }
        base.Render();
        foreach (var child in ChildObjects)
        {
            child.Render();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        ClearChildren();
    }
}