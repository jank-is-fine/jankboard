using Managers.Clipboard;

namespace Rendering.UI
{
    public partial class InputField : UIObject
    {
        private void PasteFromClipboard()
        {
            try
            {
                string? clipboardText = ClipboardManager.GetText();
                if (string.IsNullOrEmpty(clipboardText)) return;

                string filteredText = "";
                foreach (char c in clipboardText)
                {
                    if (AllowedCharacters.Count > 0 && !AllowedCharacters.Contains(c)) continue;
                    if (DisallowedCharacters.Count > 0 && DisallowedCharacters.Contains(c)) continue;
                    filteredText += c;
                }

                string textToPaste = filteredText;

                if (MaxCharAmount > 0)
                {
                    string contentTest = Content;
                    if (HasSelection)
                    {
                        int start = Math.Min(SelectionStart, SelectionEnd);
                        int end = Math.Max(SelectionStart, SelectionEnd);
                        contentTest = Content.Remove(start, end - start);
                    }

                    if (contentTest.Length + textToPaste.Length > MaxCharAmount)
                    {
                        int allowedChars = MaxCharAmount - contentTest.Length;
                        if (allowedChars <= 0) return;
                        textToPaste = textToPaste[..Math.Min(textToPaste.Length, allowedChars)];
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

                Content = Content.Insert(CursorPosition, textToPaste);
                CursorPosition += textToPaste.Length;
                ContentChanged?.Invoke(Content);
            }
            catch (Exception ex)
            {
                Logger.Log("InputfieldClipboard", $"Clipboard paste failed: {ex.Message}\nStacktrace:\n{ex.StackTrace}", LogLevel.ERROR);
            }

        }

        private void CopyToClipboard()
        {
            try
            {
                if (!HasSelection) return;

                int start = Math.Min(SelectionStart, SelectionEnd);
                int end = Math.Max(SelectionStart, SelectionEnd);
                string selectedText = Content[start..end];

                ClipboardManager.SetText(selectedText);
            }
            catch (Exception ex)
            {
                Logger.Log("InputfieldClipboard", $"Clipboard copy failed: {ex.Message}\nStacktrace:\n{ex.StackTrace}", LogLevel.ERROR);
            }
        }

        private void CutToClipboard()
        {
            try
            {
                if (!HasSelection) return;

                CopyToClipboard();

                int start = Math.Min(SelectionStart, SelectionEnd);
                int end = Math.Max(SelectionStart, SelectionEnd);
                Content = Content.Remove(start, end - start);
                CursorPosition = start;
                ClearSelection();
                ContentChanged?.Invoke(Content);
            }
            catch (Exception ex)
            {
                Logger.Log("InputfieldClipboard", $"Clipboard cut failed: {ex.Message}\nStacktrace:\n{ex.StackTrace}", LogLevel.ERROR);
            }
        }


    }
}