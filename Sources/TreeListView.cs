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
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls.Primitives;
using DragDropListBox;


namespace UVOutliner
{
    public class TreeListView : TreeView
    {
        public static DependencyProperty HeaderVisibleProperty =
            DependencyProperty.Register("HeaderVisible", typeof(Visibility), typeof(TreeListView));

        public TreeListView()
            : base()
        {
            CommandBindings.Add(new CommandBinding(OutlinerCommands.UnfocusEditor, new ExecutedRoutedEventHandler(UnfocusEditorCommand)));
            CommandBindings.Add(new CommandBinding(OutlinerCommands.FocusEditor, new ExecutedRoutedEventHandler(FocusEditorCommand)));
            
            InputBindings.Add(new KeyBinding(OutlinerCommands.IncIndent, new KeyGesture(Key.Right, ModifierKeys.Control)));
            InputBindings.Add(new KeyBinding(OutlinerCommands.DecIndent, new KeyGesture(Key.Left, ModifierKeys.Control)));

            HeaderVisible = Visibility.Collapsed;
        }

        public Visibility HeaderVisible
        {
            get { return (Visibility)GetValue(HeaderVisibleProperty); }
            set { SetValue(HeaderVisibleProperty, value); }
        }

        public new OutlinerNote SelectedItem
        {
            get
            {
                return base.SelectedItem as OutlinerNote;
            }
        }

        public static Window FindParentWindow(FrameworkElement current)
        {
            if (!(current.Parent is FrameworkElement))
                return null;

            FrameworkElement p = (FrameworkElement)current.Parent;
            while (!(p is Window))
            {
                if (p.Parent is FrameworkElement)
                    p = (FrameworkElement)p.Parent;
            }

            if (p is Window)
                return (Window)p;

            return null;
        }

        private void FocusEditorCommand(object sender, RoutedEventArgs e)
        {
            FocusEditorForSelectedRow();
        }

        private void UnfocusEditorCommand(object sender, RoutedEventArgs e)
        {
            DependencyObject o = GetTemplateChild("PART_DockPanel");
            
            if (o is UIElement)
            {
                (o as UIElement).Focus();               
            }

            if (SelectedItem != null)
            {
                ItemContainerGenerator generator = ItemContainerGeneratorFor(SelectedItem);
                if (generator != null)
                {
                    DependencyObject obj = generator.ContainerFromItem(SelectedItem);
                    if (obj is IInputElement)
                        Keyboard.Focus((IInputElement)obj);
                }
            }

            e.Handled = true;
        }

        MainWindow __MainWindow = null;

        private int GetLastEditorColumnId()
        {
            if (__MainWindow == null)
                __MainWindow = DragDropHelper.GetMainWindow(this);

            return __MainWindow.LastColumn;
        }

        private int GetViewLastEditorColumnId()
        {
            if (__MainWindow == null)
                __MainWindow = DragDropHelper.GetMainWindow(this);
            
            return __MainWindow.GetViewColumnId(GetLastEditorColumnId());
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                {
                    OutlinerCommands.InsertBeforeCurrent.Execute(null, this);
                } else
                    OutlinerCommands.InsertAfterCurrent.Execute(null, this);
                e.Handled = true;
            }
            if (e.Key == Key.F2)
            {
                OutlinerCommands.FocusEditor.Execute(null, this);
                //FocusEditorForSelectedRow();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                found_selected = false;
                TreeListViewItem item = FindNextAfterSelected<TreeListViewItem>(this);
                if (item != null)
                    item.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                previousContainer = null;
                TreeListViewItem item = FindBeforeSelected<TreeListViewItem>(this);
                if (item != null)
                    item.Focus();
                e.Handled = true;

            }
            else if (e.Key == Key.Delete)
            {
                OutlinerCommands.DeleteCurrentRow.Execute(null, this);
                e.Handled = true;
            }
            else
            if (e.Key == Key.Tab && e.KeyboardDevice.Modifiers != ModifierKeys.Shift)
            {
                OutlinerCommands.IncIndent.Execute(null, this);
                e.Handled = true;
            }
            else
                if (e.Key == Key.Tab && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                {
                    OutlinerCommands.DecIndent.Execute(null, this);
                    e.Handled = true;
                }
        }

        private void FocusEditorForSelectedRow()
        {
            if (SelectedItem != null)
            {
                ItemContainerGenerator itemGenerator = ItemContainerGeneratorFor(SelectedItem);

                //BUG: Почему здесь иногда бывает null - не ясно
                if (itemGenerator != null)
                {
                    TreeListViewItem li = itemGenerator.ContainerFromItem(SelectedItem) as TreeListViewItem;
                    if (li != null)
                    {
                        MyEdit edit = li.GetEditor(GetViewLastEditorColumnId(), GetLastEditorColumnId(), false) as MyEdit;
                        if (edit != null)
                            Keyboard.Focus(edit);
                    }
                }
            }
        }

        public ItemContainerGenerator ItemContainerGeneratorFor(object SelectedItem)
        {
            return ItemContainerGeneratorFor(SelectedItem, false);
        }

        public ItemContainerGenerator ItemContainerGeneratorFor(object SelectedItem, bool doWait)
        {
            if (SelectedItem == null)
                return null;

            OutlinerNote Note = (SelectedItem as OutlinerNote);
            if (Note.Parent.IsRoot)
                return ItemContainerGenerator;
            else
            {
                ItemContainerGenerator ig = ItemContainerGeneratorFor(Note.Parent);

                if (ig != null)
                {
                    if (ig.Status != GeneratorStatus.ContainersGenerated && doWait)
                    {
                        for (int limit = 0; limit < 100; limit++)
                        {
                            if (ig.Status == GeneratorStatus.ContainersGenerated)
                                break;

                            app.DoEvents();
                            Thread.Sleep(10);
                        }                        
                    }

                    TreeListViewItem tvi = (ig.ContainerFromItem(Note.Parent) as TreeListViewItem);
                    if (tvi != null)
                        return tvi.ItemContainerGenerator;
                }
                return null;
            }
        }

        private bool found_selected = false;
        private bool MoveFocusToNextContainer()
        {
            found_selected = false;
            TreeListViewItem item = FindNextAfterSelected<TreeListViewItem>(this);
            if (item != null)
            {
                item.Focus();
                //item.IsSelected = true;
                return true;
            }
            return false;
        }

        public bool FoundSelected
        {
            set { found_selected = value; }
        }

        public childItem FindNextAfterSelected<childItem>(DependencyObject obj)
                where childItem : TreeListViewItem
        {
            if (found_selected == false && obj is childItem)
            {
                if ((obj as TreeListViewItem).IsSelected)
                    found_selected = true;
            }
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem && found_selected == true && ((child as UIElement).Visibility == Visibility.Visible))
                    return (childItem)child;
                else
                {
                    childItem childOfChild = null;
                    if (child is UIElement && (child as UIElement).Visibility == Visibility.Visible)
                        childOfChild = FindNextAfterSelected<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private TreeListViewItem previousContainer;

        private bool MoveFocusToPreviousContainer()
        {
            previousContainer = null;
            TreeListViewItem item = FindBeforeSelected<TreeListViewItem>(this);
            if (item != null)
            {
                item.Focus();
                return true;
            }
            return false;
        }

        public childItem FindBeforeSelected<childItem>(DependencyObject obj)
                        where childItem : TreeListViewItem
        {
            if (obj is childItem)
                previousContainer = obj as childItem;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem && (child as childItem).IsSelected)
                    return previousContainer as childItem;
                else
                {
                    childItem childOfChild = null;
                    if (child is UIElement && (child as UIElement).Visibility == Visibility.Visible)
                        childOfChild = FindBeforeSelected<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);
        }

        protected override DependencyObject
                           GetContainerForItemOverride()
        {
            TreeListViewItem newItem = new TreeListViewItem(this);
            this.AddLogicalChild(newItem);
            return newItem;
        }

        protected override bool
                           IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        internal void MakeFirstEditorFocused()
        {
            TreeListViewItem ti = (ItemContainerGenerator.ContainerFromIndex(0) as TreeListViewItem);
            if (ti != null)
            {
                MyEdit editor = ti.GetEditor(GetViewLastEditorColumnId(), GetLastEditorColumnId(), false) as MyEdit;
                if (editor != null)
                    Keyboard.Focus(editor);
            }
        }

        private EventHandler __OnMadeActive;
        private int __ColumnIdxToFocus = -1;
        private bool __ColumnToFocusIsInline = false;
        private TreeListViewItem __TviToMakeActive = null;
        private OutlinerNote __NoteToMakeActive = null;

        /// <summary>
        /// Move focus to the editor
        /// </summary>
        /// <param name="newNote">Note, editor of which to make active</param>
        /// <param name="focusColumnIdx">Column number</param>
        internal void MakeActive(OutlinerNote newNote, int focusColumnIdx, bool isActiveInlineEdit)
        {
            MakeActive(newNote, focusColumnIdx, isActiveInlineEdit, null);
        }

        internal void MakeActive(OutlinerNote newNote, int focusColumnIdx, bool isActiveInlineEdit, EventHandler madeActive)
        {
            __OnMadeActive = madeActive;
            __ColumnIdxToFocus = focusColumnIdx;
            __ColumnToFocusIsInline = isActiveInlineEdit;

            ItemContainerGenerator itemContainerGenerator = ItemContainerGeneratorFor(newNote, true);
            if (itemContainerGenerator == null)
                return;

            if (itemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                __NoteToMakeActive = newNote;
                itemContainerGenerator.StatusChanged += new EventHandler(itemContainerGenerator_StatusChanged);
                return;
            }

            TreeListViewItem ti = (itemContainerGenerator.ContainerFromItem(newNote) as TreeListViewItem);                                    
            if (ti != null)
            {                
                ti.Focus();
                
                if (__ColumnIdxToFocus == -1)
                    MakeActiveDone(newNote, null);
                else 
                {
                    CheckMainWindow();
                    MyEdit editor = ti.GetEditor(__MainWindow.GetViewColumnId(focusColumnIdx), focusColumnIdx, isActiveInlineEdit) as MyEdit;
                    if (editor != null)
                    {                        
                        Keyboard.Focus(editor);
                        MakeActiveDone(newNote, editor);
                    }
                    else
                    {
                        __NoteToMakeActive = newNote;
                        __TviToMakeActive = ti;
                        ti.LayoutUpdated += new EventHandler(ti_LayoutUpdated);
                    }
                }
            }
        }

        private void CheckMainWindow()
        {
            if (__MainWindow == null)
                __MainWindow = DragDropHelper.GetMainWindow(this);
        }

        private void MakeActiveDone(OutlinerNote note, MyEdit editor)
        {
            if (__OnMadeActive != null)
            {
                __OnMadeActive(this, new MakeActiveArgs(note, editor));
                __OnMadeActive = null;
            }
        }

        void itemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            ItemContainerGenerator generator = sender as ItemContainerGenerator;
            if (generator == null)
                return;

            if (generator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return;

            generator.StatusChanged -= itemContainerGenerator_StatusChanged;
            TreeListViewItem ti = (generator.ContainerFromItem(__NoteToMakeActive) as TreeListViewItem);                                    

            if (ti == null)
                __NoteToMakeActive = null;
            else
            {                
                ti.Focus();

                if (__ColumnIdxToFocus == -1)
                {
                    MakeActiveDone(__NoteToMakeActive, null);
                    __NoteToMakeActive = null;
                }
                else
                {
                    MyEdit editor = ti.GetEditor(GetViewLastEditorColumnId(), GetLastEditorColumnId(), __ColumnToFocusIsInline) as MyEdit;
                    if (editor != null)
                    {
                        Keyboard.Focus(editor);
                        MakeActiveDone(__NoteToMakeActive, editor);
                        __NoteToMakeActive = null;
                    }
                    else
                    {
                        __TviToMakeActive = ti;
                        ti.LayoutUpdated += new EventHandler(ti_LayoutUpdated);
                    }
                }
            }
            
        }

        void ti_LayoutUpdated(object sender, EventArgs e)
        {
            if (__TviToMakeActive != null)
            {
                MyEdit editor = __TviToMakeActive.GetEditor(GetViewLastEditorColumnId(), GetLastEditorColumnId(), __ColumnToFocusIsInline) as MyEdit;
                if (editor != null)
                {
                    Keyboard.Focus(editor);
                    __TviToMakeActive.LayoutUpdated -= ti_LayoutUpdated;
                    __TviToMakeActive = null;
                    MakeActiveDone(__NoteToMakeActive, editor);
                }                
            }

            __NoteToMakeActive = null;
        }        

        internal void MakeNextOrPrevSelection()
        {
            found_selected = false;
            TreeListViewItem item = FindNextAfterSelected<TreeListViewItem>(this);
            if (item != null)
            {
                item.Focus();
                return;
            }

            previousContainer = null;
            item = FindBeforeSelected<TreeListViewItem>(this);
            if (item != null)
                item.Focus();            
        }        

        OutlinerNote rowToMakeSelected = null;
        internal void SelectRow(OutlinerNote row)
        {
            ItemContainerGenerator t = ItemContainerGeneratorFor(row);
            if (t.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {                
                t.StatusChanged += new EventHandler(SelectRow_ContainerGeneratorStatusChanged);
                rowToMakeSelected = row;

            } else {
                TreeListViewItem tvi = (t.ContainerFromItem(row) as TreeListViewItem);
                tvi.IsSelected = true;
            }
        }

        void SelectRow_ContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            ItemContainerGenerator icg = sender as ItemContainerGenerator;
            if (icg.Status == System.Windows.Controls.Primitives.GeneratorStatus.GeneratingContainers)
                return;

            icg.StatusChanged -= SelectRow_ContainerGeneratorStatusChanged;
            if (rowToMakeSelected == null)
                return;

            TreeListViewItem tvi = icg.ContainerFromItem(rowToMakeSelected) as TreeListViewItem;
            if (tvi != null)
            {
                tvi.IsSelected = true;
                rowToMakeSelected = null;
            }
        }
    }

}
    
