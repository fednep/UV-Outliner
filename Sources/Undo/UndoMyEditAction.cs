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
using System.Diagnostics;
using System.Windows.Input;
using DragDropListBox;

namespace UVOutliner.Undo
{
    public class UndoMyEditAction: DocumentUndoAction
    {        
        private int __ColumnId;
        private bool __IsInlineNote;
        private Guid __DocumentGuid;
        private UVEditUndoAction __UndoAction;

        public UndoMyEditAction(int columnId, bool isInlineNote, FlowDocument document, UVEditUndoAction undoAction)
        {           
            __ColumnId = columnId;
            __IsInlineNote = isInlineNote;
            __DocumentGuid = (Guid)document.Tag;            
            __UndoAction = undoAction;
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            MyEdit edit = GetEdit(document, treeListView);
            Debug.Assert(edit != null, "Bad news: UndoMyAction's Undo has edit == null");

            __UndoAction.Undo(edit);
            edit.Links_Update();
            Keyboard.Focus(edit);
        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            MyEdit edit = GetEdit(document, treeListView);
            Debug.Assert(edit != null, "Bad news: UndoMyAction's Undo has edit == null");

            __UndoAction.Redo(edit);
            edit.Links_Update();
            Keyboard.Focus(edit);
        }

        private MyEdit GetEdit(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteByDocument(__DocumentGuid);
            Debug.Assert(note != null, "GetEdit's note == null");

            TreeListViewItem item = ViewHelpers.GetContainerForItem(treeListView, note);
            Debug.Assert(item != null, "Bad news: UndoMyAction's Undo has item == null");

            MainWindow mw = DragDropHelper.GetMainWindow(treeListView);

            MyEdit edit = item.GetEditor(mw.GetViewColumnId(__ColumnId), __ColumnId, __IsInlineNote);
            return edit;
        }

        public override bool CanMerge(DocumentUndoAction action)
        {
            if (!(action is UndoMyEditAction))
                return false;

            var action_casted = (UndoMyEditAction)action;
            if (!(action_casted.__UndoAction is UndoTextEnter))
                return false;

            return __UndoAction.CanBeMergedWith((UndoTextEnter)(action_casted.__UndoAction));
        }

        public override void Merge(DocumentUndoAction action)
        {
            Debug.Assert(action is UndoMyEditAction);
            Debug.Assert(((UndoMyEditAction)action).__UndoAction is UndoTextEnter);
            __UndoAction.Merge((UndoTextEnter)(((UndoMyEditAction)action).__UndoAction));
        }

        public override bool UndoNext
        {
            get
            {                
                return __UndoAction.UndoNext;
            }
        }
    }
}
