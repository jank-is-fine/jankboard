using System.Numerics;

public class ImageData(Guid parent, Vector2 targetPosition)
{
    public bool IsDeleted = false;
    public Guid ParentEntryGuid = parent;
    public Guid guid = Guid.NewGuid();
    public string ImagePath = string.Empty;
    public Vector2 position = targetPosition;
    public Vector2 Size = new(20, 20);
    public long SavedRenderKey = 0;

}