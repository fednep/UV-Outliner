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
using System.IO;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;

namespace UVOutliner
{
    public class UndoBlockRemove : UVEditUndoAction
    {
        private MemoryStream __DataStream;

        private bool __IsBlockSelected;

        private int __OffsetStart;
        private int __OffsetEnd;
        private int __OffsetCursorPosition;

        public UndoBlockRemove(RichTextBox edit, TextRange range)
        {
            __DataStream = new MemoryStream();
            UpdateOffsets(edit, range);
            __OffsetCursorPosition = edit.Document.ContentStart.GetOffsetToPosition(edit.CaretPosition);

            if (!edit.Selection.IsEmpty)
                __IsBlockSelected = true;
            
            TextRange textRange = new TextRange(range.Start, range.End);
            textRange.Save(__DataStream, DataFormats.Xaml);
        }

        public void UpdateOffsets(RichTextBox edit, TextRange range)
        {
            __OffsetStart = edit.Document.ContentStart.GetOffsetToPosition(range.Start);
            __OffsetEnd = edit.Document.ContentStart.GetOffsetToPosition(range.End);
        }

        public override void Undo(RichTextBox edit)
        {
            FlowDocument document = edit.Document;
            __DataStream.Seek(0, SeekOrigin.Begin);
            TextRange whole = new TextRange(document.ContentStart, document.ContentEnd);

            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);            
            whole.Select(start, start);
            whole.Load(__DataStream, DataFormats.Xaml);            
            TextPointer caretPosition = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetCursorPosition);
            edit.CaretPosition = caretPosition;

            UpdateOffsets(edit, whole);

            if (__IsBlockSelected)
            {
                // Пересчитать ещё раз start
                start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);            
                TextPointer end = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetEnd);                                
                edit.Selection.Select(start, end);
            }
        }

        public override void Redo(RichTextBox edit)
        {
            FlowDocument document = edit.Document;
            TextRange whole = new TextRange(document.ContentStart, document.ContentEnd);
            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);
            TextPointer end = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetEnd);
            TextRange range = new TextRange(start, end);
            range.ClearAllProperties();
            range.Text = "";

            edit.CaretPosition = end;
            UpdateOffsets(edit, range);
        }
    }
}
