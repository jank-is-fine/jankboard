using System.Numerics;
using Rendering.UI;

namespace Managers
{
    /// <summary>
    /// <para>Manages Entries and EntryUIs and holds the reference for the current Parent Entry Guid</para>
    /// <para>The coordinator for loading of layers; calls the "LoadLayer" methods in GroupManager, ImageManager and ConnectionManager</para>
    /// </summary>

    public static class EntryManager
    {
        public static Guid CurrentParentEntry;
        private static Dictionary<Guid, (Entry, EntryUI?)> entries = [];
        public static List<Entry> GetAllEntries => [.. entries.Where(x => !x.Value.Item1.IsDeleted).Select(x => x.Value.Item1)];
        public static EntryUI? CurrentlyEditingEntry { get; private set; } = null;
        public static bool IsEditing => CurrentlyEditingEntry != null;
        public static Action<Guid>? LayerLoaded;
        private static bool LayerLoadingError = false;

        public static InputField inputField = new()
        {
            TextureColor = Settings.InputfieldBackgroundColor,
            RenderOrder = 5,
            IsVisible = false,
        };

        public static void Init()
        {
            CurrentParentEntry = Guid.Empty;
            entries = [];
            inputField.Transform.Position = new(5000, 5000);

            inputField.SubmitAction += EndEditing;
        }

        public static void StartEditing(EntryUI entry)
        {
            if (entry == CurrentlyEditingEntry) { return; }
            else if (CurrentlyEditingEntry != null)
            {
                EndEditing();
            }

            entry.StartEditing();
            CurrentlyEditingEntry = entry;
            inputField.SetText(entry.ReferenceEntry.Content);
            inputField.OnClick();
            inputField.Transform.Position = entry.Transform.Position;
            inputField.IsVisible = true;
        }

        public static void EndEditing()
        {
            CurrentlyEditingEntry?.EndEditing(inputField.Content);

            CurrentlyEditingEntry = null;
            inputField.IsVisible = false;
            inputField.Transform.Position = new(5000, 5000);
        }

        public static void CancelEditing()
        {
            CurrentlyEditingEntry?.EndEditing();

            CurrentlyEditingEntry = null;
            inputField.IsVisible = false;
            inputField.Transform.Position = new(5000, 5000);
        }

        public static void LoadFromSave(List<Entry> TargetEntries)
        {
            entries.Clear();
            foreach (var entry in TargetEntries)
            {
                entries.Add(entry.guid, (entry, null));
            }
            LayerLoadingError = false;
        }

        public static void LoadEntryLayer(Guid? target, bool createAction = true)
        {
            try
            {
                if (target == null)
                {
                    target = Guid.Empty;
                }

                var formerParent = CurrentParentEntry;
                CurrentParentEntry = (Guid)target;

                var allElementsSnapshot = ChunkManager.GetAllObjects()?.ToList() ?? [];

                if (allElementsSnapshot.Count > 0)
                {
                    var sortedElements = allElementsSnapshot
                        .Where(x => x != null && !x.IsScreenSpace)
                        .OrderBy(x => x.RenderKey)
                        .ToList();

                    for (int i = 0; i < sortedElements.Count; i++)
                    {
                        sortedElements[i].RenderKey = i;
                    }
                }

                SelectionManager.ClearSelection();
                ChunkManager.Clear();

                List<Entry> targetEntries =
                [
                    .. entries.Where(x => x.Value.Item1 != null && x.Value.Item1.ParentEntryGuid == target && !x.Value.Item1.IsDeleted).Select(x => x.Value.Item1)
                ];

                CreateNewEntryUIs(targetEntries);

                GroupManager.LoadGroups(CurrentParentEntry);
                ImageManager.LoadImages(CurrentParentEntry);
                ConnectionManager.LoadConnections(CurrentParentEntry);
                ConnectionManager.UpdateAllConnections();

                if (createAction)
                {
                    var action = new UndoRedoAction(
                        undoActions: [() => LoadEntryLayer(formerParent, false)],
                        redoActions: [() => LoadEntryLayer(target, false)]
                    );
                    UndoRedoManager.ActionExecuted(action);
                }

                LayerLoaded?.Invoke(CurrentParentEntry);

                var allElementsInNewLayer = ChunkManager.GetAllObjects()?.ToList() ?? [];

                if (allElementsInNewLayer.Count > 0)
                {
                    var worldScreenElements = allElementsInNewLayer.Where(x => x != null && !x.IsScreenSpace).ToList();

                    if (worldScreenElements.Count > 0)
                    {
                        long highestRenderKey = worldScreenElements.Max(x => x.RenderKey);
                        SelectionManager.GlobalRenderCounter = highestRenderKey + 1;
                    }
                    else
                    {
                        SelectionManager.GlobalRenderCounter = 0;
                    }
                }
                else
                {
                    SelectionManager.GlobalRenderCounter = 0;
                }
            }
            catch (InvalidOperationException ex)
            {
                Logger.Log("EntryManager", $"error: {ex.Message}\nStacktrace: {ex.StackTrace}", LogLevel.FATAL);

                // try to load root again
                if (!LayerLoadingError)
                {
                    LayerLoadingError = true;
                    LoadEntryLayer(Guid.Empty);
                }
                else
                {
                    // Maybe something corrupted back to main menu just in case
                    RenderManager.ChangeScene("MainMenu");
                }
            }

            //stress test
            /*
            for(int i = 0 ; i< 10000; i++)
            {
                CreateNewEntry(null);
            }
            */
        }


        public static void CreateNewEntry(Vector2? targetPos)
        {
            Vector2 spawnPos;
            if (targetPos == null)
            {
                spawnPos = Camera.Position;
            }
            else
            {
                spawnPos = (Vector2)targetPos;
            }

            Entry entry = new(CurrentParentEntry, spawnPos)
            {
                Content = "New Entry",
            };

            EntryUI initiatedEntry = SpawnNewEntry(entry);

            if (initiatedEntry != null)
            {
                entries.Add(entry.guid, (entry, initiatedEntry));
            }

            var action = new UndoRedoAction(
               undoActions: [() => MarkEntriesAsDeleted([entry.guid])],
               redoActions: [() => UnmarkEntriesAsDeleted([entry.guid])]
            );

            UndoRedoManager.ActionExecuted(action);
        }

        private static EntryUI SpawnNewEntry(Entry entry)
        {
            EntryUI uI = new(entry)
            {
                Transform = { Position = entry.position, Scale = new(100, 100) },
                RenderOrder = 3,
            };
            uI.RecalcContainerSize();
            ChunkManager.AddObject(uI);
            return uI;
        }

        private static void CreateNewEntryUIs(List<Entry> TargetEntries)
        {
            foreach (Entry entry in TargetEntries)
            {
                if (entries.TryGetValue(entry.guid, out var foundValuePair))
                {
                    var spawnedEntry = SpawnNewEntry(entry);
                    entries[entry.guid] = (foundValuePair.Item1, spawnedEntry);
                }
                else
                {
                    Logger.Log("EntryManager", $"Entry {entry.guid} not found when creating UI!", LogLevel.FATAL);
                }
            }
        }

        public static Entry? GetEntryByGuid(Guid target)
        {
            entries.TryGetValue(target, out var entry);
            if (entry == default)
            {
                return null;
            }
            return entry.Item1;
        }

        public static EntryUI? GetEntryUIByGuid(Guid target)
        {
            entries.TryGetValue(target, out var entry);
            if (entry == default)
            {
                return null;
            }
            return entry.Item2;
        }

        public static bool DoesEntryExist(Guid target)
        {
            entries.TryGetValue(target, out var entryParent);
            return !(entryParent == default);
        }

        public static void SetMark(List<EntryUI>? uIs, EntryMark target)
        {
            if (uIs == null) { return; }
            foreach (var entry in uIs)
            {
                entry.ReferenceEntry.mark = target;
            }
        }

        public static void LayerUp()
        {
            entries.TryGetValue(CurrentParentEntry, out var currentParentEntryObj);

            if (currentParentEntryObj.Item1 == null)
            {
                return;
            }

            LoadEntryLayer(currentParentEntryObj.Item1.ParentEntryGuid);
        }

        public static void MarkSelectedEntriesAsDeleted()
        {
            List<EntryUI>? affectedEntryUIs = SelectionManager.GetSelectedTypeOfObject<EntryUI>();
            if (affectedEntryUIs == null || affectedEntryUIs.Count < 1) { return; }

            List<Guid> toBeDeletedEntries = [];
            List<Guid> toBeDeletedConnections = [];

            foreach (var entryUI in affectedEntryUIs)
            {
                var allChildren = GetAllChildrenRecursive(entryUI.ReferenceEntry.guid);
                toBeDeletedEntries.AddRange(allChildren);

                toBeDeletedEntries.Add(entryUI.ReferenceEntry.guid);
            }

            toBeDeletedEntries = [.. toBeDeletedEntries.Distinct()];

            foreach (var entryGuid in toBeDeletedEntries)
            {
                var connections = ConnectionManager.GetConnectionsForEntry(entryGuid);
                toBeDeletedConnections.AddRange(connections.Select(x => x.guid));
            }

            toBeDeletedConnections = [.. toBeDeletedConnections.Distinct()];

            MarkEntriesAsDeleted(toBeDeletedEntries);
            ConnectionManager.MarkConnectionsAsDeleted(toBeDeletedConnections);

            //create undoredo action and push to the manager

            var action = new UndoRedoAction(
                redoActions: [() => MarkEntriesAsDeleted(toBeDeletedEntries), () => ConnectionManager.MarkConnectionsAsDeleted(toBeDeletedConnections)],
                undoActions: [() => UnmarkEntriesAsDeleted(toBeDeletedEntries), () => ConnectionManager.UnmarkConnectionsAsDeleted(toBeDeletedConnections)],
                PopActions: [() => RemoveEntries(toBeDeletedEntries), () => ConnectionManager.RemoveConnections(toBeDeletedConnections)]
            );

            UndoRedoManager.ActionExecuted(action);
        }

        public static List<Guid> GetAllChildrenRecursive(Guid parentGuid)
        {
            List<Guid> children = [];

            var directChildren = entries.Where(x => x.Value.Item1.ParentEntryGuid == parentGuid)
                                       .Select(x => x.Key)
                                       .ToList();

            foreach (var childGuid in directChildren)
            {
                children.Add(childGuid);
                children.AddRange(GetAllChildrenRecursive(childGuid));
            }

            return children;
        }

        public static void MarkEntriesAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (entries.TryGetValue(guid, out var tuple))
                {
                    tuple.Item1.IsDeleted = true;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.IsVisible = false;
                        SelectionManager.Deselect(tuple.Item2);
                    }
                }
            }
        }

        public static void UnmarkEntriesAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (entries.TryGetValue(guid, out var tuple))
                {
                    tuple.Item1.IsDeleted = false;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.IsVisible = true;
                    }
                }
            }
        }

        public static void RemoveEntries(List<Guid> guids)
        {
            foreach (Guid guid in guids)
            {
                entries.Remove(guid);
            }
        }

        public static void Dispose()
        {
            foreach (var ui in entries.Where(x => x.Value.Item2 != null))
            {
                ui.Value.Item2?.Dispose();
            }
            inputField.SubmitAction -= EndEditing;
            inputField.Dispose();
        }

        static internal void RecalcEntrySizes()
        {
            foreach (var entry in entries)
            {
                entry.Value.Item2?.RecalcContainerSize();
            }
        }
    }
}