using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.Input;

public class UIScrollableObjectList : UIImage
{
    public float ItemPadding { get; set; } = 2f;
    public float ScrollOffset { get; private set; } = 0f;
    public float MaxScrollOffset { get; private set; } = 0f;
    private bool IsHovering = false;
    private float _contentHeight = 0f;

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
        ScrollOffset = 0f;
        MaxScrollOffset = 0f;
        _contentHeight = 0f;
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
        var bounds = Bounds;

        _contentHeight = 0f;
        foreach (var item in ChildObjects)
        {
            _contentHeight += item.Transform.Scale.Y + ItemPadding;
        }
        _contentHeight += ItemPadding;

        MaxScrollOffset = Math.Max(0f, _contentHeight - Transform.Scale.Y);

        ScrollOffset = Math.Clamp(ScrollOffset, 0f, MaxScrollOffset);

        float currentY = bounds.Top - ScrollOffset + ItemPadding;

        foreach (var item in ChildObjects)
        {
            if (item is UIButton uIButton)
            {
                uIButton.SetScale(new Vector2(
                    Transform.Scale.X - ItemPadding,
                    item.Transform.Scale.Y
                ));
                continue;
            }

            item.Transform.Scale = new Vector2(
                Transform.Scale.X - ItemPadding,
                item.Transform.Scale.Y
            );
        }

        LayoutHelper.Vertical(ChildObjects, new(bounds.Left + ItemPadding / 2, currentY), ItemPadding);

        foreach (var item in ChildObjects)
        {
            item.IsVisible = IsItemVisible(item);
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
        if (!IsHovering || MaxScrollOffset <= 0f) return;

        float scrollDelta = -wheel.Y * 12f;
        ScrollOffset = Math.Clamp(ScrollOffset + scrollDelta, 0f, MaxScrollOffset);
        RecalcLayout();
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
        /*
        foreach(var item in ChildObjects)
        {
            item.Render();
        }
        */
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