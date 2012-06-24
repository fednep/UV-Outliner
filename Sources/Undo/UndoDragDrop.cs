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
using System.Diagnostics;

namespace UVOutliner.Undo
{
    public class UndoDragDrop:DocumentUndoAction
    {

        private int __NoteId;
        private int __ParentNoteIdBefore;
        private int __IndexBefore;

        private int __ParentNoteIdAfter;
        private int __IndexAfter;



        public UndoDragDrop(OutlinerNote note)
        {
            __NoteId = note.Id;
            __ParentNoteIdBefore = note.Parent.Id;
            __IndexBefore = note.Parent.SubNotes.IndexOf(note);
        }        

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            __ParentNoteIdAfter = note.Parent.Id;
            __IndexAfter = note.Parent.SubNotes.IndexOf(note);

            OutlinerNote newParent = document.FindOutlinerNoteById(__ParentNoteIdBefore);
            Debug.Assert(newParent != null);
            note.Parent.SubNotes.Remove(note);

            OutlinerNote newNote = new OutlinerNote(newParent);
            newNote.Clone(note);
            DocumentHelpers.CopyNodesRecursively(newNote, note);
            newParent.SubNotes.Insert(__IndexBefore, newNote);
            treeListView.MakeActive(newNote, -1, false);
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);

            OutlinerNote newParent = document.FindOutlinerNoteById(__ParentNoteIdAfter);
            Debug.Assert(newParent != null);
            note.Parent.SubNotes.Remove(note);

            OutlinerNote newNote = new OutlinerNote(newParent);
            newNote.Clone(note);
            DocumentHelpers.CopyNodesRecursively(newNote, note);
            newParent.SubNotes.Insert(__IndexAfter, newNote);
            treeListView.MakeActive(newNote, -1, false);
        }
        
    }
}
