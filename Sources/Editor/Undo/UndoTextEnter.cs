/*
    Copyright (c) 2005-2012 Fedir Nepyivoda <fednep@gmail.com>
  
    This file is part of UV Outliner project.
    http://uvoutliner.com

    UV Outliner is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    UV Outliner is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with UV Outliner.  If not, see <http://www.gnu.org/licenses/>
 
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Windows;

namespace UVOutliner
{    
    public class UndoTextEnter : UVEditUndoAction
    {
        private MemoryStream __DataStream;

        private int __OffsetStart;
        private int __OffsetEnd;
        private int __OffsetCursorPosition;
        private string __TextEntered;

        public UndoTextEnter(RichTextBox edit, TextRange range, string text)
        {
            UpdateOffsets(edit, range);
            __OffsetCursorPosition = edit.Document.ContentStart.GetOffsetToPosition(edit.CaretPosition);

            __TextEntered = text;
        }

        private void UpdateOffsets(RichTextBox edit, TextRange range)
        {
            __OffsetStart = edit.Document.ContentStart.GetOffsetToPosition(range.Start);
            __OffsetEnd = edit.Document.ContentStart.GetOffsetToPosition(range.End);
        }

        public override void Undo(RichTextBox edit)
        {
            FlowDocument document = edit.Document;
            TextRange whole = new TextRange(document.ContentStart, document.ContentEnd);            
            TextRange range = new TextRange(
                                    UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart), 
                                    UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetEnd));

            __DataStream = new MemoryStream();
            range.Save(__DataStream, DataFormats.Xaml);

            range.ClearAllProperties();
            range.Text = "";
            UpdateOffsets(edit, range);

            TextPointer caretPosition = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);
            edit.CaretPosition = caretPosition;            
        }

        public override void Redo(RichTextBox edit)
        {
            FlowDocument document = edit.Document;
            __DataStream.Seek(0, SeekOrigin.Begin);
            TextRange whole = new TextRange(document.ContentStart, document.ContentEnd);

            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);
            whole.Select(start, start);
            whole.Load(__DataStream, DataFormats.Xaml);
            UpdateOffsets(edit, whole);

            TextPointer caretPosition = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetCursorPosition);
            edit.CaretPosition = caretPosition;            
        }

        public bool CanMerge(RichTextBox edit, TextPointer insertPosition)
        {
            if (edit.Document.ContentStart.GetOffsetToPosition(insertPosition) == __OffsetEnd &&
                insertPosition.IsAtInsertionPosition)
                return true;

            return false;
        }

        public override bool CanBeMergedWith(UndoTextEnter undoAction)
        {
            if (__OffsetEnd == undoAction.__OffsetStart)
                return true;

            return false;
        }

        public override void Merge(UndoTextEnter undoAction)
        {
            __OffsetEnd = undoAction.__OffsetEnd;
            __TextEntered += undoAction.__TextEntered;
            __OffsetCursorPosition = undoAction.__OffsetCursorPosition;
        }
    }
}
