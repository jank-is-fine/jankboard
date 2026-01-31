using System.Drawing;
using System.Numerics;
using Managers;

namespace Rendering.UI
{
    public class ContextMenu : UIImage
    {
        public static ContextMenu Instance = null!;
        private List<(UIButton button, List<Type> categories)> contextMenuOptions = [];
        private List<(UIButton button, List<Type> categories)> CurrentActiveOptions = [];
        public List<UIObject> Options => [.. CurrentActiveOptions.Select(x => x.button).Where(x => x.IsVisible)];
        private float maxWidth = 0;
        private const float BUTTON_MARGIN = 1.5f;
        public Vector2 TargetPos = Vector2.Zero;

        public ContextMenu()
        : base(screenSpace: true)
        {
            Instance = this;
            IsSelectable = true;
            TextureColor = Color.DimGray;
            RenderOrder = 40;
            IsVisible = false;

            SetupContextMenu();
        }

        private void SetupContextMenu()
        {
            AddNewContextMenuOption
            (
                button: ContextButtonList
                (
                    "Create new",
                    [
                        ContextButton(" Entry ", [() => EntryManager.CreateNewEntry(Camera.ScreenToWorld(TargetPos))]),
                        ContextButton(" Group ", [() => GroupManager.CreateNewGroup(Camera.ScreenToWorld(TargetPos))]),
                        ContextButton(" Image or GIF ",[() => ImageManager.CreateNewImages(FileBrowserManager.OpenFileBrowser([Settings.ImageOrGifFilter]),Camera.ScreenToWorld(TargetPos))])
                    ]
                ),
                categories: []
            );

            AddNewContextMenuOption
            (
                button: ContextButton
                (
                    "Connect from",
                    [() => ConnectionManager.StartConnection(UIobjectHandler.GetObjectUnderMouse(TargetPos))]
                ),
                categories: [typeof(EntryUI)]
            );

            AddNewContextMenuOption
            (
                button: ContextButton
                (
                    "Connect to",
                    [() => ConnectionManager.EndConnection(UIobjectHandler.GetObjectUnderMouse(TargetPos))]
                ),
                categories: [typeof(EntryUI)]
            );

            AddNewContextMenuOption
            (
                button: ContextButton
                (
                    "Connect all selected to",
                    [() => ConnectionManager.EndConnectionFromAllSelected(UIobjectHandler.GetObjectUnderMouse(TargetPos))]
                ),
                categories: [typeof(EntryUI)]
            );

            AddNewContextMenuOption
            (
                button: ContextButtonList
                (
                    "Delete",
                    [
                        ContextButton("Entries", [EntryManager.MarkSelectedEntriesAsDeleted]),
                        ContextButton("Groups", [() => GroupManager.MarkSelectedGroupsAsDeleted()]),
                        ContextButton("Connections", [() => ConnectionManager.MarkSelectedConnectionsAsDeleted()]),
                        ContextButton("Images", [ImageManager.MarkSelectedImagesAsDeleted]),
                        ContextButton("All", [DeleteAllSelected])
                    ]
                ),
                categories: [typeof(EntryUI), typeof(GroupUI), typeof(ImageUI), typeof(ConnectionUI)]
            );


            AddNewContextMenuOption
            (
                button: ContextButton
                (
                    "Enter Entry", [() => EntryManager.LoadEntryLayer(SelectionManager.GetSelectedTypeOfObject<EntryUI>().Last().ReferenceEntry.guid)]
                ),
                categories: [typeof(EntryUI)]
            );

            AddNewContextMenuOption
            (
                button: ContextButton("Layer Up", [() => EntryManager.LayerUp()]),
                categories: []
            );


            AddNewContextMenuOption
            (
                button: ContextButtonList
                (
                    "Set Entry Mark",
                    [
                        ContextButton("None", [() => EntryManager.SetMark(SelectionManager.GetSelectedTypeOfObject<EntryUI>(), EntryMark.NONE)]),
                        ContextButton("PRIORITY", [() => EntryManager.SetMark(SelectionManager.GetSelectedTypeOfObject<EntryUI>(), EntryMark.PRIORITY)]),
                        ContextButton("DONE", [() => EntryManager.SetMark(SelectionManager.GetSelectedTypeOfObject<EntryUI>(), EntryMark.DONE)]),
                        ContextButton("DROPPED", [() => EntryManager.SetMark(SelectionManager.GetSelectedTypeOfObject<EntryUI>(), EntryMark.DROPPED)])
                    ]
                ),
                categories: [typeof(EntryUI)]
            );


            AddNewContextMenuOption
            (
                button: ContextButtonList
                (
                    "Set Arrow Type",
                    [
                        ContextButton
                        (
                            "Default",
                            [() => ConnectionManager.SetArrowType(SelectionManager.GetSelectedTypeOfObject<ConnectionUI>(), ArrowType.Default)]
                        ),

                        ContextButton
                        (
                            "Loose",
                            [() => ConnectionManager.SetArrowType(SelectionManager.GetSelectedTypeOfObject<ConnectionUI>(), ArrowType.Loose)]
                        ),

                        ContextButton
                        (
                            "Loose Diagonal",
                            [() => ConnectionManager.SetArrowType(SelectionManager.GetSelectedTypeOfObject<ConnectionUI>(), ArrowType.LooseDiagonal)]
                        )
                    ]
                ),
                categories: [typeof(ConnectionUI)]
            );


            SetupButtons([]);
        }

        public void DeleteAllSelected()
        {
            var affectedEntries = SelectionManager.GetSelectedTypeOfObject<EntryUI>();
            var affectedConnections = SelectionManager.GetSelectedTypeOfObject<ConnectionUI>();
            var affectedGroups = SelectionManager.GetSelectedTypeOfObject<GroupUI>();
            var affectedImages = SelectionManager.GetSelectedTypeOfObject<ImageUI>();

            List<Guid> TargetEntries = [];
            List<Guid> TargetConnections = [];
            List<Guid> TargetGroups = [];
            List<Guid> TargetImages = [];

            foreach (var entry in affectedEntries)
            {
                TargetEntries.AddRange(EntryManager.GetAllChildrenRecursive(entry.ReferenceEntry.guid));
                TargetEntries.Add(entry.ReferenceEntry.guid);

                TargetConnections.AddRange(ConnectionManager.GetConnectionsForEntry(entry.ReferenceEntry.guid).Select(x => x.guid));
            }

            TargetConnections.AddRange(affectedConnections.Select(x => x.ReferenceConnection.guid));
            //TargetConnections = [.. TargetConnections.Distinct()];

            TargetGroups.AddRange(affectedGroups.Select(x => x.ReferenceGroup.guid));
            TargetImages.AddRange(affectedImages.Select(x => x.ReferenceImage.guid));
            SelectionManager.ClearSelection();

            EntryManager.MarkEntriesAsDeleted(TargetEntries);
            ConnectionManager.MarkConnectionsAsDeleted(TargetConnections);
            GroupManager.MarkGroupsAsDeleted(TargetGroups);
            ImageManager.MarkImagesAsDeleted(TargetImages);

            var action = new UndoRedoAction(
               undoActions: [() => EntryManager.UnmarkEntriesAsDeleted(TargetEntries),
                             () => ConnectionManager.UnmarkConnectionsAsDeleted(TargetConnections),
                             () => GroupManager.UnmarkGroupsAsDeleted(TargetGroups),
                             () => ImageManager.UnmarkImagesAsDeleted(TargetImages),
                           ],

               redoActions: [() => EntryManager.MarkEntriesAsDeleted(TargetEntries),
                             () => ConnectionManager.MarkConnectionsAsDeleted(TargetConnections),
                             () => GroupManager.MarkGroupsAsDeleted(TargetGroups),
                             () => ImageManager.MarkImagesAsDeleted(TargetImages),
                           ],

                PopActions: [() => EntryManager.RemoveEntries(TargetEntries),
                  () => ConnectionManager.RemoveConnections(TargetConnections),
                  () => GroupManager.RemoveGroups(TargetGroups),
                  () => ImageManager.RemoveImages(TargetImages),
                ]
           );

            UndoRedoManager.ActionExecuted(action);
        }

        public void AddNewContextMenuOption(UIButton button, List<Type> categories)
        {
            //sanity check
            if (button == null) { return; }

            if (button is UIButton uIButton)
            {
                uIButton.actions.Insert(0, Hide);
            }

            contextMenuOptions.Add((button, categories));

            if (button is UIButton iButton)
            {
                RectangleF box = TextHelper.GetStringRenderBox(iButton.ButtonContent, FontType.REGULAR, Settings.TextSize);
                if (box.Width > maxWidth)
                {
                    maxWidth = box.Width;
                }

            }
            else if (button is UIButtonList iListButton)
            {
                RectangleF box = TextHelper.GetStringRenderBox(iListButton.ButtonContent, FontType.REGULAR, Settings.TextSize);
                if (box.Width + BUTTON_MARGIN / 2 > maxWidth)
                {
                    maxWidth = box.Width;
                }
            }
            UpdateAllButtonSizes();
        }

        private void UpdateAllButtonSizes()
        {
            var allButtons = contextMenuOptions;
            var maxSize = LayoutHelper.CalculateMaxSize([.. contextMenuOptions.Where(x => x.button is not null).Select(x => x.button).Cast<UIObject>()]);
            maxSize = new(maxSize.X + BUTTON_MARGIN * 2f, maxSize.Y + BUTTON_MARGIN * 2f);

            foreach (var (button, categories) in allButtons)
            {
                button.SetScale(maxSize);
            }
        }

        public void SetupButtons(List<Type> menuCategories)
        {
            CurrentActiveOptions.Clear();

            foreach (var btn in Options)
            {
                btn.IsVisible = false;
            }

            CurrentActiveOptions.AddRange([.. contextMenuOptions.Where(x => x.categories.Count == 0)]);

            foreach (var menuCategory in menuCategories)
            {
                CurrentActiveOptions.AddRange([.. contextMenuOptions.Where(x => x.categories.Contains(menuCategory))]);
            }
            CurrentActiveOptions = [.. CurrentActiveOptions.Distinct()];
        }

        public void PositionButtons()
        {
            Vector2 Pos = new(Transform.Position.X - Transform.Scale.X / 2, Transform.Position.Y - Transform.Scale.Y / 2);

            List<UIObject> Buttons = [.. CurrentActiveOptions.Select(option => option.button)];

            LayoutHelper.Vertical(Buttons, Pos, BUTTON_MARGIN);
            foreach (var activeBtn in Buttons)
            {
                activeBtn.IsVisible = true;
            }
        }

        public void Hide()
        {
            IsVisible = false;

            foreach (var uIListButton in CurrentActiveOptions.Where(x => x.button is UIButtonList))
            {
                if (uIListButton.button is UIButtonList button)
                {
                    button.CloseList();
                }
            }

            foreach (var btn in CurrentActiveOptions)
            {
                btn.button.IsVisible = false;
            }
        }

        public override void RecalcSize()
        {
            var Height = CurrentActiveOptions.Sum(x => x.button.Transform.Scale.Y);
            Transform.Scale = new(maxWidth, Height + (BUTTON_MARGIN * CurrentActiveOptions.Count) - BUTTON_MARGIN);
        }

        public override void Render()
        {
            if (!IsVisible) { return; }
            base.Render();

            foreach (var (button, _) in CurrentActiveOptions)
            {
                button.Render();
            }
        }

        public void Show(Vector2 targetPos)
        {
            Hide();
            TargetPos = targetPos;
            float xCheck = targetPos.X;
            float yCheck = targetPos.Y;

            List<Type> categories = SelectionManager.GetAllTypesOfSelected();

            Vector2 viewportSize = new(Camera.ViewportSize.X, Camera.ViewportSize.Y);

            SetupButtons(categories);
            RecalcSize();

            if (xCheck + Transform.Scale.X > viewportSize.X)
            {
                float overflow = viewportSize.X - (xCheck + Transform.Scale.X);
                xCheck += Transform.Scale.X / 2 + overflow;
            }
            else
            {
                xCheck += Transform.Scale.X / 2;
            }

            if (yCheck + Transform.Scale.Y + (BUTTON_MARGIN * 2f) > viewportSize.Y)
            {
                float overflow = viewportSize.Y - (yCheck + Transform.Scale.Y + (BUTTON_MARGIN * 2f));
                yCheck += Transform.Scale.Y / 2f + overflow;
            }
            else
            {
                yCheck += Transform.Scale.Y / 2f;
            }

            Transform.Position = new(xCheck, yCheck);
            PositionButtons();

            IsVisible = true;
        }

        #region Factory functions

        private UIButton ContextButton(string displayText, List<Action> actions)
        {
            return new UIButton
            (
                DisplayText: $" {displayText}", // space at the beggining for a simple padding hack
                targetActions: [Hide, .. actions],
                textAnchorPoint: TextAnchorPoint.Left_Center
            )
            {
                IsSelectable = true,
                IsDraggable = false,
                RenderOrder = 61,
            };
        }

        private UIButtonList ContextButtonList(string displayText, List<UIButton> buttons)
        {
            return new UIButtonList
            (
                TextToDisplay: $" {displayText}", // space at the beggining for a simple padding hack
                isScreenSpace: true,
                ImmidieteClose: true,
                ListButtons: buttons,
                showListOnSide: true,
                textAnchorPoint: TextAnchorPoint.Left_Center
            )
            {
                IsDraggable = false,
                TextureColor = Color.White,
                RenderOrder = 61,
            };
        }

        #endregion Factory functions
    }
}