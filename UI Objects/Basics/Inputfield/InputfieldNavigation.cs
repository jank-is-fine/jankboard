namespace Rendering.UI
{
    public partial class InputField : UIObject
    {
        
        private void SelectWordAtCursor()
        {
            if (string.IsNullOrEmpty(Content)) return;

            int start = FindWordStart(CursorPosition);
            int end = FindWordEnd(CursorPosition);

            SelectionStart = start;
            SelectionEnd = end;

            CursorPosition = end;
        }

        private void MoveToWordLeft(bool selecting = false)
        {
            if (string.IsNullOrEmpty(Content) || CursorPosition == 0) return;

            int newPosition;

            if (CursorPosition > 0 && !char.IsWhiteSpace(Content[CursorPosition - 1]))
            {
                newPosition = FindWordStart(CursorPosition);
            }
            else
            {
                newPosition = FindPreviousWordStart(CursorPosition);
            }

            if (selecting)
            {
                if (!HasSelection)
                {
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;

                    SelectionStart = Math.Min(SelectionStart, newPosition);
                    SelectionEnd = Math.Max(SelectionEnd, newPosition);
                }
                else
                {
                    if (CursorPosition == SelectionEnd)
                    {
                        SelectionEnd = newPosition;

                        if (SelectionEnd < SelectionStart)
                        {
                            (SelectionEnd, SelectionStart) = (SelectionStart, SelectionEnd);
                        }
                    }
                    else
                    {
                        SelectionStart = Math.Min(SelectionStart, newPosition);
                    }
                }
            }
            else
            {
                ClearSelection();
            }

            CursorPosition = newPosition;
        }

        private void MoveToWordRight(bool selecting = false)
        {
            if (string.IsNullOrEmpty(Content) || CursorPosition >= Content.Length) return;

            int newPosition;

            if (CursorPosition < Content.Length && !char.IsWhiteSpace(Content[CursorPosition]))
            {
                newPosition = FindWordEnd(CursorPosition);
            }
            else
            {
                newPosition = FindNextWordStart(CursorPosition);
                newPosition = FindWordEnd(newPosition);
            }

            if (selecting)
            {
                if (!HasSelection)
                {
                    SelectionStart = CursorPosition;
                    SelectionEnd = CursorPosition;

                    SelectionStart = Math.Min(SelectionStart, newPosition);
                    SelectionEnd = Math.Max(SelectionEnd, newPosition);
                }
                else
                {
                    if (CursorPosition == SelectionStart)
                    {
                        SelectionStart = newPosition;
                        if (SelectionStart > SelectionEnd)
                        {
                            (SelectionEnd, SelectionStart) = (SelectionStart, SelectionEnd);
                        }
                    }
                    else
                    {
                        SelectionEnd = Math.Max(SelectionEnd, newPosition);
                    }
                }
            }
            else
            {
                ClearSelection();
            }

            CursorPosition = newPosition;
        }

        private int FindPreviousWordStart(int position)
        {
            if (position <= 0) return 0;

            position = Math.Min(position, Content.Length);

            while (position > 0 && char.IsWhiteSpace(Content[position - 1]))
            {
                position--;
            }

            while (position > 0 && !char.IsWhiteSpace(Content[position - 1]))
            {
                position--;
            }

            return position;
        }

        private int FindNextWordStart(int position)
        {
            if (position >= Content.Length) return Content.Length;

            position = Math.Min(position, Content.Length);

            while (position < Content.Length && char.IsWhiteSpace(Content[position]))
            {
                position++;
            }

            return position;
        }

        private int FindWordStart(int position)
        {
            position = Math.Min(position, Content.Length);

            if (position > 0 && position <= Content.Length && !char.IsWhiteSpace(Content[position - 1]))
            {
                while (position > 0 && !char.IsWhiteSpace(Content[position - 1]))
                {
                    position--;
                }
            }

            return position;
        }

        private int FindWordEnd(int position)
        {
            position = Math.Min(position, Content.Length);

            while (position < Content.Length && !char.IsWhiteSpace(Content[position]))
            {
                position++;
            }

            return position;
        }

        private void MoveToLineUp(bool selecting = false)
        {
            if (string.IsNullOrEmpty(Content)) return;


            float currentCursorX = GetCursorXPosition();
            int currentLine = GetCurrentLineIndex();

            if (currentLine > 0)
            {
                int newLine = currentLine - 1;
                string[] lines = Content.Split('\n');

                if (newLine < lines.Length)
                {
                    string targetLine = lines[newLine];
                    int newCharPos = GetClosestCharacterPosition(targetLine, currentCursorX, Settings.TextSize);

                    int newPosition = GetLineStartIndex(lines, newLine) + newCharPos;

                    if (selecting)
                    {
                        if (!HasSelection)
                        {
                            SelectionStart = CursorPosition;
                            SelectionEnd = CursorPosition;

                            SelectionStart = Math.Min(SelectionStart, newPosition);
                            SelectionEnd = Math.Max(SelectionEnd, newPosition);
                        }
                        else
                        {
                            if (CursorPosition == SelectionEnd)
                            {

                                SelectionEnd = newPosition;

                                if (SelectionEnd < SelectionStart)
                                {
                                    (SelectionEnd, SelectionStart) = (SelectionStart, SelectionEnd);
                                }
                            }
                            else if (CursorPosition == SelectionStart)
                            {

                                SelectionStart = newPosition;
                                if (SelectionStart > SelectionEnd)
                                {
                                    (SelectionEnd, SelectionStart) = (SelectionStart, SelectionEnd);
                                }
                            }
                            else
                            {
                                if (newPosition < SelectionStart)
                                {
                                    SelectionStart = newPosition;
                                }
                                else if (newPosition > SelectionEnd)
                                {
                                    SelectionEnd = newPosition;
                                }
                            }
                        }
                    }
                    else
                    {
                        ClearSelection();
                    }

                    CursorPosition = newPosition;
                }
            }
            else if (currentLine == 0 && CursorPosition > 0)
            {
                int newPosition = 0;

                if (selecting)
                {
                    if (!HasSelection)
                    {
                        SelectionStart = CursorPosition;
                        SelectionEnd = CursorPosition;
                    }
                    SelectionStart = Math.Min(SelectionStart, newPosition);
                    SelectionEnd = Math.Max(SelectionEnd, newPosition);
                }
                else
                {
                    ClearSelection();
                }

                CursorPosition = newPosition;
            }
        }

        private void MoveToLineDown(bool selecting = false)
        {
            if (string.IsNullOrEmpty(Content)) return;

            float currentCursorX = GetCursorXPosition();
            int currentLine = GetCurrentLineIndex();

            string[] lines = Content.Split('\n');

            if (currentLine < lines.Length - 1)
            {
                int newLine = currentLine + 1;

                if (newLine < lines.Length)
                {
                    string targetLine = lines[newLine];
                    int newCharPos = GetClosestCharacterPosition(targetLine, currentCursorX, Settings.TextSize);

                    int newPosition = GetLineStartIndex(lines, newLine) + newCharPos;

                    if (selecting)
                    {
                        if (!HasSelection)
                        {
                            SelectionStart = CursorPosition;
                            SelectionEnd = CursorPosition;


                            SelectionStart = Math.Min(SelectionStart, newPosition);
                            SelectionEnd = Math.Max(SelectionEnd, newPosition);
                        }
                        else
                        {
                            if (CursorPosition == SelectionStart)
                            {
                                SelectionStart = newPosition;
                                if (SelectionStart > SelectionEnd)
                                {
                                    (SelectionEnd, SelectionStart) = (SelectionStart, SelectionEnd);
                                }
                            }
                            else if (CursorPosition == SelectionEnd)
                            {
                                SelectionEnd = newPosition;
                                if (SelectionEnd < SelectionStart)
                                {
                                    (SelectionEnd, SelectionStart) = (SelectionStart, SelectionEnd);
                                }
                            }
                            else
                            {
                                if (newPosition < SelectionStart)
                                {
                                    SelectionStart = newPosition;
                                }
                                else if (newPosition > SelectionEnd)
                                {
                                    SelectionEnd = newPosition;
                                }
                            }
                        }
                    }
                    else
                    {
                        ClearSelection();
                    }

                    CursorPosition = newPosition;
                }
            }
            else if (currentLine == lines.Length - 1 && CursorPosition < Content.Length)
            {
                int newPosition = Content.Length;

                if (selecting)
                {
                    if (!HasSelection)
                    {
                        SelectionStart = CursorPosition;
                        SelectionEnd = CursorPosition;
                    }
                    SelectionStart = Math.Min(SelectionStart, newPosition);
                    SelectionEnd = Math.Max(SelectionEnd, newPosition);
                }
                else
                {
                    ClearSelection();
                }

                CursorPosition = newPosition;
            }
        }

        private float GetCursorXPosition()
        {
            string[] lines = Content.Split('\n');
            int currentLine = GetCurrentLineIndex();
            int charsBeforeCurrentLine = GetLineStartIndex(lines, currentLine);

            string currentLineText = lines[currentLine];
            int cursorInLine = CursorPosition - charsBeforeCurrentLine;
            string textUpToCursor = currentLineText[..Math.Min(cursorInLine, currentLineText.Length)];

            return TextHelper.GetFormattedTextWidth(textUpToCursor, Settings.TextSize);
        }

        private int GetCurrentLineIndex()
        {
            string[] lines = Content.Split('\n');
            int currentIndex = 0;
            int charCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                charCount += lines[i].Length;
                if (CursorPosition <= charCount + i) // +i for newline characters
                {
                    currentIndex = i;
                    break;
                }
            }

            return currentIndex;
        }

        private int GetClosestCharacterPosition(string line, float targetX, float scale)
        {
            float currentX = 0;

            for (int i = 0; i < line.Length; i++)
            {
                float charWidth = TextHelper.GetTextWidth(line[i].ToString(), FontType.REGULAR, scale);
                float charMid = currentX + charWidth / 2;

                if (targetX < charMid)
                    return i;

                currentX += charWidth;
            }

            return line.Length;
        }

    }
}