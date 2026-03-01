using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;

public class SaveFileButton : UIButton
{
    private string Save;
    private UIButton DeleteButton;

    public SaveFileButton(string TargetSave, Action RefreshAction) : base(" ", [])
    {
        Save = TargetSave;
        var saveName = Path.GetFileNameWithoutExtension(TargetSave);
        SetText($"  {saveName}");
        TextAnchorPoint = TextAnchorPoint.Left_Center;

        void DeleteAction()
        {
            RenderManager.modal.ShowModal
            (
                $"[b]Are you sure you want to delete \"{saveName}\" ?[/b]",
                OnConfirmActions: [() => SaveManager.DeleteSave(saveName), () => RefreshAction?.Invoke()]
            );
        }

        var DeleteIcon = TextureHandler.GetEmbeddedTextureByName("trashcanOpen.png");
        DeleteButton = new
        (
            DeleteIcon == null ? "Delete" : "",
            [DeleteAction],
            nineSlice: DeleteIcon == null
        )
        { GetButtonColorAuto = false };

        //This needs to be here since the button gets a default white texture depending on the nineSlice bool
        if (DeleteIcon != null)
        {
            DeleteButton.Texture = DeleteIcon;
        }

        ChildObjects.Add(DeleteButton);
        RecalcSize();
    }

    public override void OnClick(Vector2 pos)
    {
        SaveManager.LoadSaveFromDisk(Save);
    }

    public override void RecalcSize()
    {
        base.RecalcSize();
        if (DeleteButton == null) { return; }
        if (!DeleteButton._nineSlice)
        {
            DeleteButton.Transform.Scale = new(Transform.Scale.Y, Transform.Scale.Y);
            DeleteButton.TextureColor = TextHelper.GetContrastColor(TextureColor);
        }
        else
        {
            //no texture, using text then
            DeleteButton.RecalcSize();
        }

        Transform.Scale = new(Transform.Scale.X + DeleteButton.Transform.Scale.X, Transform.Scale.Y);
    }

    public void PositionButtons()
    {
        DeleteButton.Transform.Position = new
        (
            Transform.Position.X + (Transform.Scale.X / 2f) - DeleteButton.Transform.Scale.X / 2f,
            Transform.Position.Y
        );

        DeleteButton.TextureColor = TextHelper.GetContrastColor(TextureColor);
        DeleteButton.RenderOrder = RenderOrder + 1;
    }

    public override void Dispose()
    {
        base.Dispose();
        DeleteButton.Dispose();
    }

}