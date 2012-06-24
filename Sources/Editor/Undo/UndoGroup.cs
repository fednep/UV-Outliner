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
using System.Text;
using System.Windows.Controls;

namespace UVOutliner.Editor
{
    public class UndoGroup: UVEditUndoAction
    {
        private List<UVEditUndoAction> __UndoActions;

        public UndoGroup()
        {
            __UndoActions = new List<UVEditUndoAction>();
        }

        public void Add(UVEditUndoAction action)
        {
            __UndoActions.Add(action);
        }

        public override void Undo(RichTextBox richTextBox)
        {
            for (int i = __UndoActions.Count - 1; i >= 0; i--)
                __UndoActions[i].Undo(richTextBox);
        }

        public override void Redo(RichTextBox edit)
        {
            for (int i = 0; i < __UndoActions.Count; i++)
                __UndoActions[i].Redo(edit);
        }
    }
}
