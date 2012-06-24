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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using UVOutliner.Undo;
using UVOutliner.Columns;
using UVOutliner.Styles;
using System.Collections.Specialized;

namespace UVOutliner
{
    public class OutlinerNote : DependencyObject, INotifyPropertyChanged
    {
        static int s_id = 0;

        private OutlinerDocument __Document = null;

        private BulletType __BulletType = BulletType.Number;
        private string __BulletSeparator = ".";

        private OutlinerNote __Parent;
        private OutlinerNoteCollection __Subnotes;
        private bool __IsExpanded;
        private bool __IsChecked;
        private bool __IsDocumentRoot;

        private List<OutlinerColumn> __Columns;
        private FlowDocument __InlineNoteDocument;

        private BaseStyle __StyleApplied;
        private bool __IsEmptyNote = true;
        private bool __IsTemporary = false;

        private bool __DragOverNote = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public OutlinerNote(OutlinerNote parent)
        {
            this.__Document = parent.Document;
            this.__Parent = parent;

            this.__Subnotes = new OutlinerNoteCollection();
            this.__Subnotes.Collection.CollectionChanged += new NotifyCollectionChangedEventHandler(Collection_CollectionChanged);
            CreateColumnData();

            Id = s_id;
            s_id += 1;
        }

        void Collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var note in __Subnotes.Collection)
            {
                note.OnPropertyChanged("ItemNumber");
                note.OnPropertyChanged("FormattedItemNumber");
            }

            Document.ResetOddEven();
        }

        public OutlinerNote(OutlinerDocument document, OutlinerNoteCollection Subnotes)
        {
            this.__Document = document;
            this.__Parent = null;
            __IsDocumentRoot = true;
            this.__Subnotes = Subnotes;
            this.__Subnotes.Collection.CollectionChanged += new NotifyCollectionChangedEventHandler(Collection_CollectionChanged);
            Id = s_id;
            s_id += 1;
            CreateColumnData();
        }

        private void CreateColumnData()
        {
            __Columns = new List<OutlinerColumn>();
            for (int i = 0; i < Document.ColumnDefinitions.Count; i++)
                __Columns.Add(ColumnHelpers.CreateColumnClass(this, Document.ColumnDefinitions[i]));
        }

        public void CreateMissingColumns()
        {
            if (__Columns.Count < Document.ColumnDefinitions.Count)
                for (int i = __Columns.Count; i < Document.ColumnDefinitions.Count; i++)
                    __Columns.Add(ColumnHelpers.CreateColumnClass(this, Document.ColumnDefinitions[i]));
        }

        public List<OutlinerColumn> Columns
        {
            get { return __Columns; }
        }

        public int Id
        {
            get;
            set;
        }

        // This property is used to hide bullet for an empty string
        public bool IsEmpty
        {
            get { return __IsEmptyNote; }
            set
            {
                __IsEmptyNote = value;
                OnPropertyChanged("IsEmpty");
            }
        }

        public bool Temporary
        {
            get { return __IsTemporary; }
            set { __IsTemporary = value; }
        }

        public Visibility BulletVisibility
        {
            get
            {
                if (__IsEmptyNote)
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;
            }
        }

        public int Level
        {
            get
            {
                if (IsDocumentRoot)
                    return -1;

                int level = 1;
                OutlinerNote parent = this.Parent;

                while (parent.IsDocumentRoot == false)
                {
                    level++;
                    parent = parent.Parent;
                }

                return level;
            }
        }

        public BaseStyle LastStyleApplied
        {
            get { return __StyleApplied; }
            set { __StyleApplied = value; }
        }

        public OutlinerDocument Document
        {
            get { return __Document; }
        }

        public bool WasAltered
        {
            get { return __Document.WasAltered; }
            set { __Document.WasAltered = value; }
        }

        private bool __LevelSelected = false;

        public bool LevelSelected
        {
            get { return __LevelSelected; }
            set
            {
                __LevelSelected = value;
                OnPropertyChanged("LevelSelected");
            }
        }

        private bool __InlineSelected = false;

        public bool InlineSelected
        {
            get { return __InlineSelected; }
            set
            {
                __InlineSelected = value;
                OnPropertyChanged("InlineSelected");
            }
        }

        public ObservableCollection<OutlinerNote> SubNotes
        {
            get
            {
                return __Subnotes.Collection;
            }
        }

        public bool IsExpanded
        {
            get { return __IsExpanded; }
            set
            {
                __IsExpanded = value;
                OnPropertyChanged("IsExpanded");
                Document.ResetOddEven();
            }
        }

        public OutlinerNote Parent
        {
            get { return __Parent; }
            set { __Parent = value; }
        }

        public bool IsRoot
        {
            get
            {
                if (Document.IsHoistRoot(this))
                    return true;

                return __IsDocumentRoot;
            }
        }

        public bool IsDocumentRoot
        {
            get
            {
                return __IsDocumentRoot;
            }
        }

        public void OnPropertyChanged(string p)
        {
            PropertyChangedEventHandler h = PropertyChanged;
            if (h != null)
                h(this, new PropertyChangedEventArgs(p));
        }

        public FlowDocument DefaultRichTextDocument
        {
            get { return (FlowDocument)(Columns[0].ColumnData); }
        }

        public bool? IsChecked
        {
            get
            {

                return GetChecked();
            }
            set
            {
                __IsChecked = value == true;
                OnPropertyChanged("IsChecked");
                OnPropertyChanged("IsCheckedDirect");

                for (int i = 0; i < __Subnotes.Collection.Count; i++)
                    __Subnotes.Collection[i].IsChecked = value;

                UpdateParentCheckboxes();
            }
        }

        public void UpdateParentCheckboxes()
        {
            OutlinerNote parent = Parent;
            while (parent != null)
            {
                parent.OnPropertyChanged("IsChecked");
                parent.OnPropertyChanged("IsCheckedDirect");
                parent = parent.Parent;
            }

            OnPropertyChanged("IsChecked");
            OnPropertyChanged("IsCheckedDirect");
        }

        private bool? GetChecked()
        {
            if (__Subnotes.Collection.Count == 0)
                return __IsChecked;

            int checkedSubnoted = 0;

            for (int i = 0; i < __Subnotes.Collection.Count; i++)
            {
                if (__Subnotes.Collection[i].IsChecked == null)
                {
                    return false;
                }

                if (__Subnotes.Collection[i].IsChecked == true)
                    checkedSubnoted++;
            }

            if (checkedSubnoted == __Subnotes.Collection.Count)
            {
                return true;
            }

            if (checkedSubnoted == 0)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Check only current node, leave subnodes without change
        /// </summary>
        /// <param name="isChecked"></param>
        public void SetCheckedForCurrentNote(bool value)
        {
            __IsChecked = value;
            OnPropertyChanged("IsChecked");
            OnPropertyChanged("IsCheckedDirect");
        }

        public bool? IsCheckedDirect
        {
            get { return GetChecked(); }
            set
            {
                UndoCheck check = new UndoCheck(this, value == true, false);
                Document.UndoManager.PushUndoAction(check);

                IsChecked = value;
                Document.WasAltered = true;
            }
        }

        internal void Clone(OutlinerNote throwawayNote)
        {
            for (int i = 0; i < __Columns.Count; i++)
                __Columns[i].CopyColumn(throwawayNote.Columns[i]);

            CopyInlineNote(throwawayNote);

            IsExpanded = throwawayNote.IsExpanded;
            IsChecked = throwawayNote.IsChecked;
            WasAltered = throwawayNote.WasAltered;
            LastStyleApplied = throwawayNote.LastStyleApplied;
            IsEmpty = throwawayNote.IsEmpty;
            Id = throwawayNote.Id;
        }

        private void CopyInlineNote(OutlinerNote throwawayNote)
        {
            if (throwawayNote.InlineNoteDocument == null)
                return;

            FlowDocument inlineDoc = throwawayNote.InlineNoteDocument;
            __InlineNoteDocument = FlowDocumentUtils.CopyFlowDocument(inlineDoc);
            OnPropertyChanged("HasInlineNote");
            OnPropertyChanged("InlineNoteDocument");
        }

        internal OutlinerNote GetRoot()
        {
            OutlinerNote tmpParent = __Parent;

            while (!tmpParent.IsRoot)
                tmpParent = tmpParent.Parent;

            return tmpParent;
        }

        internal void UpdateIsEmpty()
        {
            for (int i = 0; i < __Columns.Count; i++)
            {
                if (!__Columns[i].IsEmpty)
                {
                    IsEmpty = false;
                    return;
                }
            }

            IsEmpty = true;
        }

        internal void ColumnDocumentChanged(OutlinerColumn column)
        {
            int columnIndex = __Columns.IndexOf(column);
            if (columnIndex != -1)
                OnPropertyChanged(String.Format("Columns[{0}].ColumnData", columnIndex));

        }

        internal bool OwnsDocument(Guid documentGuid)
        {
            for (int i = 0; i < __Columns.Count; i++)
            {
                if (__Columns[i].OwnsDocument(documentGuid))
                    return true;
            }

            if (__InlineNoteDocument != null)
                if (__InlineNoteDocument.Tag.Equals(documentGuid))
                    return true;

            return false;
        }

        internal void DeleteColumn(int columnId)
        {
            __Columns.RemoveAt(columnId);
        }

        public bool DragOverNote
        {
            get
            {
                return __DragOverNote;
            }

            set
            {
                __DragOverNote = value;
                OnPropertyChanged("DragOverNote");
            }
        }

        public FlowDocument InlineNoteDocument
        {
            get
            {
                return __InlineNoteDocument;
            }
        }

        public bool HasInlineNote
        {
            get
            {
                if (__InlineNoteDocument == null)
                    return false;

                return true;
            }
        }

        internal void CreateInlineNote(FlowDocument defaultDoc = null)
        {
            if (defaultDoc == null)
                __InlineNoteDocument = new FlowDocument();
            else
                __InlineNoteDocument = defaultDoc;

            __InlineNoteDocument.Tag = Guid.NewGuid();

            Document.FocusInlineEditAfterTemplateChange = true;
            OnPropertyChanged("HasInlineNote");
        }

        public bool IsInlineNoteEmpty()
        {
            if (__InlineNoteDocument == null)
                return true;

            TextRange range = new TextRange(
                            __InlineNoteDocument.ContentStart,
                            __InlineNoteDocument.ContentEnd);

            if (range.Text.Trim() == "")
                return true;

            return false;
        }

        internal void RemoveInlineNoteIfEmpty()
        {

            if (IsInlineNoteEmpty())
                RemoveInlineNote();
        }

        public void RemoveInlineNote()
        {
            __InlineNoteDocument = null;
            OnPropertyChanged("HasInlineNote");
        }


        private Brush __TransparentBrush = new SolidColorBrush(Colors.Transparent);

        public Brush Background
        {
            get
            {
                if (IsEven)
                    return Document.EvenBackgroundColor;
                else
                    return Document.OddBackgroundColor;
            }
        }

        bool __IsEven;

        public bool IsEven
        {
            get
            {
                return __IsEven;
            }

            set
            {
                __IsEven = value;
                OnPropertyChanged("IsEven");
                OnPropertyChanged("Background");
            }
        }

        private bool __ShowCheckbox;

        public bool ShowCheckbox
        {
            get
            {
                return __ShowCheckbox;
            }

            set
            {
                __ShowCheckbox = value;
                OnPropertyChanged("ShowCheckbox");
            }
        }

        public int ItemNumber
        {
            get
            {
                return Parent.__Subnotes.Collection.IndexOf(this);
            }
        }

        public string FormattedItemNumber
        {
            get
            {
                if (Parent.__BulletType == BulletType.Bullet)
                    return "•";
                else
                    return String.Format("{0:N0}{1}", ItemNumber, __BulletSeparator);
            }
        }

        private bool __IsItemNumber;
        public bool IsItemNumber
        {
            get
            {
                return __IsItemNumber;
            }

            set
            {
                __IsItemNumber = value;
                OnPropertyChanged("IsItemNumber");
            }
        }

    }
}