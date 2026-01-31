public class Connection(Guid parentGuid, Guid source, Guid target)
{
    public bool IsDeleted = false;
    public Guid guid = Guid.NewGuid();
    public Guid ParentEntryGuid = parentGuid;
    public ArrowType arrowType = 0;
    public Guid SourceEntry = source;
    public Guid TargetEntry = target;
    public long SavedRenderKey = 0;
}

public enum ArrowType
{
    Default = 0,
    Loose = 1,
    LooseDiagonal = 2,
}