using System.Numerics;

namespace Rendering.UI
{
    public class UIButtonList : UIButton
    {
        public bool ListVisible = false;
        public UIImage ListBG;
        public float ButtonPadding = 1f;
        public float maxOptionWidth = 0;
        public float optionButtonHeight => Settings.TextSize;
        private bool ShowListOnSide = false;

        public UIButtonList(string TextToDisplay, bool isScreenSpace, bool ImmidieteClose, List<UIButton> ListButtons, bool showListOnSide, TextAnchorPoint textAnchorPoint = TextAnchorPoint.Left_Top)
        : base(TextToDisplay, [], screenSpace: isScreenSpace, textAnchorPoint: textAnchorPoint)
        {
            ShowListOnSide = showListOnSide;
            IsScreenSpace = isScreenSpace;

            ListBG = new(null, isScreenSpace, true) { IsSelectable = false };

            CalculateOptionButtonSizes();
            if (ListButtons.Count > 0)
            {
                float Height = optionButtonHeight * ListButtons.Count;
                ListBG.Transform.Scale = new(maxOptionWidth, Height - ButtonPadding);

                foreach (var button in ListButtons)
                {
                    if (ImmidieteClose)
                    {
                        button.actions.Add(CloseList);
                    }
                    button.actions.Reverse();
                    button.IsScreenSpace = true;
                    button.TextureColor = Settings.ButtonBGColor;
                }
            }
            SetText(TextToDisplay);
            UpdateListBackgroundSize();
            ChildObjects.AddRange(ListButtons);
        }

        public void AddOptionButton(UIButton button)
        {
            button.actions.Insert(0, CloseList);

            ChildObjects.Add(button);
            CalculateOptionButtonSizes();
            UpdateListBackgroundSize();
        }

        public void RemoveOptionButton(UIButton button)
        {
            ChildObjects.Remove(button);
            CalculateOptionButtonSizes();
        }

        private void CalculateOptionButtonSizes()
        {
            var maxsize = LayoutHelper.CalculateMaxSize([.. ChildObjects.Where(x => x is not null).Cast<UIObject>()]);
            maxOptionWidth = maxsize.X;

            foreach (var button in ChildObjects)
            {
                button.Transform.Scale = maxsize;
            }
            UpdateListBackgroundSize();
        }


        private void UpdateListBackgroundSize()
        {
            if (ChildObjects.Count > 0)
            {
                float Height = ChildObjects.Sum(x => x.Transform.Scale.Y) + (ButtonPadding * ChildObjects.Count);
                ListBG.Transform.Scale = new(maxOptionWidth, Height);
            }
        }

        public override void OnClick(Vector2 pos)
        {
            ToggleList();
        }

        public void CloseList()
        {
            ListVisible = false;
            foreach (var button in ChildObjects)
            {
                button.IsVisible = false;
            }
        }

        public void ToggleList()
        {
            ListVisible = !ListVisible;

            if (ListVisible)
            {
                CalculateOptionButtonSizes();
                if (ShowListOnSide)
                {
                    ShowListSide();
                }
                else
                {
                    ShowListBottom();
                }

                AudioHandler.PlaySound("open_001");

                foreach (var btn in ChildObjects)
                {
                    btn.IsVisible = true;
                    btn.RenderOrder = RenderOrder + 1;
                }
            }
            else
            {
                AudioHandler.PlaySound("close_004");

                CloseList();
            }


            ListBG.RenderOrder = RenderOrder;
        }

        private void ShowListSide()
        {
            var bounds = Bounds;
            var RightProbe = bounds.Right + (ListBG.Transform.Scale.X / 2f);

            Vector2 viewportSize = new(Camera.ViewportSize.X, Camera.ViewportSize.Y);

            Vector2 currentPos;

            if (RightProbe < viewportSize.X)
            {
                currentPos = new(RightProbe, Transform.Position.Y);
                ListBG.Transform.Position = new(RightProbe, Transform.Position.Y - Transform.Scale.Y / 2f);
            }
            else
            {
                currentPos = new(bounds.Left - (ListBG.Transform.Scale.X / 2f), Transform.Position.Y);
                ListBG.Transform.Position = new(currentPos.X, Transform.Position.Y - Transform.Scale.Y / 2f);
            }

            if (currentPos.Y + ListBG.Transform.Scale.Y > viewportSize.Y)
            {
                float overflow = viewportSize.Y - (currentPos.Y + ListBG.Transform.Scale.Y);
                currentPos.Y += overflow;
            }

            if (currentPos.Y < 0)
            {
                currentPos.Y = 0;
            }

            var listBGBounds = ListBG.Bounds;
            LayoutHelper.Vertical(ChildObjects, new(listBGBounds.Left, listBGBounds.Top), ButtonPadding);
        }

        private void ShowListBottom()
        {
            Vector2 viewportSize = new(Camera.ViewportSize.X, Camera.ViewportSize.Y);
            var bounds = Bounds;

            Vector2 currentPos = new(
                bounds.Left + maxOptionWidth / 2,
                bounds.Bottom + ListBG.Transform.Scale.Y / 2
            );

            currentPos = ClampListToBottom(currentPos, viewportSize);

            LayoutHelper.Vertical(ChildObjects, currentPos - ListBG.Transform.Scale / 2, ButtonPadding);

            foreach (var button in ChildObjects)
            {
                button.IsVisible = true;
            }
        }

        private Vector2 ClampListToBottom(Vector2 currentPos, Vector2 viewportSize)
        {
            if (currentPos.Y + ListBG.Transform.Scale.Y / 2 > viewportSize.Y)
            {
                currentPos.Y = Bounds.Left - ListBG.Transform.Scale.Y / 2;
            }

            if (currentPos.X - ListBG.Transform.Scale.X / 2 < 0)
            {
                currentPos.X = ListBG.Transform.Scale.X / 2;
            }
            else if (currentPos.X + ListBG.Transform.Scale.X / 2 > viewportSize.X)
            {
                currentPos.X = viewportSize.X - ListBG.Transform.Scale.X / 2;
            }
            Vector2 pos = new(currentPos.X, currentPos.Y);
            ListBG.Transform.Position = pos;
            return pos;
        }


        public override void Render()
        {
            if (!IsVisible)
            {
                ListVisible = false;
                return;
            }

            base.Render();
            if (!ListVisible) { return; }
            ListBG.Render();
            foreach (var button in ChildObjects)
            {
                button.Render();
            }
        }
    }
}