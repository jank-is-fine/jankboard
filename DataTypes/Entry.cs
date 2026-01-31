using System.Numerics;

public class Entry(Guid parent, Vector2 targetPosition)
{
    public bool IsDeleted = false;
    public Guid ParentEntryGuid = parent;
    public Guid guid = Guid.NewGuid();
    public string Content = string.Empty;
    public Vector2 position = targetPosition;
    public EntryMark mark = EntryMark.NONE;
    public long SavedRenderKey = 0;
}

public enum EntryMark
{
    NONE = 0,
    PRIORITY = 1,
    DONE = 2,
    DROPPED = 3
}