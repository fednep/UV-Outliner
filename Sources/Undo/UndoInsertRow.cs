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

namespace UVOutliner.Undo
{
    public class UndoInsertRow: DocumentUndoAction
    {
        private int __NoteId;
        private int __ParentNoteId;
        private int __Index;
        private int __ColumnIndexAfterInsert;
        private int __ColumnIndexBeforeUndo;

        private OutlinerNote __SavedNote;

        public UndoInsertRow(OutlinerNote note, int columnIndex)
        {
            __NoteId = note.Id;
            __ParentNoteId = note.Parent.Id;
            __Index = note.Parent.SubNotes.IndexOf(note);
            __ColumnIndexAfterInsert = columnIndex;
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            __SavedNote = note;
            __ColumnIndexBeforeUndo = DocumentHelpers.GetFocusedColumnIdx(treeListView, note);

            DocumentHelpers.DeleteRow(note, treeListView, __ColumnIndexAfterInsert);
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote newParent = document.FindOutlinerNoteById(__ParentNoteId);

            OutlinerNote newNote = new OutlinerNote(newParent);
            newNote.Clone(__SavedNote);
            DocumentHelpers.CopyNodesRecursively(newNote, __SavedNote);

            newParent.SubNotes.Insert(__Index, newNote);

            treeListView.MakeActive(newNote, __ColumnIndexBeforeUndo, false);
        }
    }
}
