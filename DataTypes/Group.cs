using System.Numerics;

public class Group(Guid parent, Vector2 targetPosition)
{
    public bool IsDeleted = false;
    public Guid ParentEntryGuid = parent;
    public Guid guid = Guid.NewGuid();
    public string GroupName = string.Empty;
    public Vector2 position = targetPosition;
    public Vector2 Size = new(100, 100);
    public long SavedRenderKey = 0;
}