using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Diagnostics;

namespace UVOutliner
{
    /*
    public class BackspaceUndoAction : UVEditUndoAction
    {
        private MemoryStream __DataStream;        

        int __OffsetStart;
        int __OffsetEnd;

        public BackspaceUndoAction(RichTextBox edit, TextRange range)
        {
            __DataStream = new MemoryStream();
            
            TextRange textRange = new TextRange(range.Start, range.End);
            textRange.Save(__DataStream, DataFormats.Xaml);
        }

        public override void Undo(RichTextBox edit)
        {
            FlowDocument document = edit.Document;
            __DataStream.Seek(0, SeekOrigin.Begin);
            TextRange whole = new TextRange(document.ContentStart, document.ContentEnd);

            TextPointer start = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetStart);            
            whole.Select(start, start);
            whole.Load(__DataStream, DataFormats.Xaml);
            TextPointer end = UndoHelpers.SafePositionAtOffset(document, document.ContentStart, __OffsetEnd);
            edit.CaretPosition = end;
        }

        public void UpdateSelectionOffsets(TextRange range)
        {
            __OffsetStart = edit.Document.ContentStart.GetOffsetToPosition(range.Start);
            __OffsetEnd = edit.Document.ContentStart.GetOffsetToPosition(range.End);
        }
    }*/
}
