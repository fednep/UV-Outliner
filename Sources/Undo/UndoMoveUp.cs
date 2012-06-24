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
    public class UndoMoveUp: DocumentUndoAction
    {
        private int __NodeId;
        private int __ActiveColumn;
        private bool __IsInlineNoteFocused;


        public UndoMoveUp(OutlinerNote note, int activeColumn, bool isInlineNoteFocused)
        {
            __NodeId = note.Id;
            __ActiveColumn = activeColumn;
            __IsInlineNoteFocused = isInlineNoteFocused;
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NodeId);
            DocumentHelpers.MoveNodeDown(note, treeListView, __ActiveColumn, __IsInlineNoteFocused);
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NodeId);
            DocumentHelpers.MoveNodeUp(note, treeListView, __ActiveColumn, __IsInlineNoteFocused);
        }
    }
}
