using System.Drawing;
using System.Numerics;
using Rendering.UI;

public class UISlider : UIImage
{
    private float _minVal;
    private float _maxVal;
    public float Value { get; private set; }
    private UIImage _sliderHandle;
    public Action<float, bool>? ValueChangedAction;
    private bool AutoUpdateBGColor;
    public UISlider(float minVal = 0, float maxVal = 10, float startVal = 1, Color? HandleColor = null, int renderOrder = 3, bool ContinueslyGetBGColor = true) : base()
    {
        _sliderHandle = new()
        {
            IsScreenSpace = true,
            Transform =
            {
                Position = Transform.Position,
                Scale = new(Transform.Scale.X/10,Transform.Scale.Y)
            },
            TextureColor = HandleColor ?? Color.White,
            RenderOrder = renderOrder + 1
        };
        _minVal = minVal;
        _maxVal = maxVal;
        Value = startVal;
        ChildObjects.Add(_sliderHandle);
        _sliderHandle.DragAction += HandleDrag;
        _sliderHandle.DragEndAction += HandleEndDrag;
        IsScreenSpace = true;
        IsDraggable = false;
        RenderOrder = renderOrder;

        AutoUpdateBGColor = ContinueslyGetBGColor;

        RecalcSize();
    }

    public override void OnClick(Vector2 pos)
    {
        base.OnClick(pos);
        _sliderHandle.Transform.Position = pos;
        HandleDrag();
    }

    public void HandleDrag()
    {
        ClampHandlePos();
        ValueChangedAction?.Invoke(Value, false);
    }

    public void HandleEndDrag()
    {
        ValueChangedAction?.Invoke(Value, true);
    }

    private void CalculateCurrentVal()
    {
        var bounds = Bounds;

        if (bounds.Width == 0) return;
        float HandlePosPercent = bounds.Left - _sliderHandle.Bounds.Left;
        HandlePosPercent /= -(bounds.Width - _sliderHandle.Transform.Scale.X);
        HandlePosPercent = Math.Clamp(HandlePosPercent, 0f, 1f);

        //Edge case at 0 it is "-0" 
        //if used for the AudioHandler Volume Controls it throws negative value exception
        if (HandlePosPercent == -0)
        {
            HandlePosPercent = 0;
        }
        Value = _maxVal * HandlePosPercent;
    }

    public void ClampHandlePos()
    {
        var bounds = Bounds;

        var left = bounds.Left + _sliderHandle.Transform.Scale.X / 2;
        var right = bounds.Right - _sliderHandle.Transform.Scale.X / 2;

        var X = Math.Clamp(_sliderHandle.Transform.Position.X, left, right);
        _sliderHandle.Transform.Position = new(X, Transform.Position.Y);
        CalculateCurrentVal();
    }

    public override void RecalcSize()
    {
        var bounds = Bounds;

        Value = Math.Clamp(Value, _minVal, _maxVal);
        float HandlePosX = bounds.Left + Value / _maxVal * bounds.Width;
        _sliderHandle.Transform.Position = new(HandlePosX, Transform.Position.Y);
        _sliderHandle.Transform.Scale = new(Transform.Scale.Y / 4, Transform.Scale.Y);
        ClampHandlePos();
    }

    public void SetValue(float value)
    {
        Value = value;
        Value = Math.Clamp(Value, _minVal, _maxVal);
        RecalcSize();
    }

    public override void Dispose()
    {
        base.Dispose();
        _sliderHandle.DragAction -= HandleDrag;
        _sliderHandle.DragEndAction -= HandleEndDrag;
    }

    public override void Render()
    {
        if (AutoUpdateBGColor)
        {
            TextureColor = Settings.ButtonBGColor;
            _sliderHandle.TextureColor = TextHelper.GetContrastColor(TextureColor);
        }

        base.Render();
    }

}