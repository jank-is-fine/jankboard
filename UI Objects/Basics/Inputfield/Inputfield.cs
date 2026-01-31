using System.Drawing;
using System.Numerics;
using Silk.NET.OpenGL;
using Managers;
using System.Diagnostics;

namespace Rendering.UI
{
    public partial class InputField : UIObject
    {
        private VertexArrayObject<float, uint> _vao;
        public string Content { get; private set; } = "";
        public Action<string>? ContentChanged;
        public int MaxCharAmount = -1;
        public float MinWidth = 100;
        public float MinHeight = 50;
        private bool _nineSlice;
        private Vector2 _nineSliceBorder;
        private string PlaceholderText = string.Empty;
        private bool hasPlaceHolderText = false;
        private bool IsSelected = false;
        public Action? SubmitAction;
        private bool AdjustTextColor = false;
        private Color TextColor = Settings.TextColor;

        public InputField(bool nineSlice = false, Vector2? NineSliceBorder = null, string? placeholderText = null, bool adjustTextColor = false)
        {
            IsDraggable = false;
            if (nineSlice)
            {
                Shader = ShaderManager.GetShaderByName("nine-slice");
                _nineSlice = true;
                _nineSliceBorder = NineSliceBorder ?? new(47, 47);
            }
            else
            {
                Shader = ShaderManager.GetShaderByName("Default Shader");
            }

            Texture = TextureHandler.GetEmbeddedTextureByName("default.png");

            _vao = new VertexArrayObject<float, uint>(ShaderManager.gl, ShaderManager.Vbo, ShaderManager.Ebo);
            _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            cursorImage = new(Texture, false)
            {
                IsSelectable = false,
                TextureColor = Color.Black,
            };

            _selectionMesh = new();

            TextureColor = Settings.InputfieldBackgroundColor;
            _keyRepeatTimer = Stopwatch.StartNew();

            if (placeholderText != null)
            {
                PlaceholderText = placeholderText;
                hasPlaceHolderText = true;
            }

            MinHeight = TextHelper.GetStringRenderBox(string.IsNullOrEmpty(PlaceholderText) ? "|" : PlaceholderText + "|", FontType.REGULAR).Height;
            CursorPositionChanged = true;

            AdjustTextColor = adjustTextColor;
        }

        private void Submit()
        {
            SubmitAction?.Invoke();
        }

        public void SetText(string target)
        {
            Content = target;
            ClearUndoRedoStack();
            cursorPosition = Content.Length;
            TakeSnapshot(forceNewSnapshot: true);

            _undoRedoTimer.Reset();
        }

        public override void RecalcSize()
        {
            MinHeight = TextHelper.GetStringRenderBox(string.IsNullOrEmpty(PlaceholderText) ? "|" : PlaceholderText + "|", FontType.REGULAR).Height;
            if (Content.Length > 0)
            {
                RectangleF textBox = TextHelper.GetStringRenderBox(Content, FontType.REGULAR);
                float width = textBox.Width > MinWidth ? textBox.Width : MinWidth;
                float height = textBox.Height > MinHeight ? textBox.Height : MinHeight;
                Transform.Scale = new(width, height);
            }
            else
            {
                float width = MinWidth;
                float height = MinHeight;
                Transform.Scale = new(width, height);
            }
            CursorPositionChanged = true;
        }

        public new RectangleF Bounds => new(
            Transform.Position.X - Transform.Scale.X * 0.5f,
            Transform.Position.Y - Transform.Scale.Y * 0.5f,
            Transform.Scale.X,
            Transform.Scale.Y
        );

        public void Update(double deltaTime)
        {
            if (HasSelection && !IsSelected) { ClearSelection(); }
            _cursorBlinkTime += deltaTime;
            if (HasSelection && (_lastSelectionStart != SelectionStart || _lastSelectionEnd != SelectionEnd))
            {
                _lastSelectionStart = SelectionStart;
                _lastSelectionEnd = SelectionEnd;
            }

            if (_cursorBlinkTime >= CursorBlinkInterval)
            {
                _cursorVisible = !_cursorVisible;
                _cursorBlinkTime = 0;
            }

            if (_isKeyPressed && CurrentRepeatChar != null)
            {
                if (!_repeating && _keyRepeatTimer.ElapsedMilliseconds >= _keyRepeatIntervalStart)
                {
                    _repeating = true;
                }

                if (_repeating && _keyRepeatTimer.ElapsedMilliseconds >= _keyRepeatInterval)
                {
                    HandleTextInputKeydown(null, (char)CurrentRepeatChar);
                }
            }

            /* //consider adding something similar to this but... better

            var visibleArea = Camera.GetVisibleWorldArea();
            var cursorBounds = cursorImage.Bounds;

            if (cursorBounds.Right < visibleArea.Left ||
                cursorBounds.Left > visibleArea.Right ||
                cursorBounds.Bottom < visibleArea.Top ||
                cursorBounds.Top > visibleArea.Bottom)
            {
                Vector2 cameraMove = Vector2.Zero;

                if (cursorBounds.Right < visibleArea.Left)
                    cameraMove.X = cursorBounds.Left - visibleArea.Left;
                else if (cursorBounds.Left > visibleArea.Right)
                    cameraMove.X = cursorBounds.Right - visibleArea.Right;

                if (cursorBounds.Bottom < visibleArea.Top)
                    cameraMove.Y = cursorBounds.Top - visibleArea.Top;
                else if (cursorBounds.Top > visibleArea.Bottom)
                    cameraMove.Y = cursorBounds.Bottom - visibleArea.Bottom;

                Camera.Position += cameraMove;
            }

            */
        }

        public override void Render()
        {
            if (!IsVisible) return;

            if (IsSelected)
            {
                RecalcSize();
            }

            if (AdjustTextColor)
            {
                TextColor = TextHelper.GetContrastColor(TextureColor);
            }
            else
            {
                TextColor = Settings.TextColor;
            }

            // BG
            Shader?.Use();
            Shader?.SetUniform("uTexture0", 0);
            Shader?.SetUniform("uModel", Transform.ViewMatrix);
            Shader?.SetUniform("uView", Camera.GetViewMatrix());
            Shader?.SetUniform("uProjection", Camera.GetProjectionMatrix());
            Shader?.SetUniform("uColor", Settings.ColorToVec4(TextureColor));
            Texture?.Bind();

            if (_nineSlice)
            {
                Shader?.SetUniform("uDimensions", Transform.Scale);
                Shader?.SetUniform("uBorderSize", _nineSliceBorder);
            }

            _vao.Bind();
            gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            _vao.Unbind();


            Vector2 textPos = new(
                Transform.Position.X - Transform.Scale.X / 2f,
                Transform.Position.Y + Transform.Scale.Y / 2f
            );

            if (Content.Length > 0)
            {
                //txt
                TextRenderer.RenderTextWorld(
                    Content,
                    textPos,
                    Settings.ColorToVec4(TextColor),
                    Settings.TextSize
                );
            }
            else if (hasPlaceHolderText)
            {
                var placeholderTextColor = Settings.ColorToVec4(TextColor);
                placeholderTextColor.W = 0.5f;

                TextRenderer.RenderTextWorld(
                 PlaceholderText,
                 textPos,
                 placeholderTextColor,
                 Settings.TextSize
                );
            }

            if (IsSelected && _cursorVisible)
            {
                RenderCursor();
            }

            if (HasSelection && SelectionStart != SelectionEnd)
            {
                RenderSelection();
            }
        }

        public override void Dispose()
        {
            _vao.Dispose();
            UnsubActions();
        }

        public void UnsubActions()
        {
            if (mouse != null)
            {
                mouse.MouseDown -= HandleMouseDown;
                mouse.MouseUp -= HandleMouseUp;
                mouse.MouseMove -= HandleMouseMove;
                mouse.DoubleClick -= HandleDoubleClick;
            }

            WindowManager.window.Update -= Update;

            if (InputDeviceHandler.primaryKeyboard == null) { return; }
            InputDeviceHandler.primaryKeyboard.KeyDown -= HandleKeyPress;
            InputDeviceHandler.primaryKeyboard.KeyChar -= HandleTextInputKeydown;
            InputDeviceHandler.primaryKeyboard.KeyUp -= HandleKeyUp;

            _keyRepeatTimer.Reset();
            IsSelected = false;
        }

        public void SubActions()
        {
            if (mouse != null)
            {
                mouse.MouseDown += HandleMouseDown;
                mouse.MouseUp += HandleMouseUp;
                mouse.MouseMove += HandleMouseMove;
                mouse.DoubleClick += HandleDoubleClick;
            }

            WindowManager.window.Update += Update;

            if (InputDeviceHandler.primaryKeyboard == null) { return; }
            InputDeviceHandler.primaryKeyboard.KeyDown += HandleKeyPress;
            InputDeviceHandler.primaryKeyboard.KeyChar += HandleTextInputKeydown;
            InputDeviceHandler.primaryKeyboard.KeyUp += HandleKeyUp;
        }

    }
}
