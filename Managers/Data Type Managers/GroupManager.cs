using System.Numerics;
using Rendering.UI;

namespace Managers
{
    /// <summary>
    /// <para>Manages Group and GroupUI</para>
    /// </summary>

    public static class GroupManager
    {
        private static Dictionary<Guid, (Group, GroupUI?)> groups = null!;
        public static List<Group> GetAllGroups => [.. groups.Select(x => x.Value.Item1).Where(x => !x.IsDeleted)];
        public static GroupUI? CurrentlyEditingGroup { get; private set; } = null;
        public static bool IsEditing => CurrentlyEditingGroup != null;

        public static InputField? inputField;

        public static void Init()
        {
            groups = [];

            inputField = new()
            {
                TextureColor = Settings.InputfieldBackgroundColor,
                RenderOrder = 5,
                IsVisible = false,
            };

            inputField.RecalcSize();
            inputField.ContentChanged += OnInputChanged;
            inputField.SubmitAction += EndEditing;
        }

        public static void OnInputChanged(string _)
        {
            if (CurrentlyEditingGroup != null && inputField != null)
            {
                inputField.Transform.Position = new(
                    CurrentlyEditingGroup.Transform.Position.X - CurrentlyEditingGroup.Transform.Scale.X / 2 + inputField.Transform.Scale.X / 2,
                    CurrentlyEditingGroup.Transform.Position.Y + CurrentlyEditingGroup.Transform.Scale.Y / 2 - inputField.Transform.Scale.Y / 2
                );
            }
        }

        public static void StartEditing(GroupUI group)
        {
            if (group == CurrentlyEditingGroup) { return; }
            else if (CurrentlyEditingGroup != null)
            {
                EndEditing();
            }

            if (inputField == null) { return; }

            CurrentlyEditingGroup = group;
            CurrentlyEditingGroup.StartEditing();

            inputField.SetText(group.ReferenceGroup.GroupName);
            inputField.OnClick();
            inputField.IsVisible = true;
            inputField.RecalcSize();

            inputField.Transform.Position = new(
                group.Transform.Position.X - group.Transform.Scale.X / 2f + inputField.Transform.Scale.X / 2f,
                group.Transform.Position.Y + group.Transform.Scale.Y / 2f - inputField.Transform.Scale.Y / 2f
            );

            //inputField.RecalcSize();
        }

        public static void EndEditing()
        {
            if (inputField == null) { return; }
            CurrentlyEditingGroup?.EndEdit(inputField.Content);

            CurrentlyEditingGroup = null;
            inputField.IsVisible = false;
            inputField.Transform.Position = new(5000, 5000);
        }

        public static void CancelEditing()
        {
            CurrentlyEditingGroup?.EndEdit();

            if (inputField == null) { return; }

            CurrentlyEditingGroup = null;
            inputField.IsVisible = false;
            inputField.Transform.Position = new(5000, 5000);
        }

        public static void LoadFromSave(List<Group> TargetGroups)
        {
            groups.Clear();

            foreach (var g in TargetGroups)
            {
                groups.TryAdd(g.guid, (g, null));
            }
        }

        public static void LoadGroups(Guid target)
        {
            List<Group> targetGroups = [.. groups.Where(x => x.Value.Item1.ParentEntryGuid == target).Select(x => x.Value.Item1)];
            CreateNewGroupUIs(targetGroups);
        }

        public static void CreateNewGroup(Vector2? targetPos)
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

            Guid currentParent = EntryManager.CurrentParentEntry;

            Group group = new(currentParent, spawnPos)
            {
                GroupName = "New Group"
            };

            var newGroup = CreateNewGroupUI(group);
            groups.Add(group.guid, (group, newGroup));
            newGroup.RecalcMinSize();
            newGroup.RecalcSize();

            var action = new UndoRedoAction(
               undoActions: [() => MarkGroupsAsDeleted([group.guid])],
               redoActions: [() => UnmarkGroupsAsDeleted([group.guid])]
            );

            UndoRedoManager.ActionExecuted(action);
        }


        static void CreateNewGroupUIs(List<Group> TargetGroups)
        {
            foreach (Group group in TargetGroups)
            {
                if (groups.TryGetValue(group.guid, out var foundValuePair))
                {
                    var spawnedEntry = CreateNewGroupUI(group);
                    groups[group.guid] = (foundValuePair.Item1, spawnedEntry);
                }
                else
                {
                    Logger.Log("GroupManager", $"Group {group.guid} not found when creating UI!", LogLevel.FATAL);
                }
            }
        }

        static GroupUI CreateNewGroupUI(Group group)
        {
            GroupUI uI = new(group)
            {
                Transform = { Position = group.position },
                RenderOrder = 0,
            };
            
            uI.RecalcMinSize();
            uI.Transform.Scale = group.Size;
            uI.RecalcHandlerPos();
            uI.OnDragResizeHandle(3);

            ChunkManager.AddObject(uI);
            return uI;
        }


        public static void MarkSelectedGroupsAsDeleted()
        {
            List<GroupUI>? affected = SelectionManager.GetSelectedTypeOfObject<GroupUI>();
            if (affected == null || affected.Count < 1) { return; }

            List<Guid> guids = [.. affected.Select(x => x.ReferenceGroup.guid)];
            MarkGroupsAsDeleted(guids);

            var action = new UndoRedoAction(
                undoActions: [() => UnmarkGroupsAsDeleted(guids)],
                redoActions: [() => MarkGroupsAsDeleted(guids)],
                PopActions: [() => RemoveGroups(guids)]
            );

            UndoRedoManager.ActionExecuted(action);
        }

        public static void MarkGroupsAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (groups.TryGetValue(guid, out var tuple))
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

        public static void UnmarkGroupsAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                if (groups.TryGetValue(guid, out var tuple))
                {
                    tuple.Item1.IsDeleted = false;
                    if (tuple.Item2 != null)
                    {
                        tuple.Item2.IsVisible = true;
                    }
                }
            }
        }

        public static void RemoveGroups(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                groups.Remove(guid);
            }
        }

        public static void Dispose()
        {
            if (inputField != null)
            {
                inputField.ContentChanged -= OnInputChanged;
                inputField.SubmitAction -= EndEditing;
                inputField.Dispose();
            }

            foreach (var ui in groups.Where(x => x.Value.Item2 != null))
            {
                ui.Value.Item2?.Dispose();
            }
        }
    }
}