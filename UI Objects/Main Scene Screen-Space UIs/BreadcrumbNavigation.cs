using System.Numerics;
using Managers;
using Rendering.UI;

public class Breadcrumb : UIImage
{
    private int BreadcrumbLimit = 3; //3 past and +1 the root layer as default
    private const float Padding = 10f;
    public Breadcrumb() : base(screenSpace: true, nineSlice: true) { UpdateCrumbLimit(); }

    public void Clear()
    {
        foreach (var child in ChildObjects)
        {
            child.IsVisible = false;
            child.Dispose();
        }
        ChildObjects.Clear();
        IsSelectable = false;
    }

    private void UpdateCrumbLimit()
    {
        var regularWidth = TextHelper.GetFormattedTextWidth($"[b]{new string('A', 15)}[/b]");

        BreadcrumbLimit = (int)float.Floor(Transform.Scale.X / regularWidth) - 1; // - 1 for the "MoreButton"/ellipse indicator
    }

    public void UpdateCrumbNavigation(Guid targetEntryGuid)
    {
        Clear(); UpdateCrumbLimit();
        if (targetEntryGuid == Guid.Empty)
        {
            IsVisible = false;
            return;
        }

        IsVisible = true;

        List<UIButton> breadcrumbs = [];
        Guid? currentGuid = targetEntryGuid;

        Entry? currentEntry = EntryManager.GetEntryByGuid(targetEntryGuid);
        if (currentEntry == null)
        {
            IsVisible = false;
            return;
        }

        currentGuid = currentEntry.ParentEntryGuid;

        for (int i = 0; i < BreadcrumbLimit; i++)
        {
            if (currentGuid == Guid.Empty || currentGuid == default)
            {
                break;
            }

            Entry? entry = EntryManager.GetEntryByGuid((Guid)currentGuid);
            if (entry == null)
            {
                break;
            }

            string buttonContent = entry.Content;
            buttonContent = buttonContent.Replace('\n', ' ');

            if (buttonContent.Length > 15)
            {
                buttonContent = buttonContent[..15] + "...";
            }

            var navButton = new UIButton(buttonContent, [() => EntryManager.LoadEntryLayer(entry.guid)])
            {
                IsDraggable = false
            };
            navButton.RecalcSize();

            breadcrumbs.Add(navButton);

            currentGuid = entry.ParentEntryGuid;

            if (!EntryManager.DoesEntryExist((Guid)currentGuid))
            {
                break;
            }
        }

        var rootButton = new UIButton("[b]Back to Root[/b]", [() => EntryManager.LoadEntryLayer(null)])
        {
            IsDraggable = false
        };
        ChildObjects.Add(rootButton);

        if (breadcrumbs.Count >= BreadcrumbLimit && currentGuid != Guid.Empty && currentGuid != default)
        {
            var MoreButton = new UIButton("[b]...[/b]", [() => EntryManager.LoadEntryLayer(null)], textAnchorPoint: TextAnchorPoint.Center_Center)
            {
                IsDraggable = false,
                IsSelectable = false
            };
            MoreButton.RecalcSize();

            ChildObjects.Add(MoreButton);
        }

        breadcrumbs.Reverse();
        ChildObjects.AddRange(breadcrumbs);

        foreach (var btn in ChildObjects)
        {
            btn.RecalcSize();
        }

        RecalcSize();
    }


    public override void RecalcSize()
    {
        if (ChildObjects.Count < 1) { return; }
        Vector2 buttonMaxSize = LayoutHelper.CalculateMaxSize(ChildObjects);

        ChildObjects.ForEach(x => x.Transform.Scale = buttonMaxSize);

        Transform.Scale = new(Camera.ViewportSize.X * 0.9f, buttonMaxSize.Y + Padding * 2f);

        Transform.Position = new(Camera.ViewportSize.X / 2f, Camera.ViewportSize.Y - (Padding * 2f) - Transform.Scale.Y / 2f);

        LayoutHelper.Horizontal(ChildObjects, new(Transform.Position.X + 5f - Transform.Scale.X / 2f, Transform.Position.Y - buttonMaxSize.Y / 2f), Padding);
    }


    public override void Render()
    {
        base.Render();
        foreach (var child in ChildObjects)
        {
            child.Render();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        Clear();
    }

}