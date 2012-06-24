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
using System.IO;
using System.Runtime.Serialization;

namespace UVOutliner.Undo
{
    struct SavedCheckState
    {        
        public int NodeId;
        public bool IsChecked;
    }


    public class UndoCheck : DocumentUndoAction
    {
        private int __NodeId;
        private bool __SelectNote;
        private bool __WasChecked;

        private List<SavedCheckState> __SavedCheckboxes;

        public UndoCheck(BinaryReader reader)
        {
            __NodeId = reader.ReadInt32();
            __SelectNote = reader.ReadBoolean();
            __WasChecked = reader.ReadBoolean();
        }

        public void SavedCheckState(BinaryWriter writer)
        {
            writer.Write(__NodeId);
            writer.Write(__SelectNote);
            writer.Write(__WasChecked);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="note"></param>
        /// <param name="wasChecked">Была ли заметка выбрана до переключения</param>
        public UndoCheck(OutlinerNote currentNote, bool wasChecked, bool selectNote)
        {
            
            __WasChecked = wasChecked;
            __NodeId = currentNote.Id;

            __SelectNote = selectNote;

            __SavedCheckboxes = new List<SavedCheckState>();
            SavedCheckState state = new SavedCheckState();
            state.NodeId = currentNote.Id;
            state.IsChecked = currentNote.IsChecked == true;
            __SavedCheckboxes.Add(state);


            OutlinerDocument.WalkRecursively(currentNote,
                (RecursiveWalkDelegate)delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    shouldContinue = true;
                    shouldWalkSubItems = true;
                    state = new SavedCheckState();
                    state.NodeId = note.Id;
                    state.IsChecked = note.IsChecked == true;
                    __SavedCheckboxes.Add(state);
                });
        }

        public override void Undo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote rootNote = null;
            for (int i = 0; i < __SavedCheckboxes.Count; i++)
            {
                OutlinerNote note = document.FindOutlinerNoteById(__SavedCheckboxes[i].NodeId);
                note.SetCheckedForCurrentNote(__SavedCheckboxes[i].IsChecked);

                if (rootNote == null)
                    rootNote = note;
            }

            for (int i = 0; i < __SavedCheckboxes.Count; i++)
            {
                OutlinerNote note = document.FindOutlinerNoteById(__SavedCheckboxes[i].NodeId);
                note.UpdateParentCheckboxes();
            }

            if (__SelectNote && rootNote != null)
            {
                treeListView.MakeActive(rootNote, -1, false);
            }

            rootNote.UpdateParentCheckboxes();
            rootNote.OnPropertyChanged("IsChecked");
            rootNote.OnPropertyChanged("IsCheckedDirect");
        }

        
        public override void Redo(OutlinerDocument document, TreeListView treeListView)
        {
            OutlinerNote note = document.FindOutlinerNoteById(__NodeId);
            note.IsChecked = __WasChecked;

            if (__SelectNote)
                treeListView.MakeActive(note, -1, false);

            note.UpdateParentCheckboxes();
            note.OnPropertyChanged("IsChecked");
            note.OnPropertyChanged("IsCheckedDirect");
        }
    }
}
