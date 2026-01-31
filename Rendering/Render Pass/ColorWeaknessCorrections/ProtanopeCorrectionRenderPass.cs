using Managers;

public class ProtanopeCorrectionRenderPass : RenderPass
{
    private Texture? lut = TextureHandler.GetEmbeddedTextureByName("protanope_correct_lut.png");

    public ProtanopeCorrectionRenderPass()
    {
        ScreenShader = ShaderManager.GetShaderByName("ProtanopeCorrectionShader");
        ActiveConditionGetter = () => Settings.ProtanopiaFilterActive;
    }

    public override void SetupPass()
    {
        if (!IsActive) { return; }
        base.SetupPass();        
        lut?.Bind(textureSlot: Silk.NET.OpenGL.TextureUnit.Texture1);
        ScreenShader?.SetUniform("uprotanope_lut", 1);
    }
}