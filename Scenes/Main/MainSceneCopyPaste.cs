using System.Numerics;
using Managers;
using Rendering.UI;

public partial class MainScene : Scene
{
    private List<object>? CopyList = null;
    private Vector2? LastCopyPos = null;

    private void CopySelectedData()
    {
        var SelectedEntryUIs = SelectionManager.GetSelectedTypeOfObject<EntryUI>();
        var SelectedGroupUIs = SelectionManager.GetSelectedTypeOfObject<GroupUI>();
        var SelectedImageUIs = SelectionManager.GetSelectedTypeOfObject<ImageUI>();
        var SelectedConnectionUIs = SelectionManager.GetSelectedTypeOfObject<ConnectionUI>();

        CopyList =
        [
            .. SelectedEntryUIs.Select(x => x.ReferenceEntry).ToList(),
            .. SelectedGroupUIs.Select(x => x.ReferenceGroup).ToList(),
            .. SelectedImageUIs.Select(x => x.ReferenceImage).ToList(),
            .. SelectedConnectionUIs.Select(x => x.ReferenceConnection).ToList()
        ];

        LastCopyPos = Camera.Position;
    }

    private void PasteCopiedData()
    {
        if (CopyList == null || CopyList.Count == 0) return;

        var currentRenderKey = SelectionManager.GlobalRenderCounter++;
        var currentParent = EntryManager.CurrentParentEntry;
        var offset = Vector2.One * 10f;
        Vector2 CameraPosOffset = LastCopyPos.HasValue ? Camera.Position - LastCopyPos.Value : Vector2.Zero;

        var sourceEntries = CopyList.OfType<Entry>().ToList();
        var sourceGroups = CopyList.OfType<Group>().ToList();
        var sourceImages = CopyList.OfType<ImageData>().ToList();
        var sourceConnections = CopyList.OfType<Connection>().ToList();

        var guidMap = new Dictionary<Guid, Guid>();

        var clonedEntries = CloneEntries(sourceEntries, currentParent, offset, currentRenderKey, guidMap, CameraPosOffset);
        var clonedConnections = CloneConnections(sourceConnections, currentParent, currentRenderKey, guidMap);

        var clonedGroups = CloneGroups(sourceGroups, currentParent, offset, currentRenderKey, CameraPosOffset);
        var clonedImages = CloneImages(sourceImages, currentParent, offset, currentRenderKey, CameraPosOffset);

        EntryManager.AddEntries(clonedEntries);
        GroupManager.AddGroups(clonedGroups);
        ImageManager.AddImages(clonedImages);
        ConnectionManager.AddConnections(clonedConnections);

        var CreatedEntryUIs = EntryManager.CreateNewEntryUIs(clonedEntries);
        var CreatedGroupUIs = GroupManager.CreateNewGroupUIs(clonedGroups);
        var CreatedImageUIs = ImageManager.CreateNewImageUIs(clonedImages);
        var CreatedConnectionUIs = ConnectionManager.CreateNewConnectionUIs(clonedConnections);

        RegisterUndoRedo(clonedEntries, clonedGroups, clonedImages, clonedConnections);
        SelectionManager.Select([.. CreatedEntryUIs,.. CreatedGroupUIs,.. CreatedImageUIs,.. CreatedConnectionUIs], SelectionOption.NONE);
    }

    private void DuplicateSelectedData()
    {
        CopySelectedData();
        PasteCopiedData();
    }

    private List<Entry> CloneEntries(List<Entry> source, Guid parent, Vector2 offset, long renderKey, Dictionary<Guid, Guid> guidMap, Vector2 CameraOffset)
    {
        List<Entry> cloned = [];

        foreach (var entry in source)
        {
            var clone = new Entry(parent, entry.position + offset + CameraOffset)
            {
                Content = entry.Content,
                mark = entry.mark,
                SavedRenderKey = renderKey
            };

            guidMap[entry.guid] = clone.guid;
            cloned.Add(clone);
        }

        return cloned;
    }

    private List<Group> CloneGroups(List<Group> source, Guid parent, Vector2 offset, long renderKey, Vector2 CameraOffset)
    {
        List<Group> cloned = [];

        foreach (var group in source)
        {
            var clone = new Group(parent, group.position + offset + CameraOffset)
            {
                GroupName = group.GroupName,
                Size = group.Size,
                SavedRenderKey = renderKey
            };

            cloned.Add(clone);
        }

        return cloned;
    }

    private List<ImageData> CloneImages(List<ImageData> source, Guid parent, Vector2 offset, long renderKey, Vector2 CameraOffset)
    {
        List<ImageData> cloned = [];

        foreach (var image in source)
        {
            var clone = new ImageData(parent, image.position + offset + CameraOffset)
            {
                ImagePath = image.ImagePath,
                Size = image.Size,
                SavedRenderKey = renderKey
            };

            cloned.Add(clone);
        }

        return cloned;
    }

    private List<Connection> CloneConnections(List<Connection> source, Guid parent, long renderKey, Dictionary<Guid, Guid> guidMap)
    {
        List<Connection> cloned = [];

        foreach (var conn in source)
        {
            if (!guidMap.TryGetValue(conn.SourceEntry, out var newSource) ||
                !guidMap.TryGetValue(conn.TargetEntry, out var newTarget))
            {
                continue;
            }

            var clone = new Connection(parent, newSource, newTarget)
            {
                arrowType = conn.arrowType,
                SavedRenderKey = renderKey
            };

            cloned.Add(clone);
        }

        return cloned;
    }

    private void RegisterUndoRedo(
        List<Entry> entries,
        List<Group> groups,
        List<ImageData> images,
        List<Connection> connections)
    {
        var entryGuids = entries.Select(x => x.guid).ToList();
        var groupGuids = groups.Select(x => x.guid).ToList();
        var imageGuids = images.Select(x => x.guid).ToList();
        var connectionGuids = connections.Select(x => x.guid).ToList();

        void undoEntries() => EntryManager.MarkEntriesAsDeleted(entryGuids);
        void undoGroups() => GroupManager.MarkGroupsAsDeleted(groupGuids);
        void undoImages() => ImageManager.MarkImagesAsDeleted(imageGuids);
        void undoConnections() => ConnectionManager.MarkConnectionsAsDeleted(connectionGuids);

        void redoEntries() => EntryManager.UnmarkEntriesAsDeleted(entryGuids);
        void redoGroups() => GroupManager.UnmarkGroupsAsDeleted(groupGuids);
        void redoImages() => ImageManager.UnmarkImagesAsDeleted(imageGuids);
        void redoConnections() => ConnectionManager.UnmarkConnectionsAsDeleted(connectionGuids);

        Action cleanupEntries = EntryManager.CleanUpDeletedEntries;
        Action cleanupGroups = GroupManager.CleanUpDeletedGroups;
        Action cleanupImages = ImageManager.CleanUpDeletedImages;
        Action cleanupConnections = ConnectionManager.CleanUpDeletedConnections;

        var undoRedoAction = new UndoRedoAction(
            undoActions: [undoEntries, undoGroups, undoImages, undoConnections],
            redoActions: [redoEntries, redoGroups, redoImages, redoConnections],
            PopActions: [cleanupEntries, cleanupGroups, cleanupImages, cleanupConnections]
        );

        UndoRedoManager.ActionExecuted(undoRedoAction);
    }
}