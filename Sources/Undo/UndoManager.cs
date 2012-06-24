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
using UVOutliner.Undo;
using System.Runtime.Serialization;
using System.Xml;

namespace UVOutliner
{
    public class UndoManager
    {

        public event EventHandler UndoActionsCountChanged;

        private Stack<DocumentUndoAction> __UndoActions = new Stack<DocumentUndoAction>();
        private Stack<DocumentUndoAction> __RedoActions = new Stack<DocumentUndoAction>();


        public UndoManager()
        {

        }

        public void PushUndoAction(DocumentUndoAction action)
        {
            // if the action is empty (does not modifies the docuement, skip adding it to the stack
            if (action.IsEmptyAction())
                return;

            SaveUndoActionStack(action);

            if (__UndoActions.Count == 0 || !CanMerge)
            {
                if (__UndoGroup != null)
                    __UndoGroup.Add(action);
                else
                    __UndoActions.Push(action);
            }
            else
            {
                DocumentUndoAction lastAction = __UndoActions.Peek();
                if (lastAction.CanMerge(action))
                    lastAction.Merge(action);
                else
                {
                    if (__UndoGroup != null)
                        __UndoGroup.Add(action);
                    else
                        __UndoActions.Push(action);
                }
            }

            CanMerge = true;
            __RedoActions.Clear();
            DoUndoCountChanged();
        }

        public bool CanUndo
        {
            get { return __UndoActions.Count > 0; }
        }

        public bool CanRedo
        {
            get { return __RedoActions.Count > 0; }
        }

        public void Undo(OutlinerDocument document, TreeListView treeListView)
        {            
            while (__UndoActions.Count > 0)
            {
                DocumentUndoAction action = __UndoActions.Pop();
                action.Undo(document, treeListView);

                __RedoActions.Push(action);
                DoUndoCountChanged();

                if (!action.UndoNext)
                    break;
            }

            GC.Collect();
        }

        public void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            while (__RedoActions.Count > 0)
            {
                DocumentUndoAction action = __RedoActions.Pop();
                action.Redo(document, treeListView);

                __UndoActions.Push(action);
                DoUndoCountChanged();

                if (__RedoActions.Count > 0)
                {
                    var nextAction = __RedoActions.Peek();
                    if (!nextAction.UndoNext)
                        break;
                }
            }
            GC.Collect();
        }

        public bool CanMerge
        {
            get;
            set;
        }

        UndoGroup __UndoGroup;
        internal void StartGroup()
        {
            __UndoGroup = new UndoGroup();
        }

        internal void EndGroup()
        {
            UndoGroup group = __UndoGroup;
            __UndoGroup = null;

            if (!group.IsEmptyAction())
                PushUndoAction(group);
        }

        internal void ClearStacks()
        {
            __UndoActions.Clear();
            __RedoActions.Clear();
            DoUndoCountChanged();
        }

        public void DoUndoCountChanged()
        {
            EventHandler h = UndoActionsCountChanged;
            if (h != null)
                h(this, new EventArgs());
        }

        internal int UndoActionsCount
        {
            get
            {
                return __UndoActions.Count;
            }
        }

        public void SaveUndoActionStack(DocumentUndoAction undoAction)
        {
            /*
            var ser = new DataContractSerializer(typeof(DocumentUndoAction));            
            using (XmlWriter xw = XmlWriter.Create(Console.Out))
            {
                ser.WriteObject(xw, undoAction);
            }*/
        }
    }
}
