using Rendering.UI;

public abstract class Scene : IDisposable
{
    public virtual string SceneName {get;} = "";
    public virtual List<UIObject> Children {get; set;} = [];
    public abstract void SubActions();
    public abstract void UnsubActions();
    public abstract void Render();
    public abstract void RecalcSize();
    public abstract void RecalcLayout();
    public abstract void Dispose();
}