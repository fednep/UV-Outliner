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

namespace UVOutliner
{
    public class UVEditUndoAction
    {
        private bool __UndoNext = false;

        virtual public void Undo(RichTextBox richTextBox) { }
        virtual public void Redo(RichTextBox edit) { }

        virtual public bool CanBeMergedWith(UndoTextEnter undoAction)
        {
            return false;
        }
        virtual public void Merge(UndoTextEnter undoAction) { }

        public bool UndoNext
        {
            get
            {
                return __UndoNext;
            }
            set
            {
                __UndoNext = value;
            }
        }
    }
}
