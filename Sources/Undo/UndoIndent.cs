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
    public enum IndentDirection {IncreaseIndent, DecreaseIndent}

    public class UndoIndent: DocumentUndoAction
    {
        private IndentDirection __Direction;
        private int __NoteId;
        private int __LimitNoteId;
        private bool __IsInlineEditFocused;

        public UndoIndent(OutlinerNote note, bool isInlineNoteFocused, IndentDirection direction)
        {
            __Direction = direction;
            __NoteId = note.Id;
            __IsInlineEditFocused = isInlineNoteFocused;

            if (direction == IndentDirection.DecreaseIndent)
            {
                if (note.SubNotes.Count > 0)
                    __LimitNoteId = note.SubNotes[note.SubNotes.Count - 1].Id;
                else
                    __LimitNoteId = note.Id;
            }
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            if (note == null)
                return;

            if (__Direction == IndentDirection.IncreaseIndent)
                DocumentHelpers.DecreaseIndent(note, treeListView, false);
            else
            {
                OutlinerNote limitNote = document.FindOutlinerNoteById(__LimitNoteId);                   
                DocumentHelpers.IncreaseIndentWithLimit(note, limitNote, __IsInlineEditFocused, treeListView, false);
            }
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NoteId);
            if (note == null)
                return;

            if (__Direction == IndentDirection.IncreaseIndent)
                DocumentHelpers.IncreaseIndent(note, treeListView, false);
            else
                DocumentHelpers.DecreaseIndent(note, treeListView, false);
        }
    }
}
