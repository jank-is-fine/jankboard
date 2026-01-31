using Managers;

/// <summary>
/// <para>Final pass to display the rendered texture/last framebuffer on screen</para>
/// </summary>

public class FinalPass : RenderPass
{
    public FinalPass()
    {
        ScreenShader = ShaderManager.GetShaderByName("FinalPassShader");
        frameBuffer = 0;
        ActiveConditionGetter = () => true;
    }

    public override void SetupPass()
    {
        if (!IsActive) { return; }
        base.SetupPass();        
    }
}