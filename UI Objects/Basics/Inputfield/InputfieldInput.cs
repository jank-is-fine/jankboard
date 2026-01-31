using System.Diagnostics;
using System.Numerics;
using Managers;
using Silk.NET.Input;

namespace Rendering.UI
{
    public partial class InputField : UIObject
    {
        private bool CursorPositionChanged { get; set; } = false;
        private int cursorPosition = 0;
        public int CursorPosition
        {
            get { return cursorPosition; }
            set
            {
                cursorPosition = value;
                CursorPositionChanged = true;
            }
        }
        public List<char> AllowedCharacters = [];
        public List<char> DisallowedCharacters = [];
        private IMouse mouse = InputDeviceHandler.primaryMouse!;
        private bool _isKeyPressed = false;
        private bool _repeating = false;
        private char? CurrentRepeatChar = null;
        private Stopwatch _keyRepeatTimer;
        private const float _keyRepeatInterval = 45;
        private const float _keyRepeatIntervalStart = 500;


        public void HandleKeyPress(IKeyboard keyboard, Key key, int arg3)
        {
            if (!IsSelected) return;

            switch (key)
            {
                case Key.Y when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    Undo();
                    break;
                case Key.Z when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    Redo();
                    break;

                case Key.Backspace when InputDeviceHandler.IsKeyPressed(Key.ControlLeft) || InputDeviceHandler.IsKeyPressed(Key.ControlRight):
                    if (HasSelection)
                    {
                        int start = Math.Min(SelectionStart, SelectionEnd);
                        int end = Math.Max(SelectionStart, SelectionEnd);
                        Content = Content.Remove(start, end - start);
                        CursorPosition = start;
                        ClearSelection();
                        TakeSnapshot(forceNewSnapshot: true);
                    }
                    else if (CursorPosition > 0)
                    {
                        FindPreviousWordStart(CursorPosition--);
                        SelectWordAtCursor();
                        int start = Math.Min(SelectionStart, SelectionEnd);
                        int end = Math.Max(SelectionStart, SelectionEnd);
                        Content = Content.Remove(start, end - start);
                        CursorPosition = start;
                        ClearSelection();
                        TakeSnapshot();
                    }
                    break;

                case Key.Backspace:
                    if (HasSelection)
                    {
                        int start = Math.Min(SelectionStart, SelectionEnd);
                        int end = Math.Max(SelectionStart, SelectionEnd);
                        Content = Content.Remove(start, end - start);
                        CursorPosition = start;
                        ClearSelection();
                        TakeSnapshot(forceNewSnapshot: true);
                    }
                    else if (CursorPosition > 0)
                    {
                        Content = Content.Remove(CursorPosition - 1, 1);
                        CursorPosition--;
                        TakeSnapshot();
                    }
                    break;

                case Key.Delete when InputDeviceHandler.IsKeyPressed(Key.ControlLeft) || InputDeviceHandler.IsKeyPressed(Key.ControlRight):
                    if (HasSelection)
                    {
                        int start = Math.Min(SelectionStart, SelectionEnd);
                        int end = Math.Max(SelectionStart, SelectionEnd);
                        Content = Content.Remove(start, end - start);
                        CursorPosition = start;
                        ClearSelection();
                        TakeSnapshot(forceNewSnapshot: true);
                    }
                    else if (CursorPosition < Content.Length)
                    {
                        CursorPosition = FindNextWordStart(CursorPosition++);
                        SelectWordAtCursor();
                        if (SelectionStart > 0)
                        {
                            SelectionStart--;
                        }
                        int start = Math.Min(SelectionStart, SelectionEnd);
                        int end = Math.Max(SelectionStart, SelectionEnd);
                        Content = Content.Remove(start, end - start);
                        CursorPosition = start;
                        ClearSelection();
                        TakeSnapshot();
                    }
                    break;

                case Key.Delete:
                    if (HasSelection)
                    {
                        int start = Math.Min(SelectionStart, SelectionEnd);
                        int end = Math.Max(SelectionStart, SelectionEnd);
                        Content = Content.Remove(start, end - start);
                        CursorPosition = start;
                        ClearSelection();
                        TakeSnapshot(forceNewSnapshot: true);
                    }
                    else if (CursorPosition < Content.Length)
                    {
                        Content = Content.Remove(CursorPosition, 1);
                        TakeSnapshot();
                    }
                    break;

                case Key.Up when InputDeviceHandler.IsKeyPressed(Key.ShiftLeft):
                    MoveToLineUp(true);
                    break;

                case Key.Up:
                    MoveToLineUp();
                    break;

                case Key.Down when InputDeviceHandler.IsKeyPressed(Key.ShiftLeft):
                    MoveToLineDown(true);
                    break;

                case Key.Down:
                    MoveToLineDown();
                    break;

                case Key.Left when InputDeviceHandler.IsKeyPressed(Key.ControlLeft) && InputDeviceHandler.IsKeyPressed(Key.ShiftLeft):
                    MoveToWordLeft(true);
                    break;

                case Key.Left when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    MoveToWordLeft();
                    break;

                case Key.Left:
                    if (HasSelection)
                    {
                        if (SelectionStart - 1 >= 0)
                        {
                            CursorPosition = SelectionStart--;
                        }
                        else
                        {
                            CursorPosition = SelectionStart;
                        }
                    }
                    else if (CursorPosition > 0)
                    {
                        CursorPosition--;
                    }

                    ClearSelection();
                    break;

                case Key.Right when InputDeviceHandler.IsKeyPressed(Key.ControlLeft) && InputDeviceHandler.IsKeyPressed(Key.ShiftLeft):
                    MoveToWordRight(true);
                    break;

                case Key.Right when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    MoveToWordRight();
                    break;

                case Key.Right:
                    if (HasSelection)
                    {
                        if (SelectionEnd + 1 < Content.Length)
                        {
                            CursorPosition = SelectionStart++;
                        }
                        else
                        {
                            CursorPosition = SelectionEnd;
                        }
                    }
                    else if (CursorPosition < Content.Length)
                    {
                        CursorPosition++;
                    }

                    ClearSelection();
                    break;

                case Key.C when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    CopyToClipboard();
                    break;

                case Key.X when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    CutToClipboard();
                    break;

                case Key.V when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    PasteFromClipboard();
                    break;

                case Key.Home:
                    CursorPosition = 0;
                    ClearSelection();
                    break;

                case Key.End:
                    CursorPosition = Content.Length;
                    ClearSelection();
                    break;

                case Key.A when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    SelectionStart = 0;
                    SelectionEnd = Content.Length;
                    break;

                case Key.Enter or Key.KeypadEnter when InputDeviceHandler.IsKeyPressed(Key.ControlLeft):
                    Submit();
                    break;

                case Key.Enter or Key.KeypadEnter:
                    if (AllowedCharacters.Count > 0 && !AllowedCharacters.Contains('\n')) { break; }
                    if (DisallowedCharacters.Count > 0 && DisallowedCharacters.Contains('\n')) { break; }

                    HandleTextInputKeydown(keyboard, '\n');
                    break;
            }
            RenderCursor(true);
            ContentChanged?.Invoke(Content);
        }

        public void HandleTextInputKeydown(IKeyboard? keyboard, char character)
        {
            if (!IsSelected || _isDraggingSelection) return;
            if (AllowedCharacters.Count > 0 && !AllowedCharacters.Contains(character)) { return; }
            if (DisallowedCharacters.Count > 0 && DisallowedCharacters.Contains(character)) { return; }

            if (MaxCharAmount > 0)
            {
                string ContentTest = Content;
                if (HasSelection)
                {
                    int start = Math.Min(SelectionStart, SelectionEnd);
                    int end = Math.Max(SelectionStart, SelectionEnd);
                    ContentTest = Content.Remove(start, end - start);
                }

                if (ContentTest.Length + 1 > MaxCharAmount)
                {
                    return;
                }
            }

            if (HasSelection)
            {
                int start = Math.Min(SelectionStart, SelectionEnd);
                int end = Math.Max(SelectionStart, SelectionEnd);
                Content = Content.Remove(start, end - start);
                CursorPosition = start;
                ClearSelection();
            }
            Content = Content.Insert(CursorPosition, character.ToString());
            CursorPosition++;
            ContentChanged?.Invoke(Content);

            _isKeyPressed = true;
            CurrentRepeatChar = character;
            _keyRepeatTimer.Reset();
            _keyRepeatTimer.Start();
            TakeSnapshot();
        }

        public void HandleKeyUp(IKeyboard keyboard, Key key, int arg3)
        {
            _isKeyPressed = false;
            CurrentRepeatChar = null;
            _repeating = false;
        }

        private void HandleMouseDown(IMouse mouse, MouseButton button)
        {
            if (!IsVisible || !IsSelectable) { return; }
            if (!ContainsPoint(Camera.ScreenToWorld(mouse.Position))) { ClearSelection(); IsSelected = false; return; }

            Vector2 worldPos = Camera.ScreenToWorld(mouse.Position);

            if (button == MouseButton.Left)
            {
                IsSelected = true;
                _isDraggingSelection = true;
                SetCursorFromWorldPosition(worldPos);
                StartDragSelection(worldPos);
            }
        }

        private void HandleMouseUp(IMouse mouse, MouseButton button)
        {
            if (button == MouseButton.Left && _isDraggingSelection)
            {
                EndDragSelection();
            }
        }

        private void HandleMouseMove(IMouse mouse, Vector2 position)
        {
            Vector2 worldPos = Camera.ScreenToWorld(position);

            if (_isDraggingSelection && IsSelected)
            {
                UpdateDragSelection(worldPos);
            }
        }

        private void HandleDoubleClick(IMouse mouse, MouseButton button, Vector2 position)
        {
            if (!IsVisible || !IsSelectable || button != MouseButton.Left) return;

            Vector2 worldPos = Camera.ScreenToWorld(position);
            if (ContainsPoint(worldPos) && IsSelected)
            {
                SelectWordAtCursor();
            }
        }

        public void StartDragSelection(Vector2 worldStartPos)
        {
            _isDraggingSelection = true;
            _dragStartPosition = GetCursorPositionFromWorld(worldStartPos);
            CursorPosition = _dragStartPosition;
            ClearSelection();
        }

        public void UpdateDragSelection(Vector2 worldCurrentPos)
        {
            if (_isDraggingSelection)
            {
                int currentPos = GetCursorPositionFromWorld(worldCurrentPos);
                CursorPosition = currentPos;
                SelectionStart = Math.Min(_dragStartPosition, currentPos);
                SelectionEnd = Math.Max(_dragStartPosition, currentPos);
            }
        }


        public void EndDragSelection()
        {
            _isDraggingSelection = false;
        }


        public override void OnClick(Vector2 pos)
        {
            if (IsSelectable && !IsSelected)
            {
                IsSelected = true;
                CursorPosition = Content.Length;
                _undoRedoTimer.Reset();
            }
            ClearSelection();
        }

        public void OnClick()
        {
            if (IsSelectable && !IsSelected)
            {
                IsSelected = true;
                CursorPosition = Content.Length;
                ClearSelection();
            }
            _undoRedoTimer.Start();
        }

        public void OnDrag(Vector2 dragStart, Vector2 dragEnd)
        {
            if (IsSelected && !_repeating)
            {
                if (!_isDraggingSelection)
                {
                    StartDragSelection(dragStart);
                }
                UpdateDragSelection(dragEnd);
            }
        }

        public override void OnHoverStart()
        {
            mouse.Cursor.StandardCursor = StandardCursor.IBeam;
        }

        public override void OnHoverEnd()
        {
            mouse.Cursor.StandardCursor = StandardCursor.Default;
        }


    }
}