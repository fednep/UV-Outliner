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
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.IO;
using System.Windows;
using UVOutliner.Editor;

namespace UVOutliner.Undo
{
    public class UndoFlowDocumentFormatting: DocumentUndoAction
    {
        private int __NoteId;
        private int __ColumnId;
        private bool __IsInlineNote;

        MemoryStream __Before;
        MemoryStream __After;

        bool __WasSelected;

        FontProperties __FontPropertiesBefore;
        FontProperties __FontPropertiesAfter;

        public UndoFlowDocumentFormatting(OutlinerNote note, int columnId, bool isInlineNote, bool wasSelected)
        {
            __NoteId = note.Id;
            __ColumnId = columnId;
            __IsInlineNote = isInlineNote;

            __Before = new MemoryStream();            

            FlowDocument flowDocument = (FlowDocument)note.Columns[columnId].ColumnData;

            __FontPropertiesBefore = new FontProperties(flowDocument);

            TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
            range.Save(__Before, DataFormats.Xaml);

            __WasSelected = wasSelected;
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            if (note == null)
                return;

            __Before.Seek(0, SeekOrigin.Begin);

            FlowDocument flowDocument;
            if (__IsInlineNote)
                flowDocument = note.InlineNoteDocument;
            else
                flowDocument = (FlowDocument)note.Columns[__ColumnId].ColumnData;

            if (flowDocument == null)
                return;

            TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
            if (__After == null)
            {
                __After = new MemoryStream();
                range.Save(__After, DataFormats.Xaml);
                __FontPropertiesAfter = new FontProperties(flowDocument);
            }
            range.Load(__Before, DataFormats.Xaml);
            __FontPropertiesBefore.ApplyToFlowDocument(flowDocument);

            if (__WasSelected)
                treeListView.MakeActive(note, -1, false);
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            if (note == null)
                return;

            __After.Seek(0, SeekOrigin.Begin);

            FlowDocument flowDocument = (FlowDocument)note.Columns[__ColumnId].ColumnData;
            TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
            range.Load(__After, DataFormats.Xaml);
            __FontPropertiesAfter.ApplyToFlowDocument(flowDocument);

            if (__WasSelected)
                treeListView.MakeActive(note, -1, false);
        }        

        public override bool IsEmptyAction()
        {            
            return false;
        }        
    }
}
