using System.Numerics;
using Rendering.UI;

namespace Managers
{
    public static class SelectionManager
    {
        private static List<UIObject> SelectedElements = [];
        public static long GlobalRenderCounter = 0;

        public static void Select(UIObject target, SelectionOption option)
        {
            switch (option)
            {
                case SelectionOption.NONE:
                    ClearSelection();
                    SelectedElements.Add(target);
                    break;

                case SelectionOption.EXCLUSIVE_ADD:
                    SelectedElements.Remove(target);
                    SelectedElements.Add(target);
                    break;

                case SelectionOption.EXCLUSIVE_REMOVE:
                    SelectedElements.Remove(target);
                    break;
            }
        }

        public static void Select(List<UIObject> targets, SelectionOption option)
        {
            if (option == SelectionOption.NONE) { ClearSelection(); }
            foreach (UIObject target in targets.Where(x => x is not ResizeHandle))
            {
                if (target is UIButton) { continue; }
                switch (option)
                {
                    case SelectionOption.NONE:
                        SelectedElements.Add(target);
                        break;
                    case SelectionOption.EXCLUSIVE_ADD:
                        if (!SelectedElements.Contains(target))
                        {
                            SelectedElements.Add(target);
                        }
                        break;
                    case SelectionOption.EXCLUSIVE_REMOVE:
                        SelectedElements.Remove(target);
                        break;
                }
            }
        }

        public static void Deselect(UIObject uIObject)
        {
            SelectedElements.Remove(uIObject);
        }

        public static bool IsObjectSelected(UIObject target)
        {
            return SelectedElements.Contains(target);
        }

        public static bool IsAnyObjectSelected()
        {
            return SelectedElements.Count != 0;
        }

        public static bool IsAnyObjectSelected(IEnumerable<Type> excludedTypes)
        {
            return SelectedElements.Any(x =>
                excludedTypes.All(t => !t.IsAssignableFrom(x.GetType()))
            );
        }

        public static bool IsAnyObjectSelected(Type excludedType)
        {
            return SelectedElements.Any(x =>
                !excludedType.IsAssignableFrom(x.GetType())
            );
        }


        public static bool IsTypeOfObjectSelected(Type type)
        {
            return SelectedElements.Any(x => type.IsAssignableFrom(x.GetType()));
        }

        public static List<T> GetSelectedTypeOfObject<T>() where T : class
        {
            return [.. SelectedElements.OfType<T>()];
        }

        public static List<Type> GetAllTypesOfSelected()
        {
            return [.. SelectedElements.Select(e => e.GetType()).Distinct()];
        }


        public static void DragStart()
        {
            GlobalRenderCounter++;
            foreach (UIObject selected in SelectedElements.Where(x => x.IsDraggable && !x.IsScreenSpace))
            {
                if (selected.IsScreenSpace)
                {
                    selected.OnDragStart();
                    continue;
                }
                selected.OnDragStart();
                selected.RenderKey = GlobalRenderCounter;
                ChunkManager.RemoveObject(selected);
            }
        }

        public static void Dragging(Vector2 delta)
        {
            var DragableObjects = SelectedElements.Where(x => x.IsDraggable).ToList();
            foreach (UIObject selected in DragableObjects)
            {
                selected.Transform.Position += delta;
                selected.OnDrag();

                if (selected is EntryUI entryUI)
                {
                    ConnectionManager.UpdateConnectionsForEntry(entryUI.ReferenceEntry.guid);
                }
            }
        }

        public static void DragEnd()
        {
            GlobalRenderCounter++;
            foreach (UIObject selected in SelectedElements.Where(x => x.IsDraggable))
            {
                if (selected.IsScreenSpace)
                {
                    selected.OnDragEnd();
                    continue;
                }
                selected.OnDragEnd();
                selected.RenderKey = GlobalRenderCounter;
                ChunkManager.AddObject(selected);
            }

            SelectedElements.RemoveAll(x => x is ResizeHandle);
        }

        public static void ClearSelection()
        {
            SelectedElements.Clear();
        }
    }

    public enum SelectionOption
    {
        EXCLUSIVE_ADD = 0,
        EXCLUSIVE_REMOVE = 1,
        NONE = 2,
    }
}