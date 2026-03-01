using System.Diagnostics;

public static class UndoRedoManager
{
    private static int _currentIndex = -1;
    private static readonly List<UndoRedoAction> _actionStack = [];

    public static bool CanUndo => _currentIndex >= 0;
    public static bool CanRedo => _currentIndex < _actionStack.Count - 1;

    public static void ActionExecuted(UndoRedoAction action)
    {
        // Remove any actions after current position (in case we undid some actions)
        if (_currentIndex < _actionStack.Count - 1)
        {
            _actionStack.RemoveRange(_currentIndex + 1, _actionStack.Count - _currentIndex - 1);
        }

        _actionStack.Add(action);
        _currentIndex = _actionStack.Count - 1;

        EnforceStackLimit();
    }

    private static void EnforceStackLimit()
    {
        if (_actionStack.Count > Settings.RedoStackLimit)
        {
            int itemsToRemove = _actionStack.Count - Settings.RedoStackLimit;

            for (int i = 0; i < itemsToRemove; i++)
            {
                foreach (var cleanup in _actionStack[i].CleanupAction)
                {
                    cleanup.Invoke();
                }
            }

            _actionStack.RemoveRange(0, itemsToRemove);
            _currentIndex -= itemsToRemove;
            if (_currentIndex < -1) _currentIndex = -1;
        }
    }

    public static void Undo()
    {
        if (!CanUndo) return;

        try
        {
            var action = _actionStack[_currentIndex];
            foreach (var a in action.UndoAction)
            {
                a.Invoke();
            }
            _currentIndex--;
        }
        catch (Exception ex)
        {
            Logger.Log("UndoRedoManager", $"Undo failed: {ex.Message}\nClearing Undo/Redo stacks", LogLevel.ERROR);
            Clear();
        }
    }

    public static void Redo()
    {
        if (!CanRedo) return;

        try
        {
            _currentIndex++;
            var action = _actionStack[_currentIndex];
            foreach (var a in action.RedoAction)
            {
                a.Invoke();
            }
        }
        catch (Exception ex)
        {
            Logger.Log("UndoRedoManager", $"Redo failed: {ex.Message}\nClearing Undo/Redo stacks", LogLevel.ERROR);
            Clear();
        }
    }

    public static void Clear(bool ExecutePopActions = true)
    {
        if (ExecutePopActions)
        {
            foreach (var action in _actionStack)
            {
                foreach (var cleanupAction in action.CleanupAction)
                {
                    cleanupAction?.Invoke();
                }
            }
        }
        _actionStack.Clear();
        _currentIndex = -1;
    }

    public static void TestUndoRedo()
    {
        Logger.Log("UndoRedoManager", $"Undo Testing, Clearing Undo/Redo stacks and testing", LogLevel.INFO);
        Clear();

        for (int i = 1; i <= 10; i++)
        {
            int actionNumber = i;

            var action = new UndoRedoAction(
                undoActions: [() => Debug.WriteLine($"Undone action {actionNumber}")],
                redoActions: [() => Debug.WriteLine($"Redone action {actionNumber}")]
            );

            ActionExecuted(action);
        }

        Debug.WriteLine("--- Testing Undo ---");

        while (CanUndo)
        {
            Undo();
        }

        Debug.WriteLine("--- Testing Redo ---");

        while (CanRedo)
        {
            Redo();
        }
    }
}

public class UndoRedoAction(List<Action> undoActions, List<Action> redoActions, List<Action>? PopActions = null)
{
    public List<Action> UndoAction { get; } = undoActions;
    public List<Action> RedoAction { get; } = redoActions;
    public List<Action> CleanupAction { get; } = PopActions ?? [];
}