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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Documents;
using UVOutliner.Columns;
using System.Windows.Controls;
using System.Windows;
using UVOutliner.Styles;
using System.Windows.Media;

namespace UVOutliner
{
    public delegate void RecursiveWalkDelegate(OutlinerNote note, out bool shouldWalkSubitems, out bool shouldContinue);

    public class OutlinerDocument : ObservableCollection<OutlinerNote>, INotifyPropertyChanged
    {
        public const string DefaultFileName = "Untitled.uvxml";

        private bool __EnableCustomRowBackground = false;

        private SolidColorBrush __EvenBackgroundColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        private SolidColorBrush __OddBackgroundColor = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        private bool __AutoOddBackgroundColor = true;

        private bool __LinesBetweenRows = false;
        private SolidColorBrush __LinesBetweenRowsBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0));

        private string __FileName = DefaultFileName;
        private bool __WasAltered = false;
        private bool __CheckboxesVisible = false;
        private bool __ShowInspectors = true;
        private bool __AutoStyles = true;
        private OutlinerStyles __Styles = new OutlinerStyles();
        private List<OutlinerNote> __HoistedNodes;
        private ColumnDefinitions __ColumnsDefinitions;

        private UndoManager __UndoManager = new UndoManager();

        // FakeRootNote is needed so that elements of the first level have a parent
        private OutlinerNote __FakeRootNote = null;

        public OutlinerDocument()
        {
            __ColumnsDefinitions = new ColumnDefinitions();
            __ColumnsDefinitions.Add(new OutlinerColumnDefinition("Topic", ColumnDataType.RichText));
            __FakeRootNote = new OutlinerNote(this, new OutlinerNoteCollection(this));
            __HoistedNodes = new List<OutlinerNote>();
            __UndoManager.UndoActionsCountChanged += new EventHandler(UndoManager_UndoActionsCountChanged);
        }

        private void UpdateWasAltered()
        {
            WasAltered = __UndoManager.UndoActionsCount != LastActionsCountOnSave;
        }

        void UndoManager_UndoActionsCountChanged(object sender, EventArgs e)
        {
            UpdateWasAltered();
        }

        public void DocumentSaved()
        {
            LastActionsCountOnSave = __UndoManager.UndoActionsCount;
            UpdateWasAltered();
        }

        public int LastActionsCountOnSave
        {
            get;
            set;
        }

        public void SelectLevel(int level)
        {
            WalkRecursively(__FakeRootNote,
                delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    if (note.Level == level || level == -1)
                        note.LevelSelected = true;
                    else
                        note.LevelSelected = false;
                    shouldContinue = true;
                    shouldWalkSubItems = true;
                });
        }

        public void DeselectLevel()
        {
            WalkRecursively(__FakeRootNote, delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
            {
                note.LevelSelected = false;
                shouldContinue = true;
                shouldWalkSubItems = true;
            });
        }

        public static void WalkRecursively(OutlinerNote note, RecursiveWalkDelegate action)
        {
            bool shouldContinue;
            bool shouldWalkSubitems;
            for (int i = 0; i < note.SubNotes.Count; i++)
            {
                action(note.SubNotes[i], out shouldWalkSubitems, out shouldContinue);
                if (shouldContinue == false)
                    break;

                if (shouldWalkSubitems)
                    WalkRecursively(note.SubNotes[i], action);
            }
        }

        public bool CheckboxesVisble
        {
            get { return __CheckboxesVisible; }
            set
            {
                __CheckboxesVisible = value;
                DoPropertyChanged("CheckboxesVisble");
            }
        }

        public bool ShowInspectors
        {
            get { return __ShowInspectors; }
            set
            {
                __ShowInspectors = value;
                DoPropertyChanged("ShowInspectors");
            }
        }

        public OutlinerStyles Styles
        {
            get { return __Styles; }
        }

        public bool AutoStyles
        {
            get { return __AutoStyles; }
            set
            {
                __AutoStyles = value;
                DoPropertyChanged("AutoStyles");
            }
        }

        public bool WasAltered
        {
            get
            {
                return __WasAltered;
            }

            set
            {
                __WasAltered = value;
                DoPropertyChanged("WasAltered");

                // tbd: make better
                (Application.Current.MainWindow as MainWindow).UpdateTitle();
            }
        }

        public bool IsDefaultFileName
        {
            get { return (__FileName == DefaultFileName); }
        }

        public string FileName
        {
            get
            {
                return __FileName;
            }

            set
            {
                __FileName = value;
            }
        }

        /// <summary>
        /// This is a root node
        /// </summary>
        public OutlinerNote FakeRootNode
        {
            get { return __FakeRootNote; }
        }

        public OutlinerNote RootNode
        {
            get
            {
                if (IsHoisted)
                    return HostNode;
                else
                    return __FakeRootNote;
            }
        }

        private void DoPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        internal void UpdateStyle(BaseStyle activeStyle, StylePropertyType stylePropertyType, object value)
        {
            LevelStyle levelStyle = activeStyle as LevelStyle;
            InlineNoteStyle inlineStyle = activeStyle as InlineNoteStyle;

            WalkRecursively(__FakeRootNote,
               delegate(OutlinerNote note, out bool shouldWalkSubnotes, out bool shouldContinue)
               {
                   shouldContinue = true;
                   shouldWalkSubnotes = true;

                   if (activeStyle.StyleType == StyleType.Inline)
                   {
                       if (!note.HasInlineNote)
                           return;

                       TextRange range = new TextRange(note.InlineNoteDocument.ContentStart, note.InlineNoteDocument.ContentEnd);

                       UndoManager.PushUndoAction(new Undo.UndoFlowDocumentFormatting(note, 0, true, false));
                       LevelStyle.ApplyPropertyToRange(range, stylePropertyType, value);
                       LevelStyle.ApplyPropertyToFlowDocument(note.InlineNoteDocument, stylePropertyType, value);
                   }
                   else // if (activeStyle.StyleType == StyleType.Level)
                   {
                       if (note.Level == levelStyle.Level || levelStyle.Level == -1)
                       {
                           ApplyStylePropertyForAllColumns(stylePropertyType, value, levelStyle, note);
                       }
                   }
               });
        }

        private void ApplyStylePropertyForAllColumns(StylePropertyType stylePropertyType, object value, LevelStyle levelStyle, OutlinerNote note)
        {
            for (int i = 0; i < note.Columns.Count; i++)
            {
                if (note.Columns[i].DataType != ColumnDataType.RichText)
                    continue;

                FlowDocument flowDocument = (FlowDocument)note.Columns[i].ColumnData;
                TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);

                UndoManager.PushUndoAction(new Undo.UndoFlowDocumentFormatting(note, i, false, false));
                LevelStyle.ApplyPropertyToRange(range, stylePropertyType, value);
                LevelStyle.ApplyPropertyToFlowDocument(flowDocument, stylePropertyType, value);

                // If document style gets modified, level style should be applied afterwards
                if (levelStyle.Level == -1)
                {
                    LevelStyle currentLevelStyle = note.Document.Styles.GetStyleForLevel(note.Level);
                    for (int si = 0; si < currentLevelStyle.Properties.Count; si++)
                    {
                        if (currentLevelStyle.Properties[si].PropertyType == stylePropertyType)
                        {
                            LevelStyle.ApplyPropertyToRange(range,
                                stylePropertyType,
                                currentLevelStyle.Properties[si].Value);

                            LevelStyle.ApplyPropertyToFlowDocument(
                                flowDocument,
                                stylePropertyType,
                                currentLevelStyle.Properties[si].Value);
                        }
                    }
                }
            }
        }

        public OutlinerNote HostNode
        {
            get
            {
                if (__HoistedNodes.Count == 0)
                    return null;

                return __HoistedNodes[__HoistedNodes.Count - 1];
            }
        }

        public OutlinerNote FirstHostNode
        {
            get
            {
                if (__HoistedNodes.Count == 0)
                    return null;

                return __HoistedNodes[0];
            }
        }

        public bool IsHoisted
        {
            get { return __HoistedNodes.Count > 0; }
        }

        public void Hoist(OutlinerNote row)
        {
            __HoistedNodes.Add(row);
        }

        public void Unhoist()
        {
            if (__HoistedNodes.Count == 0)
                return;

            __HoistedNodes.RemoveAt(__HoistedNodes.Count - 1);
        }

        internal bool IsHoistRoot(OutlinerNote outlinerNote)
        {
            if (__HoistedNodes.Contains(outlinerNote))
                return true;

            return false;
        }

        public OutlinerNote FindOutlinerNoteByDocument(Guid documentGuid)
        {
            OutlinerNote foundNote = null;

            OutlinerDocument.WalkRecursively(RootNode,
                delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    shouldContinue = true;
                    shouldWalkSubItems = true;
                    if (note.OwnsDocument(documentGuid))
                    {
                        foundNote = note;
                        shouldContinue = false;
                    }
                });

            return foundNote;
        }

        internal OutlinerNote FindOutlinerNoteById(int noteId)
        {
            OutlinerNote foundNote = null;

            if (RootNode.Id == noteId)
                return RootNode;

            OutlinerDocument.WalkRecursively(RootNode,
                delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    shouldContinue = true;
                    shouldWalkSubItems = true;
                    if (note.Id == noteId)
                    {
                        foundNote = note;
                        shouldContinue = false;
                    }
                });

            return foundNote;
        }

        public UndoManager UndoManager
        {
            get { return __UndoManager; }
        }

        internal ObservableCollection<OutlinerNote> GetParentCollection(OutlinerNote note)
        {
            if (note.Parent != null)
                return note.Parent.SubNotes;

            return this;
        }

        public ColumnDefinitions ColumnDefinitions
        {
            get
            {
                return __ColumnsDefinitions;
            }
        }

        internal void DeselectInlineNote()
        {
            WalkRecursively(__FakeRootNote,
                delegate(OutlinerNote note, out bool shouldWalkSubnotes, out bool shouldContinue)
                {
                    note.InlineSelected = false;
                    shouldContinue = true;
                    shouldWalkSubnotes = true;
                });
        }

        internal void SelectInlineNotes()
        {
            WalkRecursively(__FakeRootNote,
                delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    note.InlineSelected = true;

                    shouldContinue = true;
                    shouldWalkSubItems = true;
                });
        }

        public bool FocusInlineEditAfterTemplateChange
        {
            get;
            set;
        }

        public bool FocusEditAfterTemplateChange
        {
            get;
            set;
        }


        internal void ResetOddEven()
        {
            int i = 1;
            WalkRecursively(__FakeRootNote,
                delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    shouldContinue = true;
                    shouldWalkSubItems = false;
                    if (note.IsExpanded)
                        shouldWalkSubItems = true;

                    note.IsEven = (i % 2) == 0;                    
                    i += 1;
                });
        }       

        public bool EnableCustomRowBackground
        {
            get
            {
                return __EnableCustomRowBackground;
            }
            set
            {
                __EnableCustomRowBackground = value;
                DoPropertyChanged("EnableCustomRowBackground");
            }
        }

        public SolidColorBrush EvenBackgroundColor
        {
            get
            {
                return __EvenBackgroundColor;
            }
            set
            {
                __EvenBackgroundColor = value;
                DoPropertyChanged("EvenBackgroundColor");
                ResetOddEven();
            }
        }

        public SolidColorBrush OddBackgroundColor
        {
            get
            {
                return __OddBackgroundColor;
            }

            set
            {
                __OddBackgroundColor = value;
                DoPropertyChanged("OddBackgroundColor");
                ResetOddEven();
            }
        }

        public bool AutoOddBackgroundColor
        {
            get
            {
                return __AutoOddBackgroundColor;
            }
            set
            {
                __AutoOddBackgroundColor = value;
                
                DoPropertyChanged("AutoOddBackgroundColor");

                if (value == true)
                    OddBackgroundColor = GetAutoOddBackgroundColor(EvenBackgroundColor);                    
                
                ResetOddEven();
            }
        }

        public bool LinesBetweenRows
        {
            get
            {
                return __LinesBetweenRows;
            }

            set
            {
                __LinesBetweenRows = value;
                DoPropertyChanged("LinesBetweenRows");
            }
        }

        public SolidColorBrush LinesBetweenRowsBrush
        {
            get
            {
                return __LinesBetweenRowsBrush;
            }

            set
            {
                __LinesBetweenRowsBrush = value;
                DoPropertyChanged("LinesBetweenRowsBrush");
            }
        }

        public SolidColorBrush GetAutoOddBackgroundColor(SolidColorBrush brush)
        {
            var color = brush.Color;
            return new SolidColorBrush(Color.FromRgb((byte)(color.R * 0.5 + 255 * 0.5),
                                                     (byte)(color.G * 0.5 + 255 * 0.5),
                                                     (byte)(color.B * 0.5 + 255 * 0.5)));
        }


    }
}
