using Managers;

public class BlackAndWhiteRenderPass : RenderPass
{
    public BlackAndWhiteRenderPass()
    {
        ScreenShader = ShaderManager.GetShaderByName("BlackAndWhiteShader");
        ActiveConditionGetter = () => Settings.ColorBlindFilterActive;
    }

    public override void SetupPass()
    {
        if (!IsActive) { return; }
        base.SetupPass();        
    }
}