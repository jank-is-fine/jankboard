public abstract class RenderPass
{
    public Shader? ScreenShader;
    public uint? frameBuffer;
    public uint? colorTexture;
    public Func<bool>? ActiveConditionGetter;
    public bool IsActive => ActiveConditionGetter != null && ActiveConditionGetter();

    public virtual void SetupPass()
    {
        if (!IsActive) { return; }
        ScreenShader?.Use();
    }
}