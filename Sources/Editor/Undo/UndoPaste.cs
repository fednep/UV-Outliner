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
using System.Windows.Documents;
using System.Windows.Controls;
using System.IO;
using System.Windows;

namespace UVOutliner
{
    public class UndoPaste: UVEditUndoAction
    {
        private MemoryStream __DataStream;

        private int __OffsetStart;
        private int __OffsetEnd;
        private int __OffsetCursorPositionBefore;
        private int __OffsetCursorPositionAfter;

        public UndoPaste(RichTextBox edit, int offsetStart, int offsetEnd)
        {
            __OffsetStart = offsetStart;
            __OffsetEnd = offsetEnd;
            __OffsetCursorPositionBefore = edit.Document.ContentStart.GetOffsetToPosition(edit.CaretPosition);
        }

        public override void Undo(RichTextBox edit)
        {            
            FlowDocument document = edit.Document;

            __OffsetCursorPositionAfter = document.ContentStart.GetOffsetToPosition(edit.CaretPosition);

            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);
            TextPointer end = UndoHelpers.SafePositionAtOffset(document, document.ContentEnd, __OffsetEnd);
            TextRange range = new TextRange(start, end);
            __DataStream = new MemoryStream();
            range.Save(__DataStream, DataFormats.Xaml);
            range.ClearAllProperties();
            range.Text = "";

            edit.CaretPosition = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetCursorPositionBefore);
        }

        public override void Redo(RichTextBox edit)
        {
            FlowDocument document = edit.Document;
            __DataStream.Seek(0, SeekOrigin.Begin);
            TextRange whole = new TextRange(document.ContentStart, document.ContentEnd);

            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);
            TextPointer end = UndoHelpers.SafePositionAtOffset(document, document.ContentEnd, __OffsetEnd);

            whole.Select(start, end);
            whole.Load(__DataStream, DataFormats.Xaml);
            
            edit.CaretPosition = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetCursorPositionAfter);
        }
    }
}
