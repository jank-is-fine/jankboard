using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;

/// <summary>
/// <para>Based on Silk.Net examples</para> 
/// <para>See <see cref="https://github.com/dotnet/Silk.NET/tree/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%202.2%20-%20Camera"/></para>
/// </summary>

public static class Camera
{
    public static Vector2 Position = Vector2.Zero;
    public static float Zoom { get; set; } = 1.0f;
    public static Vector2D<int> ViewportSize { get; private set; }

    public static void Init(Vector2D<int> initialViewportSize)
    {
        ViewportSize = initialViewportSize;
    }

    public static void ResetView()
    {
        Position = new(0, 0);
        Zoom = 1;
    }

    public static void Resize(Vector2D<int> newSize)
    {
        ViewportSize = newSize;
    }

    public static Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(
            new Vector3(Position.X, Position.Y, 1.0f),
            new Vector3(Position.X, Position.Y, 0.0f),
            Vector3.UnitY
        );
    }

    public static Matrix4x4 GetProjectionMatrix()
    {
        float halfWidth = ViewportSize.X / 2f / Zoom;
        float halfHeight = ViewportSize.Y / 2f / Zoom;

        return Matrix4x4.CreateOrthographicOffCenter(
            -halfWidth, halfWidth,
            -halfHeight, halfHeight,
            -1, 1f
        );
    }

    public static Matrix4x4 GetStationalViewMatrix()
    {
        return Matrix4x4.CreateLookAt(
            new Vector3(0, 0, 1.0f),
            new Vector3(0, 0, 0.0f),
            Vector3.UnitY
        );

    }

    public static Matrix4x4 GetStationalProjectionMatrix()
    {
        return Matrix4x4.CreateOrthographicOffCenter(
            0, ViewportSize.X,
            ViewportSize.Y, 0,
            -1, 1
        );
    }

    public static RectangleF GetVisibleWorldArea()
    {
        float halfWidth = ViewportSize.X / 2f / Zoom;
        float halfHeight = ViewportSize.Y / 2f / Zoom;

        return new RectangleF(
            Position.X - halfWidth,
            Position.Y - halfHeight,
            halfWidth * 2f,
            halfHeight * 2f
        );
    }

    public static Vector2 ScreenToWorld(Vector2 screenPos)
    {
        Vector2 ndc = new(
            screenPos.X / ViewportSize.X * 2f - 1f,
            1f - screenPos.Y / ViewportSize.Y * 2f
        );

        Vector2 worldPos = new(
            ndc.X * (ViewportSize.X / 2f) / Zoom + Position.X,
            ndc.Y * (ViewportSize.Y / 2f) / Zoom + Position.Y
        );

        return worldPos;
    }

    public static Vector2 ScreenToWorld(Vector2 screenPos, float zoom)
    {
        Vector2 ndc = new(
            screenPos.X / ViewportSize.X * 2f - 1f,
            1f - screenPos.Y / ViewportSize.Y * 2f
        );

        Vector2 worldPos = new(
            ndc.X * (ViewportSize.X / 2f) / zoom + Position.X,
            ndc.Y * (ViewportSize.Y / 2f) / zoom + Position.Y
        );

        return worldPos;
    }

    public static Vector2 WorldToScreen(Vector2 worldPos)
    {
        Vector2 ndc = new(
            (worldPos.X - Position.X) * Zoom / (ViewportSize.X / 2f),
            (worldPos.Y - Position.Y) * Zoom / (ViewportSize.Y / 2f)
            );

        Vector2 screenPos = new(
            (ndc.X + 1f) * ViewportSize.X / 2f,
            (1f - ndc.Y) * ViewportSize.Y / 2f
        );

        return screenPos;
    }

    public static void Pan(Vector2 screenDelta)
    {
        Position += new Vector2(-screenDelta.X / Zoom, screenDelta.Y / Zoom);
    }

}