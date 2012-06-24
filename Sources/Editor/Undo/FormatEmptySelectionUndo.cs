using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;

namespace UVOutliner
{
    class FormatEmptySelectionUndo : UVEditUndoAction
    {
        private MemoryStream __DataStream;
        private MemoryStream __UndoStream;

        object __UndoFontWeight;
        object __UndoFontFamily;
        object __UndoFontSize;
        object __UndoFontStyle;
        object __UndoTextDecoration;

        object __RedoFontWeight;
        object __RedoFontFamily;
        object __RedoFontSize;
        object __RedoFontStyle;
        object __RedoTextDecoration;

        private int __OffsetCursorPosition;

        public FormatEmptySelectionUndo(RichTextBox edit)
        {
            __DataStream = new MemoryStream();

            __OffsetCursorPosition = edit.Document.ContentStart.GetOffsetToPosition(edit.Selection.Start);
            __UndoFontWeight = edit.Selection.GetPropertyValue(RichTextBox.FontWeightProperty);
            __UndoFontFamily = edit.Selection.GetPropertyValue(RichTextBox.FontFamilyProperty);
            __UndoFontSize = edit.Selection.GetPropertyValue(RichTextBox.FontSizeProperty);
            __UndoFontStyle = edit.Selection.GetPropertyValue(RichTextBox.FontStyleProperty);
            __UndoTextDecoration = edit.Selection.GetPropertyValue(TextBlock.TextDecorationsProperty);

            __DataStream.Seek(0, SeekOrigin.Begin);
            StreamReader sr = new StreamReader(__DataStream);
            string res = sr.ReadToEnd();
        }

        public override void Undo(RichTextBox edit)
        {            
            __DataStream.Seek(0, SeekOrigin.Begin);
            
            edit.CaretPosition = UndoHelpers.SafePositionAtOffset(edit.Document, edit.Document.ContentStart, __OffsetCursorPosition);

            edit.Selection.Select(edit.CaretPosition, edit.CaretPosition);
            __RedoFontWeight = edit.Selection.GetPropertyValue(RichTextBox.FontWeightProperty);
            __RedoFontFamily = edit.Selection.GetPropertyValue(RichTextBox.FontFamilyProperty);
            __RedoFontSize = edit.Selection.GetPropertyValue(RichTextBox.FontSizeProperty);
            __RedoFontStyle = edit.Selection.GetPropertyValue(RichTextBox.FontStyleProperty);
            __RedoTextDecoration = edit.Selection.GetPropertyValue(TextBlock.TextDecorationsProperty);

            edit.Selection.ApplyPropertyValue(RichTextBox.FontWeightProperty, __UndoFontWeight);
            edit.Selection.ApplyPropertyValue(RichTextBox.FontFamilyProperty, __UndoFontFamily);
            edit.Selection.ApplyPropertyValue(RichTextBox.FontSizeProperty, __UndoFontSize);
            edit.Selection.ApplyPropertyValue(RichTextBox.FontStyleProperty, __UndoFontStyle);
            edit.Selection.ApplyPropertyValue(TextBlock.TextDecorationsProperty, __UndoTextDecoration);            
        }

        public override void Redo(RichTextBox edit)
        {            
            edit.CaretPosition = UndoHelpers.SafePositionAtOffset(edit.Document, edit.Document.ContentStart, __OffsetCursorPosition);
            edit.Selection.Select(edit.CaretPosition, edit.CaretPosition);
            edit.Selection.ApplyPropertyValue(RichTextBox.FontWeightProperty, __RedoFontWeight);
            edit.Selection.ApplyPropertyValue(RichTextBox.FontFamilyProperty, __RedoFontFamily);
            edit.Selection.ApplyPropertyValue(RichTextBox.FontSizeProperty, __RedoFontSize);
            edit.Selection.ApplyPropertyValue(RichTextBox.FontStyleProperty, __RedoFontStyle);
            edit.Selection.ApplyPropertyValue(TextBlock.TextDecorationsProperty, __RedoTextDecoration);
        }

        
        internal void UpdateSelectionOffsets(RichTextBox edit)
        {         
            __OffsetCursorPosition = edit.Document.ContentStart.GetOffsetToPosition(edit.CaretPosition);
        }
    }
}
