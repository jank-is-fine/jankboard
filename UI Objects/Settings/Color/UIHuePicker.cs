using System.Drawing;
using System.Numerics;
using Managers;
using Rendering.UI;
using Silk.NET.OpenGL;

public class UIHuePicker : UIImage
{
    public UIImage PickerTexture;
    public Vector2 PickedPercent;
    public Action<Vector2, bool>? ValueChangedAction;
    private Color _hueColor = Color.Red;
    public Color HueColor
    {
        get => _hueColor;
        set
        {
            _hueColor = value;
        }
    }

    public UIHuePicker(Vector2? StartingColor = null) : base(texture: null, screenSpace: true)
    {
        Shader = ShaderManager.GetShaderByName("Color-Picker Shader");
        PickerTexture = new(TextureHandler.GetEmbeddedTextureByName("ColorPickerTexture.png"), true)
        {
            TextureColor = Color.White,
            IsDraggable = true,
            IsScreenSpace = true,
            RenderOrder = 6
        };

        PickerTexture.DragAction += OnPickerDrag;
        PickerTexture.DragEndAction += OnPickerDragEnd;

        PickerTexture.Transform.Position = Transform.Position;
        PickedPercent = StartingColor ?? new(0, 0);
        RenderOrder = 0;
        ChildObjects.Add(PickerTexture);
    }

    public void SetValue(Vector2 value)
    {
        value.X = Math.Clamp(value.X, 0, 1);
        value.Y = Math.Clamp(value.Y, 0, 1);

        PickedPercent = value;

        float minX = Transform.Position.X - Transform.Scale.X / 2;
        float maxY = Transform.Position.Y + Transform.Scale.Y / 2;

        float posX = minX + (value.X * Transform.Scale.X);
        float posY = maxY - (value.Y * Transform.Scale.Y);

        PickerTexture.Transform.Position = new Vector2(posX, posY);
    }

    public override void OnClick(Vector2 pos)
    {
        PickerTexture.Transform.Position = pos;
        ClampPickerPos();
        CalculatePickedPos(true);
    }

    public void OnPickerDrag()
    {
        PickerDragged(false);
    }

    public void OnPickerDragEnd()
    {
        PickerDragged(true);
    }

    public void ClampPickerPos()
    {
        float xCheck = PickerTexture.Transform.Position.X;
        float yCheck = PickerTexture.Transform.Position.Y;

        if (xCheck < Transform.Position.X - Transform.Scale.X / 2)
        {
            xCheck = Transform.Position.X - Transform.Scale.X / 2;
        }
        else if (xCheck > Transform.Position.X + Transform.Scale.X / 2)
        {
            xCheck = Transform.Position.X + Transform.Scale.X / 2;
        }

        if (yCheck < Transform.Position.Y - Transform.Scale.Y / 2)
        {
            yCheck = Transform.Position.Y - Transform.Scale.Y / 2;
        }
        else if (yCheck > Transform.Position.Y + Transform.Scale.Y / 2)
        {
            yCheck = Transform.Position.Y + Transform.Scale.Y / 2;
        }

        PickerTexture.Transform.Position = new(xCheck, yCheck);
    }

    public void PickerDragged(bool FinalPick)
    {
        ClampPickerPos();
        CalculatePickedPos(FinalPick);
    }

    private void CalculatePickedPos(bool FinalPick)
    {
        float minX = Transform.Position.X - Transform.Scale.X / 2;
        float minY = Transform.Position.Y - Transform.Scale.Y / 2;
        float maxY = Transform.Position.Y + Transform.Scale.Y / 2;

        float relativeX = PickerTexture.Transform.Position.X - minX;
        float relativeY = PickerTexture.Transform.Position.Y - minY;

        PickedPercent = new Vector2(
            relativeX / Transform.Scale.X,
            1f - (relativeY / Transform.Scale.Y)
        );

        ValueChangedAction?.Invoke(PickedPercent, FinalPick);
    }

    public void RecalSize()
    {
        var scale = Transform.Scale / 10;
        PickerTexture.Transform.Scale = new(scale.Y, scale.Y);

        SetValue(PickedPercent);
        ClampPickerPos();
    }

    public override void Render()
    {
        if (!IsVisible) return;

        _vao.Bind();
        Shader?.Use();

        Shader?.SetUniform("uTexture0", 0);
        if (IsScreenSpace)
        {
            Shader?.SetUniform("uView", Camera.GetStationalViewMatrix());
            Shader?.SetUniform("uProjection", Camera.GetStationalProjectionMatrix());
        }
        else
        {
            Shader?.SetUniform("uView", Camera.GetViewMatrix());
            Shader?.SetUniform("uProjection", Camera.GetProjectionMatrix());
        }
        Shader?.SetUniform("uModel", Transform.ViewMatrix);
        Shader?.SetUniform("uColor", TextureColor.R / 255.0f, TextureColor.G / 255.0f, TextureColor.B / 255.0f, TextureColor.A / 255.0f);

        Shader?.SetUniform("uPickerColor", _hueColor.R / 255.0f, _hueColor.G / 255.0f, _hueColor.B / 255.0f, 1.0f);

        Texture?.Bind();

        gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

        var error = gl.GetError();
        if (error != GLEnum.NoError)
        {
            //Logging every frame would be bad. No logging
            Console.WriteLine($"OpenGL Error after drawing: {error}");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        PickerTexture.DragAction -= OnPickerDrag;
        PickerTexture.DragEndAction -= OnPickerDragEnd;
    }
}