using Managers;

public class DeuteranopeCorrectionRenderPass : RenderPass
{
    private Texture? lut = TextureHandler.GetEmbeddedTextureByName("deuteranope_correct_lut.png");
    
    public DeuteranopeCorrectionRenderPass()
    {
        ScreenShader = ShaderManager.GetShaderByName("DeuteranopeCorrectionShader");
        ActiveConditionGetter = () => Settings.DeuteranopiaFilterActive;
    }

    public override void SetupPass()
    {
        if (!IsActive) { return; }
        base.SetupPass();
        lut?.Bind(textureSlot: Silk.NET.OpenGL.TextureUnit.Texture1);
        ScreenShader?.SetUniform("udeuteranope_lut", 1);
    }
}