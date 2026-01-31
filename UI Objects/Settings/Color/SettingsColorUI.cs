using System.Drawing;
using System.Numerics;
using Rendering.UI;

public class SettingsColorUI : UIButton
{
    public UIImage ColorSwatch { get; private set; }
    public Func<Color> Getter;
    
    public SettingsColorUI(string displayText, Func<Color> getter, bool recalcSize = true, bool screenSpace = true)
        : base(displayText, [], recalcSize, screenSpace)
    {
        ColorSwatch = new UIImage(null, true)
        {
            Transform =
            {
                Scale = new Vector2(30f, 30f)
            }
        };
        IsDraggable = false;
        Getter = getter;
    }

    public void PositionSwatch()
    {
        ColorSwatch.Transform.Position = new Vector2(
           Transform.Position.X + (Transform.Scale.X / 2) + (ColorSwatch.Transform.Scale.X / 2),
           Transform.Position.Y
       );
        ColorSwatch.RenderOrder = RenderOrder + 1;
    }

    public override void Render()
    {
        TextureColor = Settings.ButtonBGColor;
        ColorSwatch.TextureColor = Getter();
        base.Render();
        ColorSwatch.Render();
    }


    public override void Dispose()
    {
        ColorSwatch?.Dispose();
        base.Dispose();
    }
}