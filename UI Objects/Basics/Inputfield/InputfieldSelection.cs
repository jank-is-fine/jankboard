using System.Numerics;
using Managers;
using Silk.NET.OpenGL;

namespace Rendering.UI
{
    public partial class InputField : UIObject
    {

        private UIImage cursorImage;
        private SelectionMesh _selectionMesh;
        private GL gl = ShaderManager.gl;
        private int _lastSelectionStart = 0;
        private int _lastSelectionEnd = 0;

        public int SelectionStart { get; set; } = 0;
        public int SelectionEnd { get; set; } = 0;
        public bool HasSelection => SelectionStart != SelectionEnd;
        private double _cursorBlinkTime = 0;
        private bool _cursorVisible = true;
        private const double CursorBlinkInterval = 0.5;
        private bool _isDraggingSelection = false;
        private int _dragStartPosition = 0;

        private void SetCursorFromWorldPosition(Vector2 worldPos)
        {
            float localX = worldPos.X - (Transform.Position.X - Transform.Scale.X / 2f);
            float localY = worldPos.Y - (Transform.Position.Y - Transform.Scale.Y / 2f);
            CursorPosition = GetCursorPositionFromLocal(localX, localY, false);
        }

        private int GetCursorPositionFromWorld(Vector2 worldPos)
        {
            float localX = worldPos.X - (Transform.Position.X - Transform.Scale.X / 2f);
            float localY = worldPos.Y - (Transform.Position.Y - Transform.Scale.Y / 2f);
            return GetCursorPositionFromLocal(localX, localY);
        }

        private int GetCursorPositionFromLocal(float localX, float localY, bool flipLocal = true)
        {
            float lineHeight = TextRenderer.Font.Metrics.LineHeight * Settings.TextSize;

            if (flipLocal)
            {
                localY = Transform.Scale.Y - localY;
            }

            string[] lines = Content.Split('\n');

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                float lineTop = lineIndex * lineHeight;
                float lineBottom = (lineIndex + 1) * lineHeight;

                if (localY >= lineTop && localY < lineBottom)
                {
                    int charPos = GetCharacterPositionInLine(lines[lineIndex], localX, Settings.TextSize);
                    int lineStart = GetLineStartIndex(lines, lineIndex);

                    return charPos + lineStart;
                }
            }

            return Content.Length;
        }

        private int GetCharacterPositionInLine(string line, float localX, float scale)
        {
            float currentX = 0;

            for (int i = 0; i < line.Length; i++)
            {
                float charWidth = TextHelper.GetTextWidth(line[i].ToString(), FontType.REGULAR, scale);
                float charMid = currentX + charWidth / 2;

                if (localX < charMid)
                    return i;

                currentX += charWidth;
            }

            return line.Length;
        }

        private int GetLineStartIndex(string[] lines, int lineIndex)
        {
            int startIndex = 0;
            for (int i = 0; i < lineIndex; i++)
            {
                startIndex += lines[i].Length + 1; // +1 for the newline character
            }
            return startIndex;
        }

        private void ClearSelection()
        {
            SelectionStart = CursorPosition;
            SelectionEnd = CursorPosition;
        }

        private void RenderSelection()
        {
            if (!HasSelection || SelectionStart == SelectionEnd)
                return;


            float lineHeight = TextRenderer.Font.Metrics.LineHeight * Settings.TextSize;

            string[] lines = Content.Split('\n');

            _selectionMesh.Transform.Position = Transform.Position;

            Vector2 localTextStart = new(
                -Transform.Scale.X / 2f,
                Transform.Scale.Y / 2f
            );

            _selectionMesh.UpdateSelection(lines, SelectionStart, SelectionEnd, localTextStart, lineHeight);
            _selectionMesh.Render();
        }

        private void RenderCursor(bool resetTimer = false)
        {
            if (resetTimer)
            {
                _cursorVisible = true;
                _cursorBlinkTime = 0;
            }

            if (CursorPosition < 0 || CursorPosition > Content.Length)
                return;

            if (CursorPositionChanged)
            {
                UpdateCursorPos();
                CursorPositionChanged = false;
            }

            cursorImage.Render();
        }

        private void UpdateCursorPos()
        {
            Vector2 textStartPos = new(
              Transform.Position.X - Transform.Scale.X / 2f,
              Transform.Position.Y + Transform.Scale.Y / 2f
            );

            var measurement = TextHelper.GetCursorPosition(Content, CursorPosition, textStartPos, Settings.TextSize);

            cursorImage.Transform.Scale = new Vector2(2f, measurement.LineHeight);
            cursorImage.Transform.Position = new(measurement.CursorPosition.X, measurement.CursorPosition.Y - cursorImage.Transform.Scale.Y / 2);
        }

    }
}