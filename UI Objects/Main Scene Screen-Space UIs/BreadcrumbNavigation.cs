using Managers;
using Rendering.UI;

public class Breadcrumb : UIImage
{
    private const float Padding = 10f;
    public bool minimized { get; private set; } = false;

    private UIButton rootButton = new
    (
        "[b]Go back to Root[/b]",
        [() => EntryManager.LoadEntryLayer(null)]
    )
    { IsDraggable = false };

    private UIButton ellipsebutton = new
    (
        "[b]...[/b]",
        [() => EntryManager.LoadEntryLayer(null)],
        textAnchorPoint: TextAnchorPoint.Center_Center
    )
    { IsDraggable = false, IsSelectable = false };

    private Texture? MinimizeTexture = TextureHandler.GetEmbeddedTextureByName("check_square_grey_cross-edited.png");
    private Texture? MaximizeTexture = TextureHandler.GetEmbeddedTextureByName("up.png");
    public UIButton StateToggle { get; private set; }

    public List<UIObject> BreadCrumbs { get; private set; } = [];

    public Breadcrumb() : base(screenSpace: true, nineSlice: true)
    {
        StateToggle = new UIButton(" ", [], nineSlice: false, recalcSize: false)
        {
            IsScreenSpace = true,
            IsDraggable = false,
            RenderOrder = 51,
            Texture = MinimizeTexture,
            Transform = { Scale = new(32f, 32f) }
        };

        StateToggle.actions.AddRange(
        [
            () => minimized = !minimized,
            () => StateToggle.Texture = minimized ? MaximizeTexture : MinimizeTexture,
            () => RecalcSize(),
        ]);

        rootButton.RecalcSize();
        ellipsebutton.RecalcSize();
        RecalcSize();
        StateToggle.Transform.Scale = new(Transform.Scale.Y, Transform.Scale.Y);

        ChildObjects.AddRange(rootButton, ellipsebutton);
    }

    public void Clear()
    {
        foreach (var child in BreadCrumbs)
        {
            child.IsVisible = false;
            child.Dispose();
        }
        BreadCrumbs.Clear();
        IsSelectable = false;
    }

    public void UpdateCrumbNavigation(Guid targetEntryGuid)
    {
        Clear();

        if (targetEntryGuid == Guid.Empty)
        {
            IsVisible = false;
            return;
        }

        IsVisible = true;

        Entry? currentEntry = EntryManager.GetEntryByGuid(targetEntryGuid);
        if (currentEntry == null)
        {
            IsVisible = false;
            return;
        }

        var entriesStartingAtTarget = EntryManager.GetHiarchyFromGuid(currentEntry.ParentEntryGuid);

        var entriesStartingFromRoot = entriesStartingAtTarget.AsEnumerable().Reverse().ToList();

        float neededWidth = entriesStartingAtTarget.Count * (rootButton.Transform.Scale.X + Padding) + StateToggle.Transform.Scale.X + (Padding * 2);

        bool ellipseNeeded = neededWidth > Transform.Scale.X - StateToggle.Transform.Scale.X;
        ellipsebutton.IsVisible = ellipseNeeded;

        float availableForCrumbs = Transform.Scale.X;
        availableForCrumbs -= rootButton.Transform.Scale.X;
        availableForCrumbs -= ellipseNeeded ? ellipsebutton.Transform.Scale.X : 0;
        availableForCrumbs -= StateToggle.Transform.Scale.X;
        availableForCrumbs -= Padding * 4;
        availableForCrumbs = Math.Max(0, availableForCrumbs);

        if (ellipseNeeded)
        {
            // start from target in direction to root, stop when we do not have space left
            // that way we get N last ancestors until we run out of space 

            BreadCrumbs =
            [
                ..
                CreateCrumbButtons
                (
                    entriesStartingAtTarget,
                    maxTotalWidth: availableForCrumbs
                )
            ];

            LayoutHelper.Horizontal
            (
                [rootButton, ellipsebutton, .. BreadCrumbs.AsEnumerable().Reverse()], // reverse the order to start with the oldest ancestor to display
                new
                (
                    Transform.Position.X + 5f - Transform.Scale.X / 2f,
                    Transform.Position.Y - rootButton.Transform.Scale.Y / 2f
                ),
                Padding
            );
        }
        else
        {
            BreadCrumbs = [.. CreateCrumbButtons(entriesStartingFromRoot)];

            LayoutHelper.Horizontal
            (
                [rootButton, .. BreadCrumbs],
                new
                (
                    Transform.Position.X + 5f - Transform.Scale.X / 2f,
                    Transform.Position.Y - rootButton.Transform.Scale.Y / 2f
                ),
                Padding
            );
        }

    }

    private string GetButtonText(Entry entry)
    {
        ParsedText parsed;

        if (entry.Content.Length >= 50)
        {
            parsed = TextFormatParser.ParseText(entry.Content[..50]);
        }
        else
        {
            parsed = TextFormatParser.ParseText(entry.Content);
        }

        var rawText = string.Concat(parsed.lines.SelectMany(line => line.lineSegments.Select(segment => segment.Text)));

        return rawText.Length > 15 ? rawText[..12] + "..." : rawText;
    }

    private List<UIButton> CreateCrumbButtons(List<Entry> entries, float maxTotalWidth = float.MaxValue)
    {
        var buttons = new List<UIButton>();
        float accumulatedWidth = 0;
        float buttonSpacing = rootButton.Transform.Scale.X + Padding;

        foreach (var entry in entries)
        {
            if (maxTotalWidth < float.MaxValue && accumulatedWidth + buttonSpacing > maxTotalWidth) { break; }

            string displayText = GetButtonText(entry);
            var button = new UIButton(displayText, [() => EntryManager.LoadEntryLayer(entry.guid)]);
            button.SetScale(rootButton.Transform.Scale);
            buttons.Add(button);

            accumulatedWidth += buttonSpacing;
        }

        return buttons;
    }



    public override void RecalcSize()
    {
        rootButton.RecalcSize();
        ellipsebutton.SetScale(rootButton.Transform.Scale);

        Transform.Scale = new(Camera.ViewportSize.X * 0.9f, rootButton.Transform.Scale.Y + Padding * 2f);
        Transform.Position = new(Camera.ViewportSize.X / 2f, Camera.ViewportSize.Y - (Padding * 2f) - Transform.Scale.Y / 2f);

        StateToggle.Transform.Scale = new(Transform.Scale.Y, Transform.Scale.Y);
        StateToggle.Transform.Position = new
        (
            Transform.Position.X + (Transform.Scale.X / 2f) - StateToggle.Transform.Scale.X / 2f,
            Transform.Position.Y
        );

        UpdateCrumbNavigation(EntryManager.CurrentParentEntry);
    }

    public override void Render()
    {
        if (minimized && IsVisible)
        {
            StateToggle.Render();
            return;
        }

        if (!IsVisible)
        {
            return;
        }

        base.Render();

        StateToggle.Render();
        rootButton.Render();
        ellipsebutton.Render();

        foreach (var child in BreadCrumbs)
        {
            child.Render();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        Clear();

        foreach (var child in ChildObjects)
        {
            child.Dispose();
        }
        ChildObjects.Clear();
    }
}