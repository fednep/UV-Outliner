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
    public class UndoDeleteRow: DocumentUndoAction
    {
        private OutlinerNote __SavedNote;

        private int __NoteId;
        private int __ParentNodeId;
        private int __NodeIndex;

        public UndoDeleteRow(OutlinerNote note)
        {
            __NoteId = note.Id;
            SaveNote(note);
        }

        private void SaveNote(OutlinerNote note)
        {
            __NoteId = note.Id;
            __SavedNote = note;
            __ParentNodeId = note.Parent.Id;
            __NodeIndex = note.Parent.SubNotes.IndexOf(note);
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote newParent = document.FindOutlinerNoteById(__ParentNodeId);

            OutlinerNote newNote = new OutlinerNote(newParent);
            newNote.Clone(__SavedNote);

            DocumentHelpers.CopyNodesRecursively(newNote, __SavedNote);
            newParent.SubNotes.Insert(__NodeIndex, newNote);
            __SavedNote = null;

            treeListView.MakeActive(newNote, -1, false);
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            SaveNote(note);
            DocumentHelpers.DeleteRow(note, treeListView);
        }
    }
}
