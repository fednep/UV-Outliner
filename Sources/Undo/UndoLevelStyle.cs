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
using UVOutliner.Columns;

namespace UVOutliner.Undo
{
    public class UndoLevelStyle: DocumentUndoAction
    {
        private int __NoteId;
        //private int __ActiveColumnId;

        MemoryStream[] __Before;
        MemoryStream[] __After;

        bool __IsEmpty;

        public UndoLevelStyle(OutlinerNote note)
        {
            __NoteId = note.Id;


            __Before = new MemoryStream[note.Columns.Count];
            __After = new MemoryStream[note.Columns.Count];

            __IsEmpty = true;

            for (int i = 0; i < note.Columns.Count; i++)
            {
                FlowDocument document = note.Columns[i].ColumnData as FlowDocument;
                if (document == null)
                    continue;

                __Before[i] = new MemoryStream();
                __After[i] = new MemoryStream();

                TextRange range = new TextRange(document.ContentStart, document.ContentEnd);
                range.Save(__Before[i], DataFormats.Xaml);

                if (!range.IsEmpty)
                    __IsEmpty = false;

            }
        }

        public void StyleApplied(OutlinerNote note)
        {
            for (int i = 0; i < note.Columns.Count; i++)
            {
                FlowDocument document = note.Columns[i].ColumnData as FlowDocument;
                if (document == null)
                    continue;

                TextRange range = new TextRange(document.ContentStart, document.ContentEnd);
                range.Save(__After[i], DataFormats.Xaml);
            }
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            if (note == null)
                return;

            for (int i = 0; i < note.Columns.Count; i++)
            {
                FlowDocument flowDocument = note.Columns[i].ColumnData as FlowDocument;
                if (flowDocument == null)
                    continue;

                __Before[i].Seek(0, SeekOrigin.Begin);

                TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                range.Load(__Before[i], DataFormats.Xaml);
            }          
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            if (note == null)
                return;

            for (int i = 0; i < note.Columns.Count; i++)
            {
                FlowDocument flowDocument = note.Columns[i].ColumnData as FlowDocument;
                if (flowDocument == null)
                    continue;

                __After[i].Seek(0, SeekOrigin.Begin);

                TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                range.Load(__After[i], DataFormats.Xaml);
            }
        }

        bool __WasCalculated = false;

        public override bool IsEmptyAction()
        {
            if (__IsEmpty)
                return true;

            if (!__WasCalculated)
            {
                __IsEmpty = TestIfMemStreamsEqual();
                __WasCalculated = true;
            }

            return __IsEmpty;
        }

        private bool TestIfMemStreamsEqual()
        {

            for (int i = 0; i < __Before.Length; i++)
            {

                if (__Before[i] == null || __After[i] == null)
                    continue;

                __Before[i].Seek(0, SeekOrigin.Begin);
                __After[i].Seek(0, SeekOrigin.Begin);
                StreamReader reader1 = new StreamReader(__Before[i]);
                StreamReader reader2 = new StreamReader(__After[i]);

                string str1 = reader1.ReadToEnd();
                string str2 = reader2.ReadToEnd();
                if (str1 != str2)
                    return false;                
            }

            return true;
        }
    }
}
