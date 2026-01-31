using System.Numerics;
using Rendering.UI;

/// <summary>
/// <para>Provides layout algorithms for arranging UI elements in grid, vertical, and horizontal configurations and executes them</para>
/// </summary>

public static class LayoutHelper
{
    public static void Grid<T>(List<T> elements, Vector2 startPos, float maxWidth, Vector2 spacing)
    where T : UIObject
    {
        if (elements.Count == 0) return;

        Vector2 currentPos = startPos;
        float currentRowHeight = 0f;

        foreach (var element in elements)
        {
            float elementLeftEdge = currentPos.X - element.Transform.Scale.X / 2f;
            float rightEdge = currentPos.X + element.Transform.Scale.X / 2f;

            if (rightEdge > startPos.X + maxWidth)
            {
                currentPos.X = startPos.X;
                currentPos.Y += currentRowHeight + spacing.Y;
                currentRowHeight = 0f;
            }

            element.Transform.Position = new(currentPos.X + element.Transform.Scale.X / 2, currentPos.Y + element.Transform.Scale.Y / 2);

            currentPos.X += element.Transform.Scale.X + spacing.X;

            currentRowHeight = Math.Max(currentRowHeight, element.Transform.Scale.Y);
        }
    }


    public static void Vertical<T>(List<T> elements, Vector2 startPos, float spacing)
        where T : UIObject
    {
        Vector2 pos = startPos;
        foreach (var element in elements)
        {
            element.Transform.Position = new(pos.X + element.Transform.Scale.X / 2, pos.Y + element.Transform.Scale.Y / 2);
            pos.Y += element.Transform.Scale.Y + spacing;
        }
    }

    public static void Horizontal<T>(List<T> elements, Vector2 startPos, float spacing)
        where T : UIObject
    {
        Vector2 pos = startPos;
        foreach (var element in elements)
        {
            element.Transform.Position = pos + element.Transform.Scale / 2;
            pos.X += element.Transform.Scale.X + spacing;
        }
    }

    public static Vector2 CalculateMaxSize(List<UIObject> TargetObjects, bool recalcSize = true)
    {
        if (TargetObjects.Count <= 0) { return new(0, 0); }
        if (recalcSize) { TargetObjects.ForEach(x => x.RecalcSize()); }
        return new(TargetObjects.Max(x => x.Transform.Scale.X), TargetObjects.Max(x => x.Transform.Scale.Y));
    }
}
