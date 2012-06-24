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
using UVOutliner.Styles;

namespace UVOutliner.Undo
{

    public class UndoStyleChange : DocumentUndoAction
    {
        private Guid __StyleTag;
        private List<LevelStyleProperty> __BeforeChange;
        private List<LevelStyleProperty> __AfterChange;         

        public UndoStyleChange(BaseStyle style)
        {
            __StyleTag = style.Tag;
            __BeforeChange = new List<LevelStyleProperty>();
            for (int i = 0; i < style.Count; i++)
                __BeforeChange.Add(new LevelStyleProperty(style.Properties[i].PropertyType, style.Properties[i].Value));
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {

            BaseStyle style = document.Styles.GetStyleByTag(__StyleTag);

            if (__AfterChange == null)
            {
                __AfterChange = new List<LevelStyleProperty>();
                for (int i = 0; i < style.Count; i++)
                    __AfterChange.Add(new LevelStyleProperty(style.Properties[i].PropertyType, style.Properties[i].Value));
            }

            style.Properties.Clear();
            for (int i = 0; i < __BeforeChange.Count; i++)
                style.AddProperty(__BeforeChange[i].PropertyType, __BeforeChange[i].Value);

            style.UpdateInspectorStyles();

        }

        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            BaseStyle style = document.Styles.GetStyleByTag(__StyleTag);

            style.Properties.Clear();
            for (int i = 0; i < __AfterChange.Count; i++)
                style.AddProperty(__AfterChange[i].PropertyType, __AfterChange[i].Value);

            style.UpdateInspectorStyles();
        }
    }
}
