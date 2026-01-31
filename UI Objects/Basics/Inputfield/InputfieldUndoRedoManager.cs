using System.Diagnostics;

namespace Rendering.UI
{
    public partial class InputField : UIObject
    {
        private record InputState(string Content, int CursorPosition, int SelectionStart, int SelectionEnd);

        private Stack<InputState> _undoStack = new();
        private Stack<InputState> _redoStack = new();
        private const double UNDO_TIME_INTERVAL = 0.5;
        private const int UNDO_STACK_SIZE_LIMIT = 100;
        private readonly Stopwatch _undoRedoTimer = new();
        private InputState? _lastSavedState = null;

        private void TakeSnapshot(bool forceNewSnapshot = false)
        {
            if (!forceNewSnapshot && !IsSelected) return;

            var currentState = new InputState(Content, CursorPosition, SelectionStart, SelectionEnd);

            if (_lastSavedState == currentState) return;

            bool shouldGroup = !forceNewSnapshot &&
                              _undoRedoTimer.IsRunning &&
                              _undoRedoTimer.Elapsed.TotalSeconds < UNDO_TIME_INTERVAL;

            if (!shouldGroup && _lastSavedState != null)
            {
                _undoStack.Push(_lastSavedState);
                _redoStack.Clear();
            }

            _lastSavedState = currentState;
            _undoRedoTimer.Restart();

             if (_undoStack.Count > UNDO_STACK_SIZE_LIMIT)
            {
                var list = new List<InputState>(_undoStack);
                list.RemoveAt(list.Count - 1);
                _undoStack = new Stack<InputState>(list);
            }
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;

            if (_lastSavedState != null)
            {
                _redoStack.Push(_lastSavedState);
            }

            if (_undoStack.TryPop(out var prevState))
            {
                ApplyState(prevState);
                _lastSavedState = prevState;
            }

            _undoRedoTimer.Restart();
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            if (_lastSavedState != null)
            {
                _undoStack.Push(_lastSavedState);
            }

            if (_redoStack.TryPop(out var nextState))
            {
                ApplyState(nextState);
                _lastSavedState = nextState;
            }

            _undoRedoTimer.Restart();
        }

        private void ApplyState(InputState state)
        {
            if (Content != state.Content)
            {
                Content = state.Content;
                ContentChanged?.Invoke(Content);
            }

            CursorPosition = state.CursorPosition;
            SelectionStart = state.SelectionStart;
            SelectionEnd = state.SelectionEnd;
            CursorPositionChanged = true;
        }

        private void ClearUndoRedoStack()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _lastSavedState = null;
            _undoRedoTimer.Reset();
        }
    }
}