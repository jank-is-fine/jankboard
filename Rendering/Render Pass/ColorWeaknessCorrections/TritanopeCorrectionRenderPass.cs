using Managers;

public class TritanopeCorrectionRenderPass : RenderPass
{
    private Texture? lut = TextureHandler.GetEmbeddedTextureByName("tritanope_correct_lut.png");
    
    public TritanopeCorrectionRenderPass()
    {
        ScreenShader = ShaderManager.GetShaderByName("TritanopeCorrectionShader");
        ActiveConditionGetter = () => Settings.TritanopiaFilterActive;
    }

    public override void SetupPass()
    {
        if (!IsActive) { return; }
        base.SetupPass();        
        lut?.Bind(textureSlot: Silk.NET.OpenGL.TextureUnit.Texture1);
        ScreenShader?.SetUniform("utritanope_lut", 1);
    }
}