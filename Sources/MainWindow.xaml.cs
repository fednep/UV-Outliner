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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using UVOutliner.Lib;
using System.IO;
using System.Windows.Threading;
using System.Windows.Documents;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.Samples.CustomControls;
using System.Windows.Interop;
using System.ComponentModel;
using UVOutliner.Editor;
using UVOutliner.Undo;
using System.Threading;
using UVOutliner.UnhandledException;
using DragDropListBox;
using UVOutliner.Columns;
using UVOutliner.Styles;

namespace UVOutliner
{
    /// <summary>
    /// Interaction log for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private OutlinerDocument __Document = new OutlinerDocument();
        private DispatcherTimer __PreloadFontFamiliesTimer = new DispatcherTimer();
        private BaseStyle __ActiveStyle;             
        private GridViewColumnCollection __OutlinerTreeColumns;

        private MyEdit __SelectedEdit = null;
        private int __SelectedEditColumn = -1;
        private int __LastColumn = 0;
        private bool __FontParametersChanging = false;

        public static RoutedCommand TourAndScreenshots = new RoutedCommand();
        public static RoutedCommand Support = new RoutedCommand();
        public static RoutedCommand About = new RoutedCommand();

        public static SolidColorBrush DefaultForegroundBrush = Brushes.Black;

        public MainWindow()
        {
            InitializeComponent();
            __Document.Styles.StyleChanged += new EventHandler<StyleChangedArgs>(Styles_StyleChanged);
            SetupColumns();

            InitFontComboBox();
            Loaded += new RoutedEventHandler(Window1_Loaded);
            Closing += new System.ComponentModel.CancelEventHandler(Window1_Closing);
            TryLoadPosition(OutlinerDocument.DefaultFileName);

            __Document.Add(new OutlinerNote(__Document.RootNode));

            UpdateOutlinerTreeItemsSource();
            OutlinerTree.Focus();
            OutlinerTree.SizeChanged += new SizeChangedEventHandler(OutlinerTree_SizeChanged);

            ReloadRecentItems();

            if (Application.Current.Properties["FileNameToOpen"] != null)
                OpenFile((string)Application.Current.Properties["FileNameToOpen"]);
            else
            {
                if (((app)Application.Current).NoAutoLoad == false && UVOutliner.Settings.AutoOpenLastSavedFile == true)
                {
                    OpenRecentFile();
                }
            }

            AddCommandBindings();

            AddHandler(MyEdit.EditorGotFocusEvent, new RoutedEventHandler(OnEditGotFocus));
            AddHandler(MyEdit.EditorLostFocusEvent, new RoutedEventHandler(OnEditLostFocus));

            AddHandler(MyEdit.MoveToPrevLine, new RoutedEventHandler(OnMoveToPrevLine));
            AddHandler(NoteContentPresenter.TemplateChangedEvent, new RoutedEventHandler(OnTemplateChangeOnContentControl));
            AddHandler(MyEdit.MoveToNextLine, new RoutedEventHandler(OnMoveToNextLine));
            AddHandler(MyEdit.UndoActionEvent, new RoutedEventHandler(OnUndoAction));
            AddHandler(MyEdit.MoveToNextColumn, new RoutedEventHandler(OnMoveToNextColumn));
            AddHandler(MyEdit.MoveToPrevColumn, new RoutedEventHandler(OnMoveToPrevColumn));

            AddHandler(TreeListViewItem.EditingFinishedEvent, new RoutedEventHandler(OnEditingFinished));

            AddHandler(CommandManager.PreviewExecutedEvent, new ExecutedRoutedEventHandler(PreviewExecutedEvent));

            if (UVOutliner.Settings.FontSelectionPreview)
                StartFontsPreload();

            OutlinerTree.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(OutlinerTree_SelectedItemChanged);

            SelectColorImage.MouseLeftButtonDown += new MouseButtonEventHandler(SelectColorImage_MouseLeftButtonDown);
            SelectColorBorder.MouseLeftButtonDown += new MouseButtonEventHandler(SelectColorBorder_MouseLeftButtonDown);

            DataContext = this;

            CheckForUpdates();

            /*TimeInterval t0 = new TimeInterval("2h 2h 3h 3h 1d 2d 2d 2h 2h 2h 2h 2h 8d 1h 1d 1h 1h 1d 3d 2d 1w 1w 2d");

            TimeInterval t = new TimeInterval("3d 5m");
            TimeInterval t2 = new TimeInterval("1h");
            TimeInterval t3 = t + t2;*/
        }

        private void CheckForUpdates()
        {
            UpdatesChecker checker = new UpdatesChecker(this);
            checker.Start();
        }

        void Styles_StyleChanged(object sender, StyleChangedArgs e)
        {
            if (__ActiveStyle == e.Style)
                UpdateFontSettingsForStyle(__ActiveStyle);
        }

        private void OpenRecentFile()
        {
            for (int i = 0; i < this.recentMenuItem.Items.Count; i++)
            {
                MenuItem item = (MenuItem)recentMenuItem.Items[i];
                string fileName = item.CommandParameter as string;

                if (fileName != null && System.IO.File.Exists(fileName))
                {
                    OpenFile(fileName);
                    return;
                }
            }
        }

        private void AddCommandBindings()
        {

            CommandBindings.Add(new CommandBinding(OutlinerCommands.InsertAfterCurrent, new ExecutedRoutedEventHandler(InsertAfterCurrent)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.InsertBeforeCurrent, new ExecutedRoutedEventHandler(InsertBeforeCurrent)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.InsertBeforeCurrent, new KeyGesture(Key.Enter, ModifierKeys.Shift)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.InsertBeforeCurrent, new KeyGesture(Key.Insert)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.New, new ExecutedRoutedEventHandler(New)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.DeleteCurrentRow, new ExecutedRoutedEventHandler(DeleteCurrentRow)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.IncIndent, new ExecutedRoutedEventHandler(IncrementIndent)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.DecIndent, new ExecutedRoutedEventHandler(DecrementIndent)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.Save, new ExecutedRoutedEventHandler(Save_Cmd)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.SaveAs, new ExecutedRoutedEventHandler(SaveAs_Cmd)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.Export, new ExecutedRoutedEventHandler(Export)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.Open, new ExecutedRoutedEventHandler(Open)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.OpenRecentFile, new ExecutedRoutedEventHandler(OpenRecent)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.ToggleShowCheckboxes, new ExecutedRoutedEventHandler(ToggleShowCheckboxes)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.ToggleAutoStyles, new ExecutedRoutedEventHandler(ToggleAutoStyles)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.ToggleShowInspectors, new ExecutedRoutedEventHandler(ToggleShowInspectors)));
            InputBindings.Add(new InputBinding(OutlinerCommands.ToggleShowInspectors, new KeyGesture(Key.H, ModifierKeys.Control)));
            InputBindings.Add(new InputBinding(OutlinerCommands.Hoist, new KeyGesture(Key.H, ModifierKeys.Alt)));
            InputBindings.Add(new InputBinding(OutlinerCommands.Unhoist, new KeyGesture(Key.G, ModifierKeys.Alt)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.Hoist, new ExecutedRoutedEventHandler(Hoist), new CanExecuteRoutedEventHandler(CanHoist)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.Unhoist, new ExecutedRoutedEventHandler(Unhoist), new CanExecuteRoutedEventHandler(CanUnhoist)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.UnhoistAll, new ExecutedRoutedEventHandler(UnhoistAll), new CanExecuteRoutedEventHandler(CanUnhoist)));

            InputBindings.Add(new InputBinding(OutlinerCommands.CheckUncheck, new KeyGesture(Key.Space)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.CheckUncheck, new ExecutedRoutedEventHandler(CheckUncheck)));

            CommandBindings.Add(new CommandBinding(OutlinerCommands.Exit, new ExecutedRoutedEventHandler(Exit)));
            CommandBindings.Add(new CommandBinding(About, new ExecutedRoutedEventHandler(About_Cmd)));
            CommandBindings.Add(new CommandBinding(TourAndScreenshots, new ExecutedRoutedEventHandler(TourAndScreenshots_Cmd)));
            CommandBindings.Add(new CommandBinding(Support, new ExecutedRoutedEventHandler(Support_Cmd)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.Save, new KeyGesture(Key.S, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.New, new KeyGesture(Key.N, ModifierKeys.Control)));

            InputBindings.Add(new KeyBinding(OutlinerCommands.OpenFindWindow, new KeyGesture(Key.F, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.OpenFindWindow, new ExecutedRoutedEventHandler(OpenFindWindow)));

            CommandBindings.Add(new CommandBinding(EditingCommands.ToggleBold, new ExecutedRoutedEventHandler(ToggleBold)));
            CommandBindings.Add(new CommandBinding(EditingCommands.ToggleItalic, new ExecutedRoutedEventHandler(ToggleItalic)));
            CommandBindings.Add(new CommandBinding(EditingCommands.ToggleUnderline, new ExecutedRoutedEventHandler(ToggleUnderline)));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, new ExecutedRoutedEventHandler(Undo_Executed), new CanExecuteRoutedEventHandler(Undo_CanExecute)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Redo, new ExecutedRoutedEventHandler(Redo_Executed), new CanExecuteRoutedEventHandler(Redo_CanExecute)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.Undo, new ExecutedRoutedEventHandler(Undo_Executed), new CanExecuteRoutedEventHandler(Undo_CanExecute)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.Redo, new ExecutedRoutedEventHandler(Redo_Executed), new CanExecuteRoutedEventHandler(Redo_CanExecute)));

            CommandBindings.Add(new CommandBinding(OutlinerCommands.NewColumn, new ExecutedRoutedEventHandler(NewColumn), new CanExecuteRoutedEventHandler(CanExecute_Always)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.RemoveColumn, new ExecutedRoutedEventHandler(RemoveColumn), new CanExecuteRoutedEventHandler(CanExecute_RemoveColumn)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.ChangeColumnName, new ExecutedRoutedEventHandler(ChangeColumnName), new CanExecuteRoutedEventHandler(CanExecute_Always)));

            InputBindings.Add(new KeyBinding(OutlinerCommands.InsertNote, new KeyGesture(Key.I, ModifierKeys.Control | ModifierKeys.Shift)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.InsertURL, new KeyGesture(Key.U, ModifierKeys.Control | ModifierKeys.Shift)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.AttachFile, new KeyGesture(Key.A, ModifierKeys.Control | ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.InsertNote, new ExecutedRoutedEventHandler(InsertNote), new CanExecuteRoutedEventHandler(InsertNote_CanExecute)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.InsertURL, new ExecutedRoutedEventHandler(InsertURL), new CanExecuteRoutedEventHandler(CanExecute_Always)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.AttachFile, new ExecutedRoutedEventHandler(AttachFile), new CanExecuteRoutedEventHandler(CanExecute_Always)));

            InputBindings.Add(new KeyBinding(EditingCommands.ToggleBold, new KeyGesture(Key.B, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(EditingCommands.ToggleItalic, new KeyGesture(Key.I, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(EditingCommands.ToggleUnderline, new KeyGesture(Key.U, ModifierKeys.Control)));

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, new ExecutedRoutedEventHandler(CopyToClipboard_Command)));
            InputBindings.Add(new InputBinding(ApplicationCommands.Copy, new KeyGesture(Key.C, ModifierKeys.Control)));
            InputBindings.Add(new InputBinding(ApplicationCommands.Copy, new KeyGesture(Key.Insert, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, new ExecutedRoutedEventHandler(PasteFromClipboard_Command), new CanExecuteRoutedEventHandler(PasterFromClipboard_CanExecute)));
            InputBindings.Add(new InputBinding(ApplicationCommands.Paste, new KeyGesture(Key.V, ModifierKeys.Control)));
            InputBindings.Add(new InputBinding(ApplicationCommands.Paste, new KeyGesture(Key.Insert, ModifierKeys.Shift)));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, new ExecutedRoutedEventHandler(CutToClipboard_Command)));
            InputBindings.Add(new InputBinding(ApplicationCommands.Cut, new KeyGesture(Key.X, ModifierKeys.Control)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.ApplyLevelStyle, new ExecutedRoutedEventHandler(ApplyStyleLevelToCurrentRow)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.MoveRowUp, new KeyGesture(Key.Up, ModifierKeys.Control)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.MoveRowDown, new KeyGesture(Key.Down, ModifierKeys.Control)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.MoveRowUp, new ExecutedRoutedEventHandler(MoveRowUp)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.Settings, new ExecutedRoutedEventHandler(SettingsCommand)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.MoveRowDown, new ExecutedRoutedEventHandler(MoveRowDown)));

            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.CollapseAll, new KeyGesture(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.CollapseAll, new KeyGesture(Key.Subtract, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.CollapseAll, new ExecutedRoutedEventHandler(CollapseAll)));

            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAll, new KeyGesture(Key.Multiply, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.ExpandAll, new ExecutedRoutedEventHandler(ExpandAll)));

            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel1, new KeyGesture(Key.D1, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel1, new KeyGesture(Key.NumPad1, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.ExpandAllLevel1, new ExecutedRoutedEventHandler(ExpandLevel1)));

            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel2, new KeyGesture(Key.D2, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel2, new KeyGesture(Key.NumPad2, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.ExpandAllLevel2, new ExecutedRoutedEventHandler(ExpandLevel2)));

            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel3, new KeyGesture(Key.D3, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel3, new KeyGesture(Key.NumPad3, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.ExpandAllLevel3, new ExecutedRoutedEventHandler(ExpandLevel3)));

            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel4, new KeyGesture(Key.D4, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel4, new KeyGesture(Key.NumPad4, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.ExpandAllLevel4, new ExecutedRoutedEventHandler(ExpandLevel4)));

            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel5, new KeyGesture(Key.D5, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.InputBindings.Add(new KeyBinding(OutlinerCommands.ExpandAllLevel5, new KeyGesture(Key.NumPad5, ModifierKeys.Control | ModifierKeys.Shift)));
            OutlinerTree.CommandBindings.Add(new CommandBinding(OutlinerCommands.ExpandAllLevel5, new ExecutedRoutedEventHandler(ExpandLevel5)));

            CommandBindings.Add(new CommandBinding(OutlinerCommands.Print, new ExecutedRoutedEventHandler(Print)));
        }

        private void CollapseAll(object sender, ExecutedRoutedEventArgs args)
        {
            OutlinerDocument.WalkRecursively(Document.FakeRootNode,
                delegate(OutlinerNote note, out bool shouldWalkSubnotes, out bool shouldContinue)
                {
                    note.IsExpanded = false;
                    shouldWalkSubnotes = true;
                    shouldContinue = true;
                });
        }

        private void ExpandAll(object sender, ExecutedRoutedEventArgs args)
        {
            OutlinerDocument.WalkRecursively(Document.FakeRootNode,
                delegate(OutlinerNote note, out bool shouldWalkSubnotes, out bool shouldContinue)
                {
                    note.IsExpanded = true;
                    shouldContinue = true;
                    shouldWalkSubnotes = true;
                });
        }

        private void ExpandLevel1(object sender, ExecutedRoutedEventArgs args)
        {
            ExpandTo(1);
        }

        private void ExpandLevel2(object sender, ExecutedRoutedEventArgs args)
        {
            ExpandTo(2);
        }

        private void ExpandLevel3(object sender, ExecutedRoutedEventArgs args)
        {
            ExpandTo(3);
        }

        private void ExpandLevel4(object sender, ExecutedRoutedEventArgs args)
        {
            ExpandTo(4);
        }

        private void ExpandLevel5(object sender, ExecutedRoutedEventArgs args)
        {
            ExpandTo(5);
        }

        public void ExpandTo(int level)
        {
            OutlinerDocument.WalkRecursively(Document.FakeRootNode,
                delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    if (note.Level <= level)
                        note.IsExpanded = true;
                    else
                        note.IsExpanded = false;

                    shouldContinue = true;
                    shouldWalkSubItems = true;
                });
        }

        private void CanExecute_Always(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
        }

        private void UpdateOutlinerTreeItemsSource()
        {
            UpdateOutlinerTreeItemsSource(Document, OutlinerTree);
        }

        private static void UpdateOutlinerTreeItemsSource(OutlinerDocument document, TreeListView treeListView)
        {
            if (!document.IsHoisted)
                treeListView.ItemsSource = document;
            else
                treeListView.ItemsSource = document.HostNode.SubNotes;
        }

        private bool FeatureAvailableOnlyInProVersion()
        {
            return true;
        }

        private void StartFontsPreload()
        {
            __PreloadFontFamiliesTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            __PreloadFontFamiliesTimer.Tick += new EventHandler(preloadFontFamilies_Tick);
            __PreloadFontFamiliesTimer.Start();
        }

        private void PreviewExecutedEvent(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Undo)
            {
                Undo_Executed(sender, e);
                e.Handled = true;
            }

            if (e.Command == ApplicationCommands.Redo)
            {
                Redo_Executed(sender, e);
                e.Handled = true;
            }
        }

        private void OnMoveToNextColumn(object sender, RoutedEventArgs e)
        {
            MoveToNextColumn();
        }

        private void MoveToNextColumn()
        {
            MoveToPrevColumn();
        }

        private void MoveToPrevColumn()
        {
            int viewColumnId = GetViewColumnId(__LastColumn);
            if (viewColumnId < __OutlinerTreeColumns.Count - 1)
                OutlinerTree.MakeActive((OutlinerNote)OutlinerTree.SelectedItem, GetColumnIdByView(viewColumnId + 1), false);
        }

        private void OnMoveToPrevColumn(object sender, RoutedEventArgs e)
        {
            int viewColumnId = GetViewColumnId(__LastColumn);
            if (viewColumnId > 0)
                OutlinerTree.MakeActive((OutlinerNote)OutlinerTree.SelectedItem, GetColumnIdByView(viewColumnId - 1), false);
        }

        private void OnUndoAction(object sender, RoutedEventArgs e)
        {
            UndoActionRoutedEventArgs args = e as UndoActionRoutedEventArgs;
            if (args.UndoAction == null)
            {
                Document.UndoManager.CanMerge = false;

                e.Handled = true;
                return;
            }

            FlowDocument document = ((RichTextBox)e.OriginalSource).Document;

            Document.UndoManager.PushUndoAction(
                new UndoMyEditAction(__SelectedEditColumn, IsInlineNoteFocused, document, args.UndoAction));
            e.Handled = true;
        }

        private void CheckUncheck(object sender, ExecutedRoutedEventArgs args)
        {
            OutlinerNote selectedItem = OutlinerTree.SelectedItem;
            if (selectedItem == null)
                return;

            var undoAction = new UndoCheck(selectedItem, !(selectedItem.IsChecked == true), true);
            Document.UndoManager.PushUndoAction(undoAction);

            selectedItem.IsChecked = selectedItem.IsChecked == true ? false : true;

            args.Handled = true;
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (!Document.UndoManager.CanUndo)
                return;

            Document.UndoManager.Undo(Document, OutlinerTree);
        }

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = Document.UndoManager.CanUndo;
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (!Document.UndoManager.CanRedo)
                return;

            Document.UndoManager.Redo(Document, OutlinerTree);
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = Document.UndoManager.CanRedo;
        }

        private void SettingsCommand(object sender, RoutedEventArgs e)
        {
            wnd_Settings settings = new wnd_Settings();
            settings.Owner = this;
            if (settings.ShowDialog() == true)
            {
                if (settings.cbFontSizes.SelectedItem != null)
                {
                    double fontSize = 12;
                    bool parseOK = double.TryParse(((ComboBoxItem)settings.cbFontSizes.SelectedItem).Content.ToString(), out fontSize);

                    if (parseOK)
                        UVOutliner.Settings.DefaultFontSize = fontSize;
                }

                if (settings.cbFonts.SelectedItem != null)
                    UVOutliner.Settings.DefaultFontFamily = (FontFamily)(settings.cbFonts.SelectedItem);

                bool oldFontSelectionPreview = UVOutliner.Settings.FontSelectionPreview;
                UVOutliner.Settings.FontSelectionPreview = (settings.cbPreviewFonts.IsChecked == true);
                UVOutliner.Settings.AutoOpenLastSavedFile = (settings.OpenLast.IsChecked == true);
                UVOutliner.Settings.SaveSettings();

                if (__SelectedEdit != null)
                {
                    TextSettingsSetFontSize(UVOutliner.Settings.DefaultFontSize);
                    TextSettingsSetFontFamily(UVOutliner.Settings.DefaultFontFamily);
                }

                if (oldFontSelectionPreview == false && UVOutliner.Settings.FontSelectionPreview == true)
                    StartFontsPreload();

                if (oldFontSelectionPreview != UVOutliner.Settings.FontSelectionPreview)
                    ReinitFontSizeCombobox();
            }
        }

        private void ReinitFontSizeCombobox()
        {
            cbFontSizes.Items.Clear();
            cbFonts.ItemsSource = null;
            InitFontComboBox();
            if (__SelectedEdit == null)
                UpdateFontSettingsForSelectedItem();
            else
                UpdateFontSettings(__SelectedEdit.Selection);
        }

        private void MoveRowUp(object sender, RoutedEventArgs e)
        {
            OutlinerNote selectedItem = OutlinerTree.SelectedItem;
            if (selectedItem == null)
                return;

            if (DocumentHelpers.CanMoveNodeUp(selectedItem, OutlinerTree))
            {
                int activeColumn = DocumentHelpers.GetFocusedColumnIdx(OutlinerTree, selectedItem);
                DocumentHelpers.MoveNodeUp(selectedItem, OutlinerTree, activeColumn, IsInlineNoteFocused);
                Document.UndoManager.PushUndoAction(new UndoMoveUp(selectedItem, activeColumn, IsInlineNoteFocused));
            }

        }

        public void MoveRowDown(object sender, RoutedEventArgs e)
        {
            OutlinerNote selectedItem = OutlinerTree.SelectedItem;
            if (selectedItem == null)
                return;

            if (DocumentHelpers.CanMoveNodeDown(selectedItem, OutlinerTree))
            {
                int activeColumn = DocumentHelpers.GetFocusedColumnIdx(OutlinerTree, selectedItem);
                DocumentHelpers.MoveNodeDown(selectedItem, OutlinerTree, activeColumn, IsInlineNoteFocused);
                Document.UndoManager.PushUndoAction(new UndoMoveDown(selectedItem, activeColumn, IsInlineNoteFocused));
            }
        }

        protected virtual void OnMoveToPrevLine(object sender, RoutedEventArgs args)
        {
            MyEdit editor = null;
            OutlinerNote currentNote = OutlinerTree.SelectedItem;
            if (currentNote != null && __LastColumn == 0)
            {
                if (currentNote.HasInlineNote && IsInlineNoteFocused)
                {
                    Document.FocusEditAfterTemplateChange = true;

                    // Inline note should be deleted before the focus moves to the main text, so that template will have time to change
                    RemoveInlineNoteIfEmpty(currentNote);

                    if (!currentNote.HasInlineNote)
                        return;
                    else
                    {
                        Document.FocusEditAfterTemplateChange = false;
                        TreeListViewItem itemContainer = ViewHelpers.GetContainerForItem(OutlinerTree, currentNote);
                        editor = itemContainer.GetEditor(GetViewColumnId(__LastColumn), __LastColumn, false);

                        Keyboard.Focus(editor);
                        editor.MoveCaretToLastLine((args as MoveToLiveEventArgs).PrevLineRect);
                        return;
                    }
                }
            }

            TreeListViewItem prevItem = OutlinerTree.FindBeforeSelected<TreeListViewItem>(OutlinerTree);
            if (prevItem == null)
                return;

            OutlinerNote prevNote = prevItem.ParentItemsControl.ItemContainerGenerator.ItemFromContainer(prevItem) as OutlinerNote;

            if (prevNote == null || prevNote == currentNote)
                return;

            prevItem.Focus();

            if (prevNote.HasInlineNote)
                editor = prevItem.GetEditor(GetViewColumnId(__LastColumn), __LastColumn, true) as MyEdit;
            else
                editor = prevItem.GetEditor(GetViewColumnId(__LastColumn), __LastColumn, false) as MyEdit;

            if (editor != null)
            {
                Keyboard.Focus(editor);
                editor.MoveCaretToLastLine((args as MoveToLiveEventArgs).PrevLineRect);
            }

        }

        protected virtual void OnMoveToNextLine(object sender, RoutedEventArgs args)
        {
            OutlinerNote note = OutlinerTree.SelectedItem;
            if (note == null)
                return;

            if (note.HasInlineNote && !IsInlineNoteFocused)
            {
                var container = ViewHelpers.GetContainerForItem(OutlinerTree, note);
                MyEdit editor = container.GetEditor(GetViewColumnId(__LastColumn), __LastColumn, true) as MyEdit;
                if (editor != null)
                {
                    Keyboard.Focus(editor);
                    editor.MoveCaretToFirstLine((args as MoveToLiveEventArgs).PrevLineRect);
                }
            }
            else
            {
                OutlinerTree.FoundSelected = false;
                TreeListViewItem item = OutlinerTree.FindNextAfterSelected<TreeListViewItem>(OutlinerTree);
                if (item == null)
                    return;

                item.Focus();
                MyEdit editor = item.GetEditor(GetViewColumnId(__LastColumn), __LastColumn, false) as MyEdit;
                if (editor != null)
                {
                    Keyboard.Focus(editor);
                    editor.MoveCaretToFirstLine((args as MoveToLiveEventArgs).PrevLineRect);
                }
            }
        }

        void ApplyStyleLevelToCurrentRow(object sender, ExecutedRoutedEventArgs e)
        {
            OutlinerNote currentNote = OutlinerTree.SelectedItem;

            if (currentNote == null)
                return;

            DocumentHelpers.ApplyLevelStyle(currentNote, __SelectedEdit);
        }

        void CutToClipboard_Command(object sender, ExecutedRoutedEventArgs e)
        {
            OutlinerNote currentNote = OutlinerTree.SelectedItem;

            if (currentNote == null)
                return;

            OpenSave.CopyToClipboard(currentNote);
            DeleteOutlinerNote(currentNote);
        }

        void CopyToClipboard_Command(object sender, ExecutedRoutedEventArgs e)
        {
            if (OutlinerTree.SelectedItem == null)
                return;

            OpenSave.CopyToClipboard((OutlinerNote)OutlinerTree.SelectedItem);
        }

        void PasterFromClipboard_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = Clipboard.ContainsData("uvoutlinerdata") || Clipboard.ContainsData(DataFormats.Text);

        }

        void PasteFromClipboard_Command(object sender, ExecutedRoutedEventArgs e)
        {
            if (OutlinerTree.SelectedItem == null)
                return;

            OutlinerNote note = OpenSave.PasteFromClipboard((OutlinerNote)OutlinerTree.SelectedItem);
            if (note != null)
                OutlinerTree.MakeActive(note, -1, false);
        }

        void SelectColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectColor();
        }

        void SelectColorImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectColor();
        }

        public ObservableCollection<LevelStyle> Styles
        {
            get { return null; }
        }

        private void SelectColor()
        {
            if (__ActiveStyle != null)
            {
                SolidColorBrush newBrush = SelectColor(__ActiveStyle);
                if (newBrush != null)
                    UpdateStyle(StylePropertyType.FontColor, newBrush);

                UpdateFontSettingsForStyle(__ActiveStyle);
            }
            else
            {
                if (__SelectedEdit != null)
                    SelectColor(__SelectedEdit, __SelectedEdit.Selection);
                else
                {
                    TextRange range = GetDefaultTextRangeForSelectedRow();
                    if (range == null)
                        return;

                    SolidColorBrush newBrush = SelectColor(null, range);
                    if (newBrush != null)
                        UpdateStyle(StylePropertyType.FontColor, newBrush);

                    UpdateFontSettings(range);
                }
            }
        }

        private SolidColorBrush SelectColor(BaseStyle activeStyle)
        {
            SolidColorBrush currentBrush = activeStyle.Foreground;
            if (currentBrush == null)
                currentBrush = DefaultForegroundBrush;

            SolidColorBrush newBrush = null;

            ColorPickerDialog colorPicker = new ColorPickerDialog();
            colorPicker.Owner = this;

            colorPicker.cPicker.SelectedColor = currentBrush.Color;
            if (colorPicker.ShowDialog() == true)
                newBrush = new SolidColorBrush(colorPicker.cPicker.SelectedColor);

            return newBrush;
        }

        public void ApplyUndoAwarePropertyValue(MyEdit edit, TextRange range,
            DependencyProperty property, object value)
        {
            edit.ApplyUndoAwarePropertyValue(range, property, value);
        }

        private SolidColorBrush SelectColor(MyEdit edit, TextRange range)
        {
            SolidColorBrush currentBrush = Brushes.Black;
            SolidColorBrush newBrush = null;
            if (range.GetPropertyValue(ForegroundProperty) != DependencyProperty.UnsetValue)
            {
                currentBrush = (SolidColorBrush)range.GetPropertyValue(ForegroundProperty);
            }

            ColorPickerDialog colorPicker = new ColorPickerDialog();
            colorPicker.Owner = this;

            colorPicker.cPicker.SelectedColor = currentBrush.Color;
            if (colorPicker.ShowDialog() == true)
            {
                newBrush = new SolidColorBrush(colorPicker.cPicker.SelectedColor);
                if (edit != null)
                {
                    edit.ApplyUndoAwarePropertyValue(range, ForegroundProperty, newBrush);
                }
                else
                {

                    ApplyUndoEnabledPropertyValue(OutlinerTree.SelectedItem, ForegroundProperty, newBrush);
                }
            }

            UpdateFontSettings(range);
            return newBrush;
        }

        private void ApplyUndoEnabledPropertyValue(OutlinerNote note, DependencyProperty property, object value)
        {
            for (int i = 0; i < note.Document.ColumnDefinitions.Count; i++)
            {
                if (note.Columns[i].DataType == ColumnDataType.RichText)
                {
                    FlowDocument document = (FlowDocument)note.Columns[i].ColumnData;
                    StoreRowFormatting(note, i);

                    TextRange range = new TextRange(document.ContentStart, document.ContentEnd);
                    range.ApplyPropertyValue(property, value);
                }
            }
        }

        private void StoreRowFormatting(OutlinerNote note, int columnId)
        {
            if (note == null)
                return;

            UndoFlowDocumentFormatting undo = new UndoFlowDocumentFormatting(note, columnId, IsInlineNoteFocused, OutlinerTree.SelectedItem == note);
            Document.UndoManager.PushUndoAction(undo);
            return;
        }

        void OpenFindWindow(object sender, ExecutedRoutedEventArgs e)
        {
            if (Document.ShowInspectors == false)
            {
                Document.ShowInspectors = true;
                UpdateDocumentInspectors();
            }

            Find.IsExpanded = true;
            Keyboard.Focus(FindString);
            FindString.SelectAll();
        }

        void ToggleBold(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleBold();
        }

        void ToggleItalic(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleItalic();
        }

        void ToggleUnderline(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleUnderlined();
        }

        void OutlinerTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (__SelectedEdit == null)
                UpdateFontSettingsForSelectedItem();

            StylesList.SelectedValue = CurrentRowStyle;
        }

        private void UpdateFontSettingsForSelectedItem()
        {
            OutlinerNote note = OutlinerTree.SelectedItem;
            if (note == null)
                return;

            TextRange range = new TextRange(note.DefaultRichTextDocument.ContentStart, note.DefaultRichTextDocument.ContentEnd);
            UpdateFontSettings(range);
        }

        int __LastPreloadedFont = 0;
        void preloadFontFamilies_Tick(object sender, EventArgs e)
        {
            __LastPreloadedFont++;
            if (__LastPreloadedFont >= __FontFamilies.Count)
            {
                __PreloadFontFamiliesTimer.IsEnabled = false;
                return;
            }

            FontPreloader.FontFamily = __FontFamilies[__LastPreloadedFont];
        }

        public void OnEditingFinished(object sender, RoutedEventArgs e)
        {
            OutlinerNote note = (OutlinerNote)OutlinerTree.SelectedItem;
            note.UpdateIsEmpty();
            RemoveInlineNoteIfEmpty(note);
        }

        public void OnEditGotFocus(object sender, RoutedEventArgs e)
        {
            UnsetEditorHandlers();

            __SelectedEdit = e.OriginalSource as MyEdit;

            if (__SelectedEdit == null)
                return;

            __SelectedEditColumn = GetSelectedEditColumn();
            if (__SelectedEditColumn != -1)
                __LastColumn = __SelectedEditColumn;

            SetEditorHandlers();
            Editor_SelectionChanged(sender, e);
            e.Handled = true;
        }

        private int GetSelectedEditColumn()
        {
            OutlinerNote note = OutlinerTree.SelectedItem;
            ItemContainerGenerator generator = OutlinerTree.ItemContainerGeneratorFor(note);
            if (note == null && generator == null)
                return -1;

            TreeListViewItem item = generator.ContainerFromItem(note) as TreeListViewItem;
            if (item == null)
                return -1;

            for (int i = 0; i < __OutlinerTreeColumns.Count; i++)
            {
                int realColumnId = -1;
                for (int k = 0; k < Document.ColumnDefinitions.Count; k++)
                    if (__OutlinerTreeColumns[i] == Document.ColumnDefinitions[k].GridViewColumn)
                    {
                        realColumnId = k;
                        break;
                    }

                if (realColumnId != -1)
                {
                    MyEdit edit = item.GetEditor(i, realColumnId, IsInlineNoteFocused);
                    if (edit != null && edit == __SelectedEdit)
                        return realColumnId;
                }
            }

            return -1;
        }

        private void UnsetEditorHandlers()
        {
            if (__SelectedEdit != null)
            {
                __SelectedEdit.SelectionChanged -= Editor_SelectionChanged;
                __SelectedEdit.TextChanged -= Editor_TextChanged;
                __SelectedEdit.EditorCommandIssued -= Editor_CommandIssued;

                __SelectedEdit = null;
            }
        }

        private void SetEditorHandlers()
        {
            __SelectedEdit.SelectionChanged += new RoutedEventHandler(Editor_SelectionChanged);
            __SelectedEdit.TextChanged += new TextChangedEventHandler(Editor_TextChanged);
            __SelectedEdit.EditorCommandIssued += new EventHandler(Editor_CommandIssued);
        }

        void Editor_CommandIssued(object sender, EventArgs e)
        {
            Editor_SelectionChanged(sender, new RoutedEventArgs());
        }

        void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            Editor_SelectionChanged(sender, new RoutedEventArgs());
        }

        void Editor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (__SelectedEdit == null)
                return;

            UpdateFontSettings(__SelectedEdit.Selection);
        }

        private void UpdateFontSettings(TextRange range)
        {
            double fontSize = -1;
            if (range.GetPropertyValue(TextBlock.FontSizeProperty) != DependencyProperty.UnsetValue)
                fontSize = (double)range.GetPropertyValue(TextBlock.FontSizeProperty);

            FontFamily fontFamily = range.GetPropertyValue(Run.FontFamilyProperty) as FontFamily;

            __FontParametersChanging = true;
            try
            {
                TextSettingsSetFontSize(fontSize);
                TextSettingsSetFontFamily(fontFamily);
            }
            finally
            {
                __FontParametersChanging = false;
            }

            SolidColorBrush fontBrush = Brushes.Black;
            if (range.GetPropertyValue(ForegroundProperty) != DependencyProperty.UnsetValue)
                fontBrush = (SolidColorBrush)range.GetPropertyValue(ForegroundProperty);

            SelectColorBorder.Background = fontBrush;

            FontBold.IsChecked = TextRangeHelpers.IsBold(range);
            FontItalic.IsChecked = TextRangeHelpers.IsItalic(range);
            FontUnderline.IsChecked = TextRangeHelpers.GetTextDecorationOnSelection(range, TextDecorationLocation.Underline);
            FontStrikethrough.IsChecked = TextRangeHelpers.GetTextDecorationOnSelection(range, TextDecorationLocation.Strikethrough);
        }

        private void TextSettingsSetFontFamily(FontFamily fontFamily)
        {
            if (fontFamily == null)
            {
                __FontFamilies[0] = null;
                cbFonts.SelectedIndex = 0;
            }
            else
            {

                bool fontFamilyFound = false;
                for (int i = 0; i < __FontFamilies.Count; i++)
                {
                    if (__FontFamilies[i] == null)
                        continue;

                    if (__FontFamilies[i].Source == fontFamily.Source)
                    {
                        fontFamilyFound = true;
                        cbFonts.SelectedIndex = i;
                        break;
                    }
                }

                if (fontFamilyFound == false)
                {
                    __FontFamilies[0] = fontFamily;
                    cbFonts.SelectedIndex = 0;
                }
            }
        }

        private void TextSettingsSetFontSize(double fontSize)
        {
            if (fontSize == -1)
            {
                ((ComboBoxItem)cbFontSizes.Items[0]).Content = "";
                ((ComboBoxItem)cbFontSizes.Items[0]).FontSize = 10;
                cbFontSizes.SelectedIndex = 0;
            }
            else
            {
                bool fontSizeFound = false;
                for (int i = 0; i < cbFontSizes.Items.Count; i++)
                {
                    ComboBoxItem cbItem = (ComboBoxItem)cbFontSizes.Items[i];
                    if (cbItem.Content == null || cbItem.Content.ToString() == "")
                        continue;

                    if (double.Parse(cbItem.Content.ToString()) == fontSize)
                    {
                        fontSizeFound = true;
                        cbFontSizes.SelectedIndex = i;
                        break;
                    }
                }

                if (fontSizeFound == false)
                {
                    ((ComboBoxItem)cbFontSizes.Items[0]).Content = fontSize.ToString();
                    ((ComboBoxItem)cbFontSizes.Items[0]).FontSize = fontSize;
                    cbFontSizes.SelectedIndex = 0;
                }
            }
        }

        public void OnEditLostFocus(object sender, RoutedEventArgs e)
        {
            UnsetEditorHandlers();
            UpdateFontSettingsForSelectedItem();
            e.Handled = true;
        }

        ObservableCollection<FontFamily> __FontFamilies;
        private void InitFontComboBox()
        {
            __FontFamilies = new ObservableCollection<FontFamily>();
            __FontFamilies.Add(null);
            foreach (FontFamily family in Fonts.SystemFontFamilies)
                __FontFamilies.Add(family);

            cbFonts.ItemsSource = __FontFamilies;

            for (double i = 5; i < 73; i += 1)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = i.ToString();
                if (UVOutliner.Settings.FontSelectionPreview)
                    cbi.FontSize = i;
                else
                    cbi.FontSize = 12;

                cbFontSizes.Items.Add(cbi);
            }
        }


        private struct RecentItem
        {
            public string fileName;
            public Int64 lastUpdated;
        }

        private void ReloadRecentItems()
        {
            recentMenuItem.Items.Clear();

            RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\Recent", false);
            if (outlinerKey == null)
                return;

            string[] files = outlinerKey.GetValueNames();
            List<RecentItem> recentItems = new List<RecentItem>();

            foreach (var file in files)
            {
                string recentData = outlinerKey.GetValue(file, "") as string;
                if (recentData != null && recentData != "")
                {
                    string[] date = recentData.Split(new char[] { ';' });
                    if (date.Length < 6)
                        continue;

                    RecentItem r = new RecentItem();
                    r.fileName = file;

                    try
                    {
                        r.lastUpdated = Int64.Parse(date[5]);
                    }
                    catch
                    {
                        continue;
                    }

                    recentItems.Add(r);
                }
            }

            recentItems.Sort(RecentItemsComparison);
            for (int i = 0; i < Math.Min(recentItems.Count, 10); i++)
            {
                if (recentItems[i].fileName == __Document.FileName)
                    continue;

                if (recentItems[i].fileName == OutlinerDocument.DefaultFileName)
                    continue;

                string fileName = recentItems[i].fileName;
                MenuItem mi = new MenuItem();
                mi.Header = fileName;
                mi.Command = OutlinerCommands.OpenRecentFile;
                mi.CommandParameter = recentItems[i].fileName;
                recentMenuItem.Items.Add(mi);

                if (!System.IO.File.Exists(fileName))
                    mi.IsEnabled = false;
            }

            if (recentMenuItem.Items.Count > 0)
                recentMenuItem.IsEnabled = true;
            else
                recentMenuItem.IsEnabled = false;
        }

        private static int RecentItemsComparison(RecentItem item1, RecentItem item2)
        {
            return item2.lastUpdated.CompareTo(item1.lastUpdated);
        }

        private void SetupColumns()
        {
            OutlinerDocument document = __Document;
            __OutlinerTreeColumns = (GridViewColumnCollection)this.FindResource("gvcc");

            while (__OutlinerTreeColumns.Count > 0)
                __OutlinerTreeColumns.RemoveAt(__OutlinerTreeColumns.Count - 1);

            for (int i = 0; i < document.ColumnDefinitions.Count; i++)
                AddColumnBinding(document.ColumnDefinitions[i], i);

            if (document.ColumnDefinitions.OrderOnInit != null)
            {
                string[] ids = document.ColumnDefinitions.OrderOnInit.Split(new char[] { ';' });
                for (int i = 0; i < ids.Length; i++)
                {
                    int newViewId = int.Parse(ids[i]);
                    int currentViewId = GetViewColumnId(i);
                    __OutlinerTreeColumns.Move(currentViewId, newViewId);
                }
                document.ColumnDefinitions.OrderOnInit = null;
            }

            OutlinerTree.HeaderVisible = (document.ColumnDefinitions.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
            AdjustColumnSizes();
        }

        private void AddColumnBinding(OutlinerColumnDefinition definition, int columnId)
        {
            GridViewColumn newColumn = new GridViewColumn();
            if (columnId == 0)
                newColumn.CellTemplate = (DataTemplate)FindResource("CellTemplate_Name");
            else
                newColumn.CellTemplate = ColumnHelpers.TemplateForColumn(this, columnId, definition.DataType);

            Binding bnd = new Binding("ColumnName");
            bnd.Source = definition;
            BindingOperations.SetBinding(newColumn, GridViewColumn.HeaderProperty, bnd);

            bnd = new Binding("Width");
            bnd.Source = definition;
            bnd.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(newColumn, GridViewColumn.WidthProperty, bnd);
            __OutlinerTreeColumns.Add(newColumn);

            definition.GridViewColumn = newColumn;
        }

        private void AddColumnToDocument(string columnName, ColumnDataType columnType)
        {
            int id = Document.ColumnDefinitions.Count;
            OutlinerColumnDefinition definition = new OutlinerColumnDefinition(columnName, columnType);
            Document.ColumnDefinitions.Add(definition);
            OutlinerDocument.WalkRecursively(Document.FakeRootNode,
                delegate(OutlinerNote note, out bool shouldWalkSubItems, out bool shouldContinue)
                {
                    note.CreateMissingColumns();
                    shouldContinue = true;
                    shouldWalkSubItems = true;
                });

            AddColumnBinding(definition, id);
            AdjustColumnSizes();
            OutlinerTree.HeaderVisible = (__OutlinerTreeColumns.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
        }

        void OutlinerTree_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustColumnSizes();
        }

        private void AdjustColumnSizes()
        {
            GridViewColumnCollection columns = __OutlinerTreeColumns;
            int viewMainColumnId = GetViewColumnId(0);
            double totalColumnsWidth = 0;
            if (columns.Count > 0)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (i == viewMainColumnId)
                        continue;

                    totalColumnsWidth += columns[i].ActualWidth;
                }
            }

            double newWidth = OutlinerTree.ActualWidth - 25 - totalColumnsWidth;
            if (newWidth < 150)
                newWidth = 150;
            if (newWidth > 0)
            {
                columns[viewMainColumnId].Width = newWidth;
            }
        }

        void Window1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (__Document.WasAltered)
            {
                MessageBoxResult shouldCloseResult =
                    MessageBox.Show("Do you want to save changes before exit?", "Changes are not saved",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question,
                        MessageBoxResult.Yes);
                if (shouldCloseResult == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (shouldCloseResult == MessageBoxResult.Yes)
                {
                    if (Save() == false)
                        e.Cancel = true;
                }
            }

            SaveWindowLocation(Document.FileName);
        }

        private void New(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(((app)Application.Current).ExecutableFile, "/noautoload");
        }

        void wnd_Closed(object sender, EventArgs e)
        {
            Dispatcher.ExitAllFrames();
        }

        public void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            wnd_Exception exceptionWindow = new wnd_Exception();
            exceptionWindow.ShowException(e.Exception.Message, e.Exception);
            e.Handled = true;
        }

        private void Export(object sender, RoutedEventArgs e)
        {
            wnd_Export exportWindow = new wnd_Export();
            exportWindow.SetMainWindow(this);
            exportWindow.Owner = this;
            exportWindow.Document = Document;
            exportWindow.ShowDialog();
        }

        private void Save_Cmd(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private bool Save()
        {

            if (__Document.IsDefaultFileName)
                return SaveAs();

            bool saveResult = OpenSave.SaveFile(this, __Document, __Document.FileName);
            if (saveResult == true)
                ShowDocumentSaved();

            SaveWindowLocation(__Document.FileName);
            UpdateTitle();
            ReloadRecentItems();
            return saveResult;
        }

        public void UpdateTitle()
        {
            string pro = "";
            string star = "";

            if (__Document.WasAltered)
                star = "*";

            if (__Document.IsDefaultFileName)
                Title = star + "UV Outliner" + pro;
            else
                Title = Path.GetFileName(__Document.FileName) + star + " - UV Outliner" + pro;
        }

        private void SaveAs_Cmd(object sender, RoutedEventArgs e)
        {
            SaveAs();
        }

        private bool SaveAs()
        {
            bool saveResult = false;
            SaveFileDialog sfd = new SaveFileDialog();
            if (!Document.IsDefaultFileName)
                sfd.FileName = System.IO.Path.GetFileName(Document.FileName);

            sfd.Filter = OpenSave.FileFilter;
            if (sfd.ShowDialog() == true)
            {
                saveResult = OpenSave.SaveFile(this, __Document, sfd.FileName);
                if (saveResult == true)
                    ShowDocumentSaved();

                SaveWindowLocation(sfd.FileName);
            }

            UpdateTitle();
            return saveResult;
        }

        private void ToggleShowCheckboxes(object sender, RoutedEventArgs e)
        {
            Document.WasAltered = true;
            Document.CheckboxesVisble = !Document.CheckboxesVisble;
        }

        private void ToggleAutoStyles(object sender, RoutedEventArgs e)
        {
            Document.WasAltered = true;
            Document.AutoStyles = !Document.AutoStyles;
        }

        private void ToggleShowInspectors(object sender, RoutedEventArgs e)
        {
            Document.WasAltered = true;
            Document.ShowInspectors = !Document.ShowInspectors;

            UpdateDocumentInspectors();
        }

        private void UpdateDocumentInspectors()
        {
            UpdateHoistTitle();

            double width = OutlinerTree.RenderSize.Width;
            if (Document.ShowInspectors == false && PropertiesPanelBorder.Visibility == Visibility.Visible)
            {
                double newWidth = this.Width - PropertiesPanelBorder.Width;
                PropertiesPanelBorder.Visibility = Visibility.Collapsed;
                if (WindowState != WindowState.Maximized)
                    this.Width = newWidth;
            }
            else
                if (Document.ShowInspectors == true && PropertiesPanelBorder.Visibility == Visibility.Collapsed)
                {
                    PropertiesPanelBorder.Visibility = Visibility.Visible;
                    if (WindowState != WindowState.Maximized)
                        this.Width += PropertiesPanelBorder.Width;
                }
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            if (!CheckForUnsavedChanges())
                return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = __Document.FileName;
            ofd.Filter = OpenSave.FileFilter;
            if (ofd.ShowDialog() == true)
            {
                OpenFile(ofd.FileName);
            }

            if (__Document.Count == 0)
                return;

            OutlinerTree.MakeActive(__Document[0], 0, false);
            ReloadRecentItems();
        }

        private void OpenRecent(object sender, ExecutedRoutedEventArgs e)
        {
            if (!CheckForUnsavedChanges())
                return;

            string fileName = e.Parameter as string;
            if (fileName == null || fileName == "")
                return;

            OpenFile(fileName);

            if (__Document.Count == 0)
                return;

            OutlinerTree.MakeActive(__Document[0], 0, false);
            ReloadRecentItems();
        }

        private bool CheckForUnsavedChanges()
        {
            SaveWindowLocation(__Document.FileName);
            if (__Document.WasAltered)
            {
                MessageBoxResult res;
                if (__Document.IsDefaultFileName)
                    res = MessageBox.Show("Do you wish to save changed?", "File was changed", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                else
                    res = MessageBox.Show(String.Format("Do you wish to save changed to file {0}?", __Document.FileName),
                        "File was changed", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (res == MessageBoxResult.Cancel)
                    return false;

                if (res == MessageBoxResult.Yes)
                {
                    OutlinerCommands.Save.Execute(null, null);
                }
            }
            return true;
        }

        private void OpenFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                MessageBox.Show(String.Format("File '{0}' cannot be found", fileName), "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OutlinerDocument newRnl = OpenSave.OpenFile(fileName);
            if (newRnl != null)
            {
                Document = newRnl;
                SetupColumns();
                UpdateOutlinerTreeItemsSource();
                UpdateTitle();

                TryLoadPosition(fileName);

                DoPropertyChanged("Document");
                UpdateDocumentInspectors();
            }
        }

        private void ShowDocumentSaved()
        {

        }

        void HideDocumentSavedWindow(object sender, EventArgs e)
        {

        }

        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
           OutlinerTree.MakeFirstEditorFocused();
        }

        private void IncrementIndent(object sender, RoutedEventArgs e)
        {
            OutlinerNote row = OutlinerTree.SelectedItem;
            if (row == null)
                return;

            if (DocumentHelpers.CanIncreaseIndent(row))
            {
                UndoIndent undo = new UndoIndent(row, IsInlineNoteFocused, IndentDirection.IncreaseIndent);
                DocumentHelpers.IncreaseIndent(row, OutlinerTree, true);
                Document.UndoManager.PushUndoAction(undo);
                GC.Collect();
            }
        }

        private void DecrementIndent(object sender, RoutedEventArgs e)
        {
            OutlinerNote selectedRow = OutlinerTree.SelectedItem;

            if (DocumentHelpers.CanDecreaseIndent(selectedRow))
            {
                UndoIndent undo = new UndoIndent(selectedRow, IsInlineNoteFocused, IndentDirection.DecreaseIndent);
                DocumentHelpers.DecreaseIndent(selectedRow, OutlinerTree, true);
                Document.UndoManager.PushUndoAction(undo);
                GC.Collect();
            }
        }

        private void DeleteCurrentRow(object sender, RoutedEventArgs e)
        {
            DeleteSelectedRow();
        }

        private void DeleteSelectedRow()
        {
            OutlinerNote row = OutlinerTree.SelectedItem;
            if (row == null)
                return;

            DeleteOutlinerNote(row);
        }

        private void DeleteOutlinerNote(OutlinerNote row)
        {

            throw new Exception("test");

            // When deleting an element in the subtree, the focus doesn't moves automatically to where we want
            // so we have to move it manually
            OutlinerNote parentToFocus = null;
            if (row.Parent.SubNotes.Count == 1)
                parentToFocus = row.Parent;

            UndoDeleteRow undoDeleteRow = new UndoDeleteRow(row);
            DocumentHelpers.DeleteRow(row, OutlinerTree);
            Document.UndoManager.PushUndoAction(undoDeleteRow);
            if (Document.RootNode.SubNotes.Count == 0)
            {
                OutlinerNote newNote = new OutlinerNote(Document.RootNode);
                Document.RootNode.SubNotes.Add(newNote);
                OutlinerTree.MakeActive(newNote, 0, false);
            }
            else
            {
                if (parentToFocus != null)
                {
                    OutlinerTree.MakeActive(parentToFocus, -1, false);
                }
            }
        }

        private void InsertBeforeCurrent(object sender, RoutedEventArgs e)
        {
            InsertBeforeCurrentNote();
        }

        private void InsertBeforeCurrentNote()
        {
            InsertNewRow(false, false);
        }

        private void InsertAfterCurrent(object sender, RoutedEventArgs e)
        {
            InsertNewRow(false, true);
        }

        private void InsertNewRow(bool isTemporary, bool insertAfterCurrent)
        {
            OutlinerNote currentRow = OutlinerTree.SelectedItem;
            if (currentRow == null)
                return;

            OutlinerNote parent;
            int indexToInsert;
            __LastColumn = 0;

            if (insertAfterCurrent == true)
            {
                if (currentRow.IsExpanded == true && currentRow.SubNotes.Count > 0)
                {
                    parent = currentRow;
                    indexToInsert = 0;
                }
                else
                {
                    parent = currentRow.Parent;
                    indexToInsert = parent.SubNotes.IndexOf(currentRow) + 1;
                }
            }
            else
            {
                parent = currentRow.Parent;
                indexToInsert = parent.SubNotes.IndexOf(currentRow);
            }

            OutlinerNote newNote = new OutlinerNote(parent);
            parent.SubNotes.Insert(indexToInsert, newNote);
            newNote.Temporary = isTemporary;

            int columnIdx = 0;

            OutlinerTree.MakeActive(newNote, columnIdx, false, new EventHandler(DocumentHelpers.ApplyStyleAfterMakeActive));
            Document.UndoManager.PushUndoAction(new UndoInsertRow(newNote, columnIdx));
            newNote.UpdateParentCheckboxes();
        }


        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        private void Border_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        private void Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = false;
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void About_Cmd(object sender, RoutedEventArgs e)
        {
            try
            {
                wnd_About about = new wnd_About();
                about.Owner = this;
                about.ShowDialog();
            }
            finally
            {

            }
        }

        private void TourAndScreenshots_Cmd(object sender, RoutedEventArgs e)
        {
            UrlHelpers.OpenInBrowser("http://uvoutliner.com/tour/");
        }

        private void Support_Cmd(object sender, RoutedEventArgs e)
        {
            UrlHelpers.OpenInBrowser("http://uvoutliner.com/support/");
        }

        RegistryKey registryKey = Registry.CurrentUser;
        private void SaveWindowLocation(string fileName)
        {
            string key = fileName;
            if (key == null)
                key = "default";

            RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\Recent", true);
            if (outlinerKey == null)
            {
                outlinerKey = registryKey.CreateSubKey("Software\\UVOutliner\\Recent", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (outlinerKey == null)
                    return;
            }

            double inspectorsWidth = 0;
            if (Document.ShowInspectors)
            {
                inspectorsWidth = PropertiesPanelBorder.Width;
                if (Width - inspectorsWidth < 0)
                    inspectorsWidth = 0;
            }

            string res = string.Format("{0};{1};{2};{3};{4};{5}", Left, Top, Width - inspectorsWidth, Height, (int)WindowState, DateTime.Now.Ticks);
            outlinerKey.SetValue(fileName, res, RegistryValueKind.String);
        }

        void TryLoadPosition(string fileName)
        {
            bool oldShowInspectors = __Document.ShowInspectors;
            __Document.ShowInspectors = false;
            UpdateDocumentInspectors();
            try
            {
                RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\Recent", false);
                if (outlinerKey == null)
                {
                    __Document.ShowInspectors = oldShowInspectors;
                    UpdateDocumentInspectors();
                    return;
                }

                string key = fileName;
                if (key == null)
                    key = "default";

                string position = outlinerKey.GetValue(key, "") as string;
                if (position != null && position != "")
                {
                    string[] pos = position.Split(new char[] { ';' });
                    double left = double.Parse(pos[0]);
                    double top = double.Parse(pos[1]);
                    double width = double.Parse(pos[2]);
                    double height = double.Parse(pos[3]);
                    WindowState state = (WindowState)int.Parse(pos[4]);

                    Left = left;
                    Top = top;
                    Width = width;
                    Height = height;

                    WindowState = state;
                    WindowStartupLocation = WindowStartupLocation.Manual;
                }
            }
            catch
            {

            }
            __Document.ShowInspectors = oldShowInspectors;
            UpdateDocumentInspectors();
        }

        public OutlinerDocument Document
        {
            get { return __Document; }
            set
            {

                __Document.Styles.StyleChanged -= Styles_StyleChanged;
                __Document = value;
                __Document.Styles.StyleChanged += new EventHandler<StyleChangedArgs>(Styles_StyleChanged);
            }
        }

        private void FontSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            Document.UndoManager.StartGroup();
            try
            {
                if (__FontParametersChanging)
                    return;

                if (cbFontSizes.SelectedItem == null)
                    return;

                double fontSize = 0;
                try
                {
                    fontSize = Double.Parse(((ComboBoxItem)cbFontSizes.SelectedItem).Content.ToString());
                }
                catch
                {
                    return;
                }

                if (__ActiveStyle == null)
                {
                    if (__SelectedEdit == null)
                    {
                        ApplyUndoEnabledPropertyValue(OutlinerTree.SelectedItem, Run.FontSizeProperty, fontSize);
                    }
                    else
                    {
                        __SelectedEdit.ApplyUndoAwarePropertyValue(__SelectedEdit.Selection, Run.FontSizeProperty, fontSize);
                    }
                }

                // Apply to style only when whole row is selected
                if (__SelectedEdit == null || EditorAllTextSelected())
                    UpdateStyle(StylePropertyType.FontSize, fontSize);
            }
            finally
            {
                Document.UndoManager.EndGroup();
            }
        }

        private bool EditorAllTextSelected()
        {
            // 
            return false;
        }

        private void Fonts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (__FontParametersChanging)
                return;

            Document.UndoManager.StartGroup();

            try
            {
                FontFamily selectedFamily = (FontFamily)cbFonts.SelectedItem;
                if (selectedFamily == null)
                    return;

                if (__ActiveStyle == null)
                {
                    if (__SelectedEdit == null)
                    {
                        ApplyUndoEnabledPropertyValue(OutlinerTree.SelectedItem, RichTextBox.FontFamilyProperty, selectedFamily);
                    }
                    else
                    {
                        __SelectedEdit.ApplyUndoAwarePropertyValue(__SelectedEdit.Selection, RichTextBox.FontFamilyProperty, selectedFamily);
                    }
                }

                if (__SelectedEdit == null || EditorAllTextSelected())
                    UpdateStyle(StylePropertyType.Typeface, selectedFamily);

            }
            finally
            {
                Document.UndoManager.EndGroup();
            }
        }

        private TextRange GetDefaultTextRangeForSelectedRow()
        {
            OutlinerNote note = OutlinerTree.SelectedItem;
            if (note == null)
                return null;

            return new TextRange(note.DefaultRichTextDocument.ContentStart, note.DefaultRichTextDocument.ContentEnd);
        }

        private void FontBold_Click(object sender, RoutedEventArgs e)
        {
            ToggleBold();
        }

        private void FontItalic_Click(object sender, RoutedEventArgs e)
        {
            ToggleItalic();
        }

        private void FontUnderline_Click(object sender, RoutedEventArgs e)
        {
            ToggleUnderlined();
        }

        private void FontStrikethrough_Click(object sender, RoutedEventArgs e)
        {
            ToggleStrikethrough();
        }

        private object GetPropertyForStyle(StylePropertyType stylePropertyType)
        {
            OutlinerNote currentNote = OutlinerTree.SelectedItem;
            if (currentNote == null)
                return null;

            TextRange range = new TextRange(currentNote.DefaultRichTextDocument.ContentStart, currentNote.DefaultRichTextDocument.ContentEnd);

            switch (stylePropertyType)
            {
                case StylePropertyType.IsBold:
                    return TextRangeHelpers.IsBold(range);
                case StylePropertyType.IsItalic:
                    return TextRangeHelpers.IsItalic(range);
                case StylePropertyType.IsStrike:
                    return TextRangeHelpers.IsStrike(range);
                case StylePropertyType.IsUnderlined:
                    return TextRangeHelpers.IsUnderlined(range);
            }

            return null;
        }

        private void ToggleBold()
        {
            Document.UndoManager.StartGroup();
            try
            {
                if (__ActiveStyle != null)
                {
                    UpdateStyle(StylePropertyType.IsBold, !__ActiveStyle.IsBold);
                }
                else
                {

                    if (__SelectedEdit == null)
                    {
                        OutlinerNote selectedNote = GetSelectedNote();

                        TextRange range = new TextRange(selectedNote.DefaultRichTextDocument.ContentStart, selectedNote.DefaultRichTextDocument.ContentEnd);
                        if (range == null)
                            return;

                        if (TextRangeHelpers.IsBold(range) == true)
                            ApplyUndoEnabledPropertyValue(selectedNote, FontWeightProperty, FontWeights.Normal);
                        else
                            ApplyUndoEnabledPropertyValue(selectedNote, FontWeightProperty, FontWeights.Bold);
                    }
                    else
                    {
                        EditingCommands.ToggleBold.Execute(null, __SelectedEdit);
                    }

                    UpdateStyle(StylePropertyType.IsBold, GetPropertyForStyle(StylePropertyType.IsBold));
                }
            }
            finally
            {
                Document.UndoManager.EndGroup();
            }
        }

        private OutlinerNote GetSelectedNote()
        {
            return OutlinerTree.SelectedItem;
        }

        private void UpdateStyle(StylePropertyType stylePropertyType, object value)
        {
            if (__ActiveStyle == null && Document.AutoStyles == false)
                return;

            LevelStyle levelStyle = null;

            if (__ActiveStyle != null)
            {
                Document.UndoManager.PushUndoAction(new UndoStyleChange(__ActiveStyle));
                __ActiveStyle.AddProperty(stylePropertyType, value);
                Document.UpdateStyle(__ActiveStyle, stylePropertyType, value);
            }
            else
            {
                if (value == null)
                    return;

                OutlinerNote currentNote = OutlinerTree.SelectedItem;
                if (currentNote == null)
                    return;

                if (value == null)
                    return;

                levelStyle = Document.Styles.GetStyleForLevel(currentNote.Level);
                Document.UndoManager.PushUndoAction(new UndoStyleChange(levelStyle));
                levelStyle.AddProperty(stylePropertyType, value);
            }
        }

        private void ToggleItalic()
        {
            Document.UndoManager.StartGroup();
            try
            {
                if (__ActiveStyle != null)
                    UpdateStyle(StylePropertyType.IsItalic, !__ActiveStyle.IsItalic);
                else
                {
                    if (__SelectedEdit == null)
                    {

                        OutlinerNote selectedNote = GetSelectedNote();

                        for (int i = 0; i < selectedNote.Columns.Count; i++)
                        {

                            if (selectedNote.Columns[i].DataType != ColumnDataType.RichText)
                                continue;

                            FlowDocument flowDocument = (FlowDocument)selectedNote.Columns[i].ColumnData;


                            TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
                            if (range == null)
                                return;

                            StoreRowFormatting(selectedNote, i);
                            TextRangeHelpers.SetItalic(range, !(TextRangeHelpers.IsItalic(range) == true));
                        }
                    }
                    else
                    {
                        EditingCommands.ToggleItalic.Execute(null, __SelectedEdit);
                    }

                    UpdateStyle(StylePropertyType.IsItalic, GetPropertyForStyle(StylePropertyType.IsItalic));
                }
            }
            finally
            {
                Document.UndoManager.EndGroup();
            }
        }

        private void ToggleUnderlined()
        {
            Document.UndoManager.StartGroup();
            try
            {
                if (__ActiveStyle != null)
                    UpdateStyle(StylePropertyType.IsUnderlined, !__ActiveStyle.IsUnderlined);
                else
                {
                    if (__SelectedEdit == null)
                    {
                        OutlinerNote selectedNote = OutlinerTree.SelectedItem;
                        if (selectedNote == null)
                            return;

                        bool currentUnderline = (TextRangeHelpers.GetTextDecorationOnSelection(GetDefaultTextRangeForSelectedRow(), TextDecorationLocation.Underline)) == true;

                        for (int i = 0; i < selectedNote.Columns.Count; i++)
                        {

                            if (selectedNote.Columns[i].DataType != ColumnDataType.RichText)
                                continue;

                            FlowDocument flowDocument = (FlowDocument)selectedNote.Columns[i].ColumnData;
                            TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);

                            StoreRowFormatting(OutlinerTree.SelectedItem, i);
                            TextRangeHelpers.SetTextDecorationOnSelection(range,
                                TextDecorationLocation.Underline, TextDecorations.Underline,
                                !currentUnderline);
                        }
                    }
                    else
                    {
                        EditingCommands.ToggleUnderline.Execute(null, __SelectedEdit);
                    }

                    UpdateStyle(StylePropertyType.IsUnderlined, GetPropertyForStyle(StylePropertyType.IsUnderlined));
                }
            }
            finally
            {
                Document.UndoManager.EndGroup();
            }
        }

        private void ToggleStrikethrough()
        {
            Document.UndoManager.StartGroup();
            try
            {
                if (__ActiveStyle != null)
                    UpdateStyle(StylePropertyType.IsStrike, !__ActiveStyle.IsStrikethrough);
                else
                {
                    if (__SelectedEdit == null)
                    {
                        OutlinerNote selectedNote = OutlinerTree.SelectedItem;
                        if (selectedNote == null)
                            return;

                        bool currentStrike = TextRangeHelpers.GetTextDecorationOnSelection(GetDefaultTextRangeForSelectedRow(), TextDecorationLocation.Strikethrough) == true;

                        for (int i = 0; i < selectedNote.Columns.Count; i++)
                        {

                            if (selectedNote.Columns[i].DataType != ColumnDataType.RichText)
                                continue;

                            FlowDocument flowDocument = (FlowDocument)selectedNote.Columns[i].ColumnData;
                            TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);

                            StoreRowFormatting(OutlinerTree.SelectedItem, i);
                            TextRangeHelpers.SetTextDecorationOnSelection(range,
                                TextDecorationLocation.Strikethrough, TextDecorations.Strikethrough,
                                !currentStrike);
                        }
                    }
                    else
                    {
                        //__SelectedEdit.IsSelectionStrikethrough = !__SelectedEdit.IsSelectionStrikethrough;
                        OutlinerCommands.ToggleCrossed.Execute(null, __SelectedEdit);
                    }

                    UpdateStyle(StylePropertyType.IsStrike, GetPropertyForStyle(StylePropertyType.IsStrike));
                }
            }
            finally
            {
                Document.UndoManager.EndGroup();
            }
        }

        private void FindString_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (__SelectedEdit != null)
                    Keyboard.Focus(__SelectedEdit);
                else
                {
                    FindString.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
                }
            }
        }

        private TextRange __LastSearchRange;
        private FlowDocument __LastSearchRangeDocument;

        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            string findText = FindString.Text;

            OutlinerNote currentNote = OutlinerTree.SelectedItem;
            if (currentNote == null)
                return;

            TextPointer startPointer = null;
            int startColumn = 0;
            if (__SelectedEdit != null)
            {
                startPointer = __SelectedEdit.Selection.Start;

                __LastColumn = __SelectedEditColumn;
                startColumn = __SelectedEditColumn;

                if (__LastSearchRange != null &&
                    __SelectedEdit.Document == __LastSearchRangeDocument &&
                    __SelectedEdit.Selection.Start.CompareTo(__LastSearchRange.Start) == 0 &&
                    __SelectedEdit.Selection.End.CompareTo(__LastSearchRange.End) == 0)
                    startPointer = __SelectedEdit.Selection.End;

                __LastSearchRangeDocument = __SelectedEdit.Document;

                if (FindStringInDocument(currentNote, __SelectedEditColumn, IsInlineNoteFocused, startPointer, __SelectedEdit.Document.ContentEnd, findText, out __LastSearchRange))
                    return;
            }
            else
            {
                startColumn = 0;
                __LastColumn = 0;

                FlowDocument firstDocument = (FlowDocument)currentNote.Columns[0].ColumnData;

                __LastSearchRangeDocument = firstDocument;
                if (FindStringInDocument(currentNote, startColumn, false, firstDocument.ContentStart, firstDocument.ContentEnd, findText, out __LastSearchRange))
                    return;

                if (currentNote.HasInlineNote)
                {
                    if (FindStringInDocument(currentNote, startColumn, true, currentNote.InlineNoteDocument.ContentStart, currentNote.InlineNoteDocument.ContentEnd, findText, out __LastSearchRange))
                        return;
                }
            }

            List<OutlinerNote> notes = new List<OutlinerNote>();
            DocumentHelpers.GetLinearNotesList(Document.RootNode, notes, true);

            // Find from which line we start the search
            int startSearchFromNoteIdx = 0;
            for (int i = 0; i < notes.Count; i++)
            {
                if (notes[i] == currentNote)
                {
                    startSearchFromNoteIdx = i;

                    // If inline note is active, start from the next line
                    if (IsInlineNoteFocused)
                    {
                        startSearchFromNoteIdx = (i + 1) % notes.Count;
                        break;
                    }

                    if (startColumn >= notes[i].Columns.Count - 1)
                    {
                        // start from the next line if there is no inline notes
                        if (notes[i].HasInlineNote)
                        {
                            startColumn = notes[i].Columns.Count;
                        }
                        else
                        {
                            startSearchFromNoteIdx = (i + 1) % notes.Count;
                            startColumn = 0;
                        }
                    }
                    else
                    {
                        // start search from the next column
                        startColumn++;
                    }

                    break;
                }
            }

            // Cycle through document and perform search
            for (int i = 0; i < notes.Count; i++)
            {
                int idx = (startSearchFromNoteIdx + i) % notes.Count;

                for (int k = startColumn; k < notes[idx].Columns.Count; k++)
                {
                    if (notes[idx].Columns[k].DataType != ColumnDataType.RichText)
                        continue;

                    FlowDocument document = (FlowDocument)notes[idx].Columns[k].ColumnData;
                    __LastSearchRangeDocument = document;
                    __LastColumn = k;
                    if (FindStringInDocument(notes[idx], k, false, document.ContentStart, document.ContentEnd,
                                        findText, out __LastSearchRange))
                    {
                        return;
                    }
                }

                if (notes[idx].HasInlineNote)
                {
                    FlowDocument document = (FlowDocument)notes[idx].InlineNoteDocument;
                    __LastSearchRangeDocument = document;
                    __LastColumn = 0;
                    if (FindStringInDocument(notes[idx], 0, true, document.ContentStart, document.ContentEnd,
                                        findText, out __LastSearchRange))
                    {
                        return;
                    }
                }

                startColumn = 0;
            }
        }



        private bool FindStringInDocument(OutlinerNote note, int columnId, bool isInlineNote, TextPointer startPointer, TextPointer endPointer, string textToFind, out TextRange selection)
        {
            selection = null;
            textToFind = textToFind.ToLower();

            TextRange range = new TextRange(startPointer, endPointer);
            if (range.Text.ToLower().Contains(textToFind))
            {
                OutlinerNote tmpNote = note;
                while (tmpNote.IsRoot == false)
                {
                    tmpNote.Parent.IsExpanded = true;
                    tmpNote = tmpNote.Parent;
                }

                // Wait until containers for all expanded elements are created
                DateTime time = DateTime.Now;
                bool wasAborted = false;
                while (OutlinerTree.ItemContainerGeneratorFor(note) == null)
                {
                    app.DoEvents();
                    System.Threading.Thread.Sleep(10);

                    if (DateTime.Now.Subtract(time).Seconds > 10)
                    {
                        wasAborted = true;
                        break;
                    }
                }

                if (wasAborted)
                    return false;

                TextPointer currentPointer = startPointer;
                while (currentPointer.CompareTo(endPointer) < 0)
                {
                    string text = currentPointer.GetTextInRun(LogicalDirection.Forward).ToLower();
                    if (text.Contains(textToFind))
                    {
                        int idx = text.IndexOf(textToFind);
                        selection = new TextRange(currentPointer.GetPositionAtOffset(idx),
                            currentPointer.GetPositionAtOffset(idx + textToFind.Length));
                        break;
                    }

                    currentPointer = currentPointer.GetNextContextPosition(LogicalDirection.Forward);
                }

                OutlinerTree.MakeActive(note, columnId, isInlineNote, new EventHandler(EditorFocused));
                return true;
            }

            return false;
        }

        private void EditorFocused(object sender, EventArgs e)
        {
            if (__LastSearchRange != null &&
                __LastSearchRangeDocument != null &&
                __SelectedEdit.Document == __LastSearchRangeDocument)
            {
                __SelectedEdit.Selection.Select(__LastSearchRange.Start, __LastSearchRange.End);
            }

            Keyboard.Focus(FindString);
        }

        public BaseStyle CurrentRowStyle
        {
            get
            {
                OutlinerNote note = OutlinerTree.SelectedItem;
                if (note == null)
                {
                    if (Document.Styles.Count > 0)
                        return Document.Styles[0];

                    return null;
                }

                if (note.Level == -1)
                    return null;

                return Document.Styles.GetStyleForLevel(note.Level);
            }
        }

        internal void MakeActiveAndApplyStyle(OutlinerNote newNote, int focusedColumn, bool isInlineNote)
        {
            OutlinerTree.MakeActive(newNote, focusedColumn, isInlineNote, new EventHandler(DocumentHelpers.ApplyStyleAfterMakeActive));
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void DoPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void AddStyleProperty_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveStyleProperty_Click(object sender, RoutedEventArgs e)
        {

        }

        private void StylesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectRowsForSelectedStyle();
        }

        private void SelectRowsForSelectedStyle()
        {
            if (!StylesList.IsKeyboardFocusWithin)
                return;

            BaseStyle style = StylesList.SelectedItem as BaseStyle;
            if (style == null)
                return;

            __ActiveStyle = style;
            UpdateFontSettingsForStyle(style);

            var levelStyle = style as LevelStyle;
            if (levelStyle != null)
            {
                Document.DeselectInlineNote();
                Document.SelectLevel(levelStyle.Level);
            }
            else
            {
                Document.DeselectLevel();
                Document.SelectInlineNotes();
            }
        }

        private void UpdateFontSettingsForStyle(BaseStyle style)
        {
            double fontSize = -1;

            if (style.FontSize != null)
                fontSize = (double)style.FontSize;

            FontFamily fontFamily = null;

            if (style.FontFamily != null)
                fontFamily = style.FontFamily;

            __FontParametersChanging = true;
            try
            {
                TextSettingsSetFontSize(fontSize);
                TextSettingsSetFontFamily(fontFamily);
            }
            finally
            {
                __FontParametersChanging = false;
            }

            SolidColorBrush fontBrush = Brushes.Black;
            if (style.Foreground != null)
                fontBrush = style.Foreground;

            SelectColorBorder.Background = fontBrush;

            FontBold.IsChecked = style.IsBold;
            FontItalic.IsChecked = style.IsItalic;
            FontUnderline.IsChecked = style.IsUnderlined;
            FontStrikethrough.IsChecked = style.IsStrikethrough;
        }

        private void StylesList_LostFocus(object sender, RoutedEventArgs e)
        {
            if (__ActiveStyle != null)
            {
                if (__ActiveStyle.StyleType == StyleType.Inline)
                    Document.DeselectInlineNote();
                else
                    Document.DeselectLevel();

                __ActiveStyle = null;
            }

            UpdateFontSettingsForSelectedItem();
        }

        private void StylesList_GotFocus(object sender, RoutedEventArgs e)
        {
            SelectRowsForSelectedStyle();
        }

        private void StylesList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (__SelectedEdit != null)
                    Keyboard.Focus(__SelectedEdit);
                else
                {
                    FindString.MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
                }
            }
        }

        void CanHoist(object sender, CanExecuteRoutedEventArgs e)
        {
            OutlinerNote currentNote = OutlinerTree.SelectedItem;
            if (currentNote == null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        void CanUnhoist(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Document.IsHoisted)
                e.CanExecute = true;
            else
                e.CanExecute = false;
        }

        void Hoist(object sender, ExecutedRoutedEventArgs e)
        {
            OutlinerNote currentNote = OutlinerTree.SelectedItem;
            if (currentNote == null)
                return;

            Document.UndoManager.PushUndoAction(new UndoHoist(currentNote));
            DoHoist(currentNote);
        }

        void Unhoist(object sender, ExecutedRoutedEventArgs e)
        {
            OutlinerNote hoistNode = Document.HostNode;
            if (hoistNode == null)
                return;

            Document.UndoManager.PushUndoAction(new UndoUnhoist(hoistNode));
            DoUnhoist(hoistNode);
        }

        public void DoHoist(OutlinerNote currentNote)
        {
            FuckAllEdits();
            currentNote.Document.Hoist(currentNote);

            UpdateOutlinerTreeItemsSource();
            UpdateDocumentInspectors();

            if (Document.RootNode.SubNotes.Count == 0)
            {
                Document.RootNode.IsExpanded = true;
                Document.RootNode.SubNotes.Add(new OutlinerNote(Document.RootNode));
                OutlinerTree.MakeActive(currentNote.SubNotes[0], 0, false);
            }

            UpdateHoistTitle();
            OutlinerTree.MakeActive(Document.RootNode.SubNotes[0], 0, false);
        }

        public void DoUnhoist(OutlinerNote hoistedNote)
        {
            FuckAllEdits();
            Document.Unhoist();
            UpdateOutlinerTreeItemsSource();
            UpdateDocumentInspectors();
            OutlinerTree.MakeActive(hoistedNote, 0, false);
        }

        private void FuckAllEdits()
        {
            if (HoistCaption.Visibility == Visibility.Visible)
                HoistCaption.Document = new FlowDocument();

            ObservableCollection<OutlinerNote> notes = (ObservableCollection<OutlinerNote>)OutlinerTree.ItemsSource;
            for (int i = 0; i < notes.Count; i++)
            {
                CheckAndFuckEdit(notes[i]);
                OutlinerDocument.WalkRecursively(notes[i],
                   delegate(OutlinerNote note, out bool shouldWalkSubnotes, out bool shouldContinue)
                   {
                       CheckAndFuckEdit(note);
                       shouldContinue = true;
                       shouldWalkSubnotes = true;
                   });
            }
        }

        private void CheckAndFuckEdit(OutlinerNote note)
        {
            ItemContainerGenerator ig = OutlinerTree.ItemContainerGeneratorFor(note);
            if (ig == null)
                ig = OutlinerTree.ItemContainerGeneratorFor(note.Parent);

            if (ig != null)
            {
                TreeListViewItem container = ig.ContainerFromItem(note) as TreeListViewItem;
                if (container != null)
                {
                    for (int i = 0; i < Document.ColumnDefinitions.Count; i++)
                    {
                        MyEdit editor = container.GetEditor(i, GetViewColumnId(i), false);
                        if (editor != null)
                            editor.Document = new FlowDocument();

                        editor = container.GetEditor(i, GetViewColumnId(i), true);
                        if (editor != null)
                            editor.Document = new FlowDocument();
                    }
                }
            }
        }

        private void UpdateHoistTitle()
        {
            if (Document.IsHoisted)
            {
                HoistCaption.Document = Document.HostNode.DefaultRichTextDocument;
                HoistCaption.Visibility = Visibility.Visible;
            }
            else
            {
                HoistCaption.Document = new FlowDocument();
                HoistCaption.Visibility = Visibility.Collapsed;
            }
            HoistBorder.Visibility = HoistCaption.Visibility;
        }

        void UnhoistAll(object sender, ExecutedRoutedEventArgs e)
        {
            OutlinerNote hoistNode = Document.FirstHostNode;
            if (hoistNode == null)
                return;

            FuckAllEdits();

            while (Document.IsHoisted)
                Document.Unhoist();

            UpdateOutlinerTreeItemsSource();
            UpdateDocumentInspectors();
            OutlinerTree.MakeActive(hoistNode, 0, false);
            UpdateOutlinerTreeItemsSource();
        }

        void NewColumn(object sender, ExecutedRoutedEventArgs e)
        {
            wnd_NewColumn wnd = new wnd_NewColumn();
            wnd.Owner = this;
            if (wnd.ShowDialog() == true)
            {
                AddColumnToDocument(wnd.ColumnName.Text, (ColumnDataType)wnd.ColumnType.SelectedIndex);
            }
        }

        void RemoveColumn(object sender, ExecutedRoutedEventArgs e)
        {
            wnd_ColumnsManager wnd = new wnd_ColumnsManager();
            wnd.InitList(Document.ColumnDefinitions, "Select column to remove", "Remove Column");
            wnd.Owner = this;
            if (wnd.ShowDialog() == true)
            {
                OutlinerColumnDefinition definition = (wnd.ColumnsList.SelectedItem as OutlinerColumnDefinition);
                if (definition == null)
                    return;
                int defIdx = Document.ColumnDefinitions.IndexOf(definition);
                if (defIdx == 0)
                {
                    MessageBox.Show("Main column cannot be removed.", "Cannot remove column", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show(
                        String.Format("This will remove column '{0}'. This action cannot be undone. \nDo you want to continue?", definition.ColumnName),
                        "Column removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        RemoveColumn(defIdx);

                    Document.UndoManager.ClearStacks();
                    OutlinerTree.HeaderVisible = (Document.ColumnDefinitions.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
                }
            }


        }

        private void RemoveColumn(int columnId)
        {
            int viewId = GetViewColumnId(columnId);
            Document.ColumnDefinitions.RemoveAt(columnId);

            OutlinerDocument.WalkRecursively(Document.FakeRootNode,
                delegate(OutlinerNote note, out bool shouldWalkSubnotes, out bool shouldContinue)
                {
                    note.DeleteColumn(columnId);
                    shouldContinue = true;
                    shouldWalkSubnotes = true;
                });

            __OutlinerTreeColumns.RemoveAt(viewId);
            AdjustColumnSizes();
        }

        void ChangeColumnName(object sender, ExecutedRoutedEventArgs e)
        {
            wnd_ColumnsManager wnd = new wnd_ColumnsManager();
            wnd.InitList(Document.ColumnDefinitions, "Select column to change name", "Change Name");
            wnd.Owner = this;
            if (wnd.ShowDialog() == true)
            {
                OutlinerColumnDefinition definition = wnd.ColumnsList.SelectedItem as OutlinerColumnDefinition;
                if (definition == null)
                    return;

                wnd_RenameColumn rename = new wnd_RenameColumn();
                rename.Owner = this;
                rename.ColumnName.Text = definition.ColumnName;
                rename.ColumnName.SelectAll();
                if (rename.ShowDialog() == true)
                {
                    definition.ColumnName = rename.ColumnName.Text;
                }
            }
        }

        void CanExecute_RemoveColumn(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Document.ColumnDefinitions.Count > 1)
                e.CanExecute = true;
            else
                e.CanExecute = false;
        }


        internal int GetViewColumnId(int columnId)
        {
            if (columnId >= Document.ColumnDefinitions.Count)
                return -1;

            GridViewColumn column = Document.ColumnDefinitions[columnId].GridViewColumn;

            for (int i = 0; i < __OutlinerTreeColumns.Count; i++)
                if (__OutlinerTreeColumns[i] == column)
                    return i;

            return -1;
        }

        public int GetColumnIdByView(int viewColumnId)
        {
            GridViewColumn column = __OutlinerTreeColumns[viewColumnId];

            for (int i = 0; i < Document.ColumnDefinitions.Count; i++)
                if (Document.ColumnDefinitions[i].GridViewColumn == column)
                    return i;

            return -1;
        }

        public int LastColumn
        {
            get { return __LastColumn; }
        }

        public GridViewColumnCollection OutlinerTreeColumns
        {
            get
            {
                return __OutlinerTreeColumns;
            }
        }

        public bool IsEditorSelected
        {
            get { return __SelectedEdit != null; }
        }


        private void Print(object sender, ExecutedRoutedEventArgs e)
        {
            wnd_PrintPreview exportWindow = new wnd_PrintPreview();
            exportWindow.SetMainWindow(this);
            exportWindow.Owner = this;
            exportWindow.Document = Document;
            exportWindow.ShowDialog();
        }

        private OutlinerNote CurrentNote()
        {
            return OutlinerTree.SelectedItem;
        }

        private void InsertNote(object sender, ExecutedRoutedEventArgs e)
        {
            OutlinerNote selectedItem = null;
            if (e.Parameter != null)
            {
                selectedItem = e.Parameter as OutlinerNote;
                OutlinerTree.MakeActive(selectedItem, 0, true);
            }
            else
                selectedItem = CurrentNote();

            if (selectedItem == null)
                return;

            UndoInsertInline undoAction = new UndoInsertInline(selectedItem);
            selectedItem.CreateInlineNote();
            Document.UndoManager.PushUndoAction(undoAction);
        }

        private void InsertNote_CanExecute(object sender, CanExecuteRoutedEventArgs args)
        {
            OutlinerNote note = CurrentNote();
            if (note == null)
                return;

            if (args.Parameter is OutlinerNote)
                note = args.Parameter as OutlinerNote;

            if (note.HasInlineNote)
                args.CanExecute = false;
            else
                args.CanExecute = true;
        }

        private void InsertURL(object sender, ExecutedRoutedEventArgs e)
        {
            if (__SelectedEdit == null)
                return;

            /*
            TextPointer p = __SelectedEdit.CaretPosition;

            Button b = new Button();            
            b.Content = "Text";

            InlineUIContainer container = new InlineUIContainer(b);
            container.BaselineAlignment = BaselineAlignment.Center;

            p.Paragraph.Inlines.Add(container);
             */
        }

        private void AttachFile(object sender, ExecutedRoutedEventArgs e)
        {

        }

        public bool IsInlineNoteFocused
        {
            get
            {
                if (__SelectedEdit == null)
                    return false;

                if (__SelectedEdit.Tag == null)
                    return false;

                if (__SelectedEdit.Tag.ToString() == "inline")
                    return true;

                return false;
            }
        }

        protected virtual void OnTemplateChangeOnContentControl(object sender, RoutedEventArgs args)
        {
            if (Document.FocusInlineEditAfterTemplateChange)
            {
                OutlinerTree.MakeActive(OutlinerTree.SelectedItem, __LastColumn, true, new EventHandler(ApplyStyleToInlineNote));
                Document.FocusInlineEditAfterTemplateChange = false;
            }

            if (Document.FocusEditAfterTemplateChange)
            {
                OutlinerTree.MakeActive(OutlinerTree.SelectedItem, __LastColumn, false);
                Document.FocusEditAfterTemplateChange = false;
            }
        }


        private void ApplyStyleToInlineNote(object sender, EventArgs e)
        {
            MakeActiveArgs args = (e as MakeActiveArgs);
            if (args == null)
                return;

            DocumentHelpers.ApplyNewInlineNoteStyle(args.Edit, args.Note);
        }

        private void RemoveInlineNoteIfEmpty(OutlinerNote currentNote)
        {
            if (currentNote.HasInlineNote && currentNote.IsInlineNoteEmpty())
            {
                // add undo operation to undo manager
                var undoAction = new UndoDeleteInline(currentNote);
                currentNote.RemoveInlineNoteIfEmpty();
                __Document.UndoManager.PushUndoAction(undoAction);
            }
        }

        private bool PickColor(ref Color selectedColor)
        {
            ColorPickerDialog colorPicker = new ColorPickerDialog();
            colorPicker.Owner = this;

            colorPicker.cPicker.SelectedColor = selectedColor;
            if (colorPicker.ShowDialog() == true)
            {
                selectedColor = colorPicker.cPicker.SelectedColor;
                return true;
            }

            return false;
        }

        private void EvenRowBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color selectedColor = Document.EvenBackgroundColor.Color;
            if (PickColor(ref selectedColor))
            {
                Document.EvenBackgroundColor = new SolidColorBrush(selectedColor);
                if (Document.AutoOddBackgroundColor)
                    Document.OddBackgroundColor = Document.GetAutoOddBackgroundColor(Document.EvenBackgroundColor);
            }
        }

        private void OddRowBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color selectedColor = Document.OddBackgroundColor.Color;
            if (PickColor(ref selectedColor))
            {
                Document.AutoOddBackgroundColor = false;
                Document.OddBackgroundColor = new SolidColorBrush(selectedColor);
            }
        }

        private void RowLineColorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Color selectedColor = Document.LinesBetweenRowsBrush.Color;
            if (PickColor(ref selectedColor))
            {
                Document.LinesBetweenRows = true;
                Document.LinesBetweenRowsBrush = new SolidColorBrush(selectedColor);
            }
        }

        internal void NewVersionExists(Version newVersion)
        {
            if (newVersion <= Settings.DismissedVersionNotification)
                return;

            TextRange selection = new TextRange(NewVersion.ContentStart, NewVersion.ContentEnd);
            selection.Text = newVersion.ToString();
            NewVersionBlock.Visibility = Visibility.Visible;
            NewVersionBlock.Tag = newVersion;
        }

        private void DismissNewVersionNotification(object sender, RoutedEventArgs e)
        {
            NewVersionBlock.Visibility = Visibility.Collapsed;
            var version = NewVersionBlock.Tag as Version;
            if (version == null)
                return;

            Settings.DismissedVersionNotification = version;
            Settings.SaveSettings();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            UrlHelpers.OpenInBrowser("http://uvoutliner.com/download/");
        }
    }
}