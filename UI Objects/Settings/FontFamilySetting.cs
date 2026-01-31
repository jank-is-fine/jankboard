using Managers;
using Rendering.UI;

public class FontFamilySetting : UIButtonList
{
    public FontFamilySetting(TextAnchorPoint _textAnchorPoint = TextAnchorPoint.Center_Center) 
    : base("[b]Current Font[/b]", isScreenSpace: true, ImmidieteClose: true, [], false,textAnchorPoint: _textAnchorPoint)
    {
        AdjustTextColor = true;
        var fonts = ShaderManager.FontHandler.GetAllFonts();

        foreach (var font in fonts)
        {
            UIButton uIButton = 
            new
            (
                $" [b]{font}[/b]",
                [
                    () => ShaderManager.SetCurrentFont(font),
                    WindowManager.RecalcSize,
                ],
                textAnchorPoint: TextAnchorPoint.Left_Top
            )
            {
                AdjustTextColor = true
            };

            AddOptionButton(uIButton);
        }
    }
}