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
using System.Windows.Controls;
using UVOutliner.Undo;
using System.Collections.ObjectModel;
using DragDropListBox;
using UVOutliner.Styles;
using System.Windows.Documents;
using System.IO;
using System.Windows.Markup;

namespace UVOutliner
{
    public static class DocumentHelpers
    {
        public static bool CanMoveNodeUp(OutlinerNote note, TreeListView outlinerTree)
        {
            int selectedIndex = note.Parent.SubNotes.IndexOf(note);
            if (selectedIndex == 0)
                return false;

            return true;
        }

        public static void MoveNodeUp(OutlinerNote selectedItem, TreeListView outlinerTree, int activeColumn, bool isInlineNoteActive)
        {
            int selectedIndex = selectedItem.Parent.SubNotes.IndexOf(selectedItem);
            if (selectedIndex == 0)
                return;

            OutlinerNote newNote = new OutlinerNote(selectedItem.Parent);
            newNote.Clone(selectedItem);
            CopyNodesRecursively(newNote, selectedItem);

            selectedItem.Parent.SubNotes.Remove(selectedItem);
            selectedItem.Parent.SubNotes.Insert(selectedIndex - 1, newNote);

            outlinerTree.MakeActive(newNote, activeColumn, isInlineNoteActive);
        }

        public static bool CanMoveNodeDown(OutlinerNote note, TreeListView outlinerTree)
        {
            int selectedIndex = note.Parent.SubNotes.IndexOf(note);
            if (selectedIndex >= note.Parent.SubNotes.Count - 1)
                return false;

            return true;
        }

        public static void MoveNodeDown(OutlinerNote selectedItem, TreeListView outlinerTree, int activeColumn, bool isInlineNoteActive)
        {
            int selectedIndex = selectedItem.Parent.SubNotes.IndexOf(selectedItem);
            if (selectedIndex >= selectedItem.Parent.SubNotes.Count - 1)
                return;
            
            OutlinerNote newNote = new OutlinerNote(selectedItem.Parent);
            newNote.Clone(selectedItem);

            CopyNodesRecursively(newNote, selectedItem);            

            selectedItem.Parent.SubNotes.Remove(selectedItem);
            selectedItem.Parent.SubNotes.Insert(selectedIndex + 1, newNote);

            outlinerTree.MakeActive(newNote, activeColumn, isInlineNoteActive);
        }

        public static void CopyNodesRecursively(OutlinerNote destination, OutlinerNote source)
        {
            foreach (OutlinerNote subnote in source.SubNotes)
            {
                OutlinerNote newNote = new OutlinerNote(destination);
                newNote.Clone(subnote);
                destination.SubNotes.Add(newNote);

                CopyNodesRecursively(newNote, subnote);
            }
        }

        private static void CopyNodesRecursively(OutlinerNote destination1, OutlinerNote destination2, OutlinerNote source, OutlinerNote limit)
        {
            foreach (OutlinerNote subnote in source.SubNotes)
            {

                OutlinerNote newNote = new OutlinerNote(destination1);
                newNote.Clone(subnote);
                destination1.SubNotes.Add(newNote);

                CopyNodesRecursively(newNote, subnote);

                if (subnote == limit)
                    destination1 = destination2;
            }
        }

        public static int GetFocusedColumnIdx(TreeListView outlinerTree, OutlinerNote note)
        {
            MainWindow mainWindow = DragDropHelper.GetMainWindow(outlinerTree);
            if (mainWindow.IsEditorSelected == true)
                return mainWindow.LastColumn;

            return -1;
        }

        public static void ApplyLevelStyle(OutlinerNote note, MyEdit myEdit)
        {            
            ApplyNewLevelStyle(note, note.Level);
        }

        public static void ApplyLevelStyleForEdit(OutlinerNote note, MyEdit myEdit)
        {
            // нолевой уровень - корень
            if (note.Level == -1)
                return;

            int level = note.Level;
            LevelStyle levelStyle = note.Document.Styles.GetStyleForLevel(level);
            LevelStyle wholeDocumentStyle = note.Document.Styles.WholeDocumentStyle;

            if (myEdit != null)
            {
                wholeDocumentStyle.ApplyToMyEdit(myEdit);
                wholeDocumentStyle.ApplyToRange(myEdit.Selection);
                levelStyle.ApplyToMyEdit(myEdit);
                levelStyle.ApplyToRange(myEdit.Selection);
                //UpdateFontSettings(myEdit.Selection);
            }
        }

        public static void ApplyNewLevelStyle(OutlinerNote note, int newLevel)
        {
            // нолевой уровень - корень
            if (newLevel <= 0)
                return;

            UndoLevelStyle undoLevelStyle = new UndoLevelStyle(note);

            if (note.LastStyleApplied != null)
                note.LastStyleApplied.UnapplyStyle(note);

            LevelStyle levelStyle = note.Document.Styles.GetStyleForLevel(newLevel);
            LevelStyle wholeDocumentStyle = note.Document.Styles.WholeDocumentStyle;

            wholeDocumentStyle.ApplyToNote(note);
            levelStyle.ApplyToNote(note);
            note.LastStyleApplied = levelStyle;

            undoLevelStyle.StyleApplied(note);
            
            note.Document.UndoManager.PushUndoAction(undoLevelStyle);
        }

        public static void ApplyNewInlineNoteStyle(MyEdit edit, OutlinerNote note)
        {
            BaseStyle inlineStyle = note.Document.Styles.InlineNoteStyle;

            TextRange range = new TextRange(
                            note.InlineNoteDocument.ContentStart, 
                            note.InlineNoteDocument.ContentEnd);
            
            inlineStyle.ApplyToRange(range);
            inlineStyle.ApplyToDocument(note.InlineNoteDocument);
        }

        public static bool CanIncreaseIndent(OutlinerNote row)
        {
            ObservableCollection<OutlinerNote> parentCollection = row.Document.GetParentCollection(row);
            int idx = GetNoteIndexAtParent(row);
            if (idx == -1 || idx == 0)
                return false;

            return true;
        }

        public static void IncreaseIndent(OutlinerNote row, TreeListView outlinerTree, bool applyStyle)
        {
            if (!CanIncreaseIndent(row))
                return;

            int activeColumn = DocumentHelpers.GetFocusedColumnIdx(outlinerTree, row);
            bool isInlineNoteFocused = DocumentHelpers.IsInlineNoteFocused(outlinerTree);
            
            ObservableCollection<OutlinerNote> parentCollection = row.Document.GetParentCollection(row);
            int idx = GetNoteIndexAtParent(row);
            parentCollection.Remove(row);

            OutlinerNote newNote = new OutlinerNote(parentCollection[idx - 1]);
            newNote.Clone(row);

            DocumentHelpers.CopyNodesRecursively(newNote, row);

            parentCollection[idx - 1].SubNotes.Add(newNote);
            parentCollection[idx - 1].IsExpanded = true;

            row.Parent.UpdateParentCheckboxes();
            newNote.UpdateParentCheckboxes();
            if (applyStyle)
                outlinerTree.MakeActive(newNote, activeColumn, isInlineNoteFocused, new EventHandler(ApplyStyleAfterMakeActive));
            else
                outlinerTree.MakeActive(newNote, activeColumn, isInlineNoteFocused);
        }

        public static bool IsInlineNoteFocused(TreeListView outlinerTree)
        {
            MainWindow mainWindow = DragDropHelper.GetMainWindow(outlinerTree);
            return mainWindow.IsInlineNoteFocused;            
        }

        public static void IncreaseIndentWithLimit(OutlinerNote row, OutlinerNote limit, bool isInlineNoteFocused, TreeListView outlinerTree, bool applyStyle)
        {
            if (!CanIncreaseIndent(row))
                return;

            int activeColumn = DocumentHelpers.GetFocusedColumnIdx(outlinerTree, row);

            ObservableCollection<OutlinerNote> parentCollection = row.Document.GetParentCollection(row);
            int idx = GetNoteIndexAtParent(row);
            parentCollection.Remove(row);

            OutlinerNote newNote = new OutlinerNote(parentCollection[idx - 1]);
            newNote.Clone(row);

            int insertIntoIdx = parentCollection[idx - 1].SubNotes.Count;
            if (limit == row)
                DocumentHelpers.CopyNodesRecursively(parentCollection[idx - 1], row);
            else 
                DocumentHelpers.CopyNodesRecursively(newNote, parentCollection[idx - 1], row, limit);
            
            parentCollection[idx - 1].SubNotes.Insert(insertIntoIdx, newNote);
            parentCollection[idx - 1].IsExpanded = true;

            row.Parent.UpdateParentCheckboxes();
            newNote.UpdateParentCheckboxes();
            if (applyStyle)
                outlinerTree.MakeActive(newNote, activeColumn, isInlineNoteFocused, new EventHandler(ApplyStyleAfterMakeActive));
            else
                outlinerTree.MakeActive(newNote, activeColumn, isInlineNoteFocused);
        }        

        public static void ApplyStyleAfterMakeActive(object sender, EventArgs e)
        {
            MakeActiveArgs ema = (MakeActiveArgs)e;
            DocumentHelpers.ApplyLevelStyle(
                ema.Note, ema.Edit);
        }

        public static bool CanDecreaseIndent(OutlinerNote selectedRow)
        {
            if (selectedRow == null)
                return false;

            if (selectedRow.Parent.IsRoot)
                return false;

            return true;
        }

        public static void DecreaseIndent(OutlinerNote selectedRow, TreeListView outlinerTree, bool applyStyle)
        {
            int activeColumn = DocumentHelpers.GetFocusedColumnIdx(outlinerTree, selectedRow);
            bool inlineNoteFocused = IsInlineNoteFocused(outlinerTree);
            OutlinerNote newRow = new OutlinerNote(selectedRow.Parent.Parent);
            newRow.Clone(selectedRow);
            newRow.Parent.IsExpanded = true;
            newRow.IsExpanded = true;
            DocumentHelpers.CopyNodesRecursively(newRow, selectedRow);

            int currentRowIndex = selectedRow.Parent.SubNotes.IndexOf(selectedRow);
            for (int i = currentRowIndex + 1; i < selectedRow.Parent.SubNotes.Count; i++)
            {
                OutlinerNote note = selectedRow.Parent.SubNotes[i];
                OutlinerNote newNote = new OutlinerNote(newRow);
                newNote.Clone(note);

                DocumentHelpers.CopyNodesRecursively(newNote, note);
                newRow.SubNotes.Add(newNote);
            }

            for (int i = selectedRow.Parent.SubNotes.Count - 1; i > currentRowIndex; i--)
                selectedRow.Parent.SubNotes.RemoveAt(i);

            int parentIdx = selectedRow.Parent.Parent.SubNotes.IndexOf(selectedRow.Parent);
            selectedRow.Parent.Parent.SubNotes.Insert(parentIdx + 1, newRow);

            selectedRow.Parent.SubNotes.Remove(selectedRow);

            selectedRow.Parent.UpdateParentCheckboxes();
            newRow.UpdateParentCheckboxes();
            if (applyStyle)
                outlinerTree.MakeActive(newRow, activeColumn, inlineNoteFocused, new EventHandler(ApplyStyleAfterMakeActive));
            else
                outlinerTree.MakeActive(newRow, activeColumn, inlineNoteFocused);
        }

        private static int GetNoteIndexAtParent(OutlinerNote note)
        {
            ObservableCollection<OutlinerNote> parentCollection = note.Document.GetParentCollection(note);

            for (int i = 0; i < parentCollection.Count; i++)
                if (parentCollection[i] == note)
                    return i;

            return -1;
        }

        public static void DeleteRow(OutlinerNote row, TreeListView outlinerTree)
        {
            DeleteRow(row, outlinerTree, -1);
        }

        public static void DeleteRow(OutlinerNote row, TreeListView outlinerTree, int columnIndex)
        {
            int idx = row.Parent.SubNotes.IndexOf(row);
            row.Parent.SubNotes.Remove(row);

            row.UpdateParentCheckboxes();
            if (row.Parent.SubNotes.Count > 0)
            {
                if (row.Parent.SubNotes.Count > idx)
                    outlinerTree.MakeActive(row.Parent.SubNotes[idx], columnIndex, false);
                else
                    outlinerTree.MakeActive(row.Parent.SubNotes[row.Parent.SubNotes.Count - 1], columnIndex, false);
            }
        }

        public static void GetLinearNotesList(OutlinerNote outlinerNote, List<OutlinerNote> notes, bool expandCollapsed)
        {
            for (int i = 0; i < outlinerNote.SubNotes.Count; i++)
            {
                notes.Add(outlinerNote.SubNotes[i]);
                if (expandCollapsed || outlinerNote.SubNotes[i].IsExpanded == true)
                    GetLinearNotesList(outlinerNote.SubNotes[i], notes, expandCollapsed);
            }
        }

        internal static MemoryStream SaveDocumentToStream(FlowDocument flowDocument)
        {
            MemoryStream stream = new MemoryStream();
            XamlWriter.Save(flowDocument, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        internal static FlowDocument RestoreDocumentFromStream(MemoryStream stream)
        {
            MemoryStream newStream = new MemoryStream();
            stream.WriteTo(newStream);
            stream.Seek(0, SeekOrigin.Begin);
            newStream.Seek(0, SeekOrigin.Begin);
            var document = (FlowDocument)XamlReader.Load(newStream);            
            return document;
        }
    }

}
