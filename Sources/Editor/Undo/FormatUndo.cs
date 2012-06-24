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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace UVOutliner
{
    class FormatUndo : UVEditUndoAction
    {
        private MemoryStream __DataStream;
        private MemoryStream __UndoStream;

        private int __OffsetCursorPositionBefore = -1;
        private int __OffsetCursorPositionAfter = -1;

        int selectionStartOffset;
        int selectionEndOffset;

        public FormatUndo(FlowDocument document, TextRange range, RichTextBox edit)
        {
            __DataStream = new MemoryStream();

            selectionStartOffset = document.ContentStart.GetOffsetToPosition(range.Start);
            selectionEndOffset = document.ContentEnd.GetOffsetToPosition(range.End);

            if (edit.Selection.IsEmpty)
                __OffsetCursorPositionBefore = document.ContentStart.GetOffsetToPosition(edit.CaretPosition);
            
            range.Save(__DataStream, DataFormats.Xaml, false);
        }

        public override void Undo(RichTextBox edit)
        {            
            PerformUndo(edit.Document, edit);            
        }

        public void Undo(FlowDocument document)
        {
            PerformUndo(document, null);
        }

        private void PerformUndo(FlowDocument document, RichTextBox edit)
        {
            __DataStream.Seek(0, SeekOrigin.Begin);

            if (__OffsetCursorPositionBefore != -1)
                __OffsetCursorPositionAfter = document.ContentStart.GetOffsetToPosition(edit.CaretPosition);

            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, selectionStartOffset);
            TextPointer end = UndoHelpers.SafePositionAtOffset(document, document.ContentEnd, selectionEndOffset);
            
            TextRange whole = new TextRange(start, end);

            __UndoStream = new MemoryStream();
            whole.Save(__UndoStream, DataFormats.Xaml);
            whole.Load(__DataStream, DataFormats.Xaml);

            UpdateSelectionOffsets(edit, whole);

            if (__OffsetCursorPositionBefore != -1)
                edit.CaretPosition = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetCursorPositionBefore);
            else
            {
                if (edit != null)
                    edit.Selection.Select(start, end);
            }
        }

        public override void Redo(RichTextBox edit)
        {
            FlowDocument document = edit.Document;
            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, selectionStartOffset);
            TextPointer end = UndoHelpers.SafePositionAtOffset(document, document.ContentEnd, selectionEndOffset);

            TextRange whole = new TextRange(start, end);            
            __UndoStream.Seek(0, SeekOrigin.Begin);
            whole.Load(__UndoStream, DataFormats.Xaml);
            UpdateSelectionOffsets(edit, whole);

            if (__OffsetCursorPositionAfter != -1)
                edit.CaretPosition = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetCursorPositionAfter);
            else
                edit.Selection.Select(start, end);
        }

        internal void UpdateSelectionOffsets(RichTextBox edit, TextRange range)
        {
            selectionStartOffset = edit.Document.ContentStart.GetOffsetToPosition(range.Start);
            selectionEndOffset = edit.Document.ContentEnd.GetOffsetToPosition(range.End);            
        }
    }
}
