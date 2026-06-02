using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

public class UIScrollableObjectList : UIImage
{
    public float ItemPadding { get; set; } = 2f;
    private bool IsHovering = false;
    int CurrentIndex = 0;

    public UIScrollableObjectList(float itemPadding = 2f, bool nineslice = true)
    : base(texture: null, screenSpace: true, nineSlice: nineslice)
    {
        if (nineslice)
        {
            Texture = TextureHandler.GetEmbeddedTextureByName("button_rectangle_line.png");
        }
        ItemPadding = itemPadding;
        IsScreenSpace = true;
    }

    public override void OnHoverStart()
    {
        base.OnHoverStart();
        IsHovering = true;
    }

    public override void OnHoverEnd()
    {
        base.OnHoverEnd();
        IsHovering = false;
    }

    public void ClearList()
    {
        foreach (var item in ChildObjects)
        {
            item.Dispose();
        }
        ChildObjects.Clear();
        CurrentIndex = 0;
    }

    public void AddItem(UIObject target, bool immediateRecalcLayout = false)
    {
        if (ChildObjects.Contains(target)) { return; }

        ChildObjects.Add(target);
        target.RenderOrder = RenderOrder + 1;

        if (immediateRecalcLayout)
        {
            RecalcLayout();
        }
    }

    public void RemoveItem(UIObject target, bool immediateRecalcLayout = true)
    {
        ChildObjects.Remove(target);

        if (immediateRecalcLayout)
        {
            RecalcLayout();
        }
    }

    public void RecalcLayout()
    {
        if (ChildObjects.Count == 0) return;

        ChildObjects.ForEach
        (
            x => x.Transform.Scale = new
            (
                Transform.Scale.X - ItemPadding,
                x.Transform.Scale.Y
            )
        );
        

        PositionObjects();
    }

    private void PositionObjects()
    {
        if (ChildObjects.Count <= 0) { return; }

        CurrentIndex = int.Clamp(CurrentIndex, 0, ChildObjects.Count - 1);

        ChildObjects.ForEach(x => x.IsVisible = false);

        //start with padding/2
        float CurrentY = Transform.Position.Y - (Transform.Scale.Y / 2f) + (ItemPadding / 2f);

        for (int i = CurrentIndex; i < ChildObjects.Count; i++)
        {
            var obj = ChildObjects[i];
            if (obj == null) { continue; }

            obj.Transform.Position = new(Transform.Position.X, CurrentY + (obj.Transform.Scale.Y / 2f));

            if (!IsItemVisible(obj))
            {
                break;
            }

            obj.IsVisible = true;

            CurrentY += obj.Transform.Scale.Y + ItemPadding;
        }

        foreach (var item in ChildObjects.Where(x => x is SaveFileButton).Cast<SaveFileButton>())
        {
            item.PositionButtons();
        }
    }

    private bool IsItemVisible(UIObject item)
    {
        var itemTop = item.Transform.Position.Y + (item.Transform.Scale.Y / 2);
        var itemBottom = item.Transform.Position.Y - (item.Transform.Scale.Y / 2);
        var containerTop = Transform.Position.Y + (Transform.Scale.Y / 2);
        var containerBottom = Transform.Position.Y - (Transform.Scale.Y / 2);

        return itemBottom >= containerBottom && itemTop < containerTop;
    }


    private void OnScroll(IMouse mouse, ScrollWheel wheel)
    {
        if (!IsHovering || ChildObjects.Count <= 0) return;
        CurrentIndex += float.IsPositive(wheel.Y) ? -1 : 1;

        PositionObjects();
    }

    private void OnMouseMove(IMouse mouse, Vector2 vector)
    {
        if (ContainsPoint(vector))
        {
            OnHoverStart();
        }
        else
        {
            OnHoverEnd();
        }
    }

    public override void Render()
    {
        base.Render();


        //RenderScrollBar();
    }


    public void SubActions()
    {
        if (InputDeviceHandler.primaryMouse == null) return;
        InputDeviceHandler.primaryMouse.Scroll += OnScroll;
        InputDeviceHandler.primaryMouse.MouseMove += OnMouseMove;
    }


    public void UnsubActions()
    {
        if (InputDeviceHandler.primaryMouse == null) return;
        InputDeviceHandler.primaryMouse.Scroll -= OnScroll;
        InputDeviceHandler.primaryMouse.MouseMove -= OnMouseMove;
    }

    public override void Dispose()
    {
        UnsubActions();
        ClearList();
        base.Dispose();
    }
}