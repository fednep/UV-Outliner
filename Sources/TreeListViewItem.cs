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
using System.ComponentModel;
using System.Diagnostics;


namespace UVOutliner
{
    
    public class TreeListViewItem : TreeViewItem, INotifyPropertyChanged
    {
        private ItemsControl __ParentItemsControl;
        public static readonly DependencyProperty IsEditorFocusedProperty = DependencyProperty.Register("IsEditorFocused", typeof(bool),
             typeof(TreeListViewItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsMouseOverSpecialAreaProperty = DependencyProperty.Register("IsMouseOverSpecialArea", typeof(bool),
                        typeof(TreeListViewItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty InsertingBeforeProperty = DependencyProperty.Register("InsertingBefore",
            typeof(bool), typeof(TreeListViewItem), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TopDragMarginProperty = DependencyProperty.Register("TopDragMargin",
            typeof(double), typeof(TreeListViewItem), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly RoutedEvent EditingFinishedEvent = EventManager.RegisterRoutedEvent("EditingFinished",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TreeViewItem));


        static TreeListViewItem()
        {
            
        }        

        public bool InsertingBefore
        {
            get { return (bool)GetValue(InsertingBeforeProperty); }
            set { SetValue(InsertingBeforeProperty, value); }
        }

        public TreeListViewItem(ItemsControl parentItemsControl)
            : base()
        {            
            AddHandler(MyEdit.PreviewEditorGotFocusEvent, new RoutedEventHandler(OnEditGotFocus));
            AddHandler(MyEdit.PreviewEditorLostFocusEvent, new RoutedEventHandler(OnEditLostFocus));

            __ParentItemsControl = parentItemsControl;            
        }

        public ItemsControl ParentItemsControl
        {
            get { return __ParentItemsControl; }
        }

        public void OnEditGotFocus(object sender, RoutedEventArgs e)
        {
            IsSelected = true;
            e.Handled = true;
            SetValue(TreeListViewItem.IsEditorFocusedProperty, true);
        }

        public void OnEditLostFocus(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(EditingFinishedEvent, this));

            e.Handled = true;
            SetValue(TreeListViewItem.IsEditorFocusedProperty, false);
        }

        public bool IsEditorFocused
        {
            get { return (bool)GetValue(IsEditorFocusedProperty); }
            set { SetValue(IsEditorFocusedProperty, value); }
        }

        public bool IsMouseOverSpecialArea
        {
            get { return (bool)GetValue(IsMouseOverSpecialAreaProperty); }
            set { SetValue(IsMouseOverSpecialAreaProperty, value); }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        /// <summary>
        /// Item's hierarchy in the tree
        /// </summary>
        public int Level
        {
            get
            {
                if (_level == -1)
                {
                    TreeListViewItem parent = 
                        ItemsControl.ItemsControlFromItemContainer(this) 
                            as TreeListViewItem;
                    _level = (parent != null) ? parent.Level + 1 : 0;
                }
                return _level;
            }
        }  

        protected override DependencyObject 
                           GetContainerForItemOverride()
        {
            TreeListViewItem newItem = new TreeListViewItem(this);
            this.AddLogicalChild(newItem);
            return newItem;
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj)
                where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }        

        /// <summary>
        /// Return editor for the column. 
        /// </summary>
        public MyEdit GetEditor(int viewColumnId, int columnId, bool isInlineNote)
        {
            GridViewRowPresenter rowPresenter = GetTemplateChild("PART_Header") as GridViewRowPresenter;
            if (rowPresenter == null)
                return null;            

            DataTemplate dataTemplate = rowPresenter.Columns[viewColumnId].CellTemplate;
            ContentPresenter contentPresenter = GetContentPresenter(rowPresenter, columnId);
            if (contentPresenter == null)
                return null;

            try
            {
                var contentControl = dataTemplate.FindName("PART_ContentControl", contentPresenter) as ContentControl;
                if (contentControl == null)
                {
                    var edit = dataTemplate.FindName("PART_MyEdit", contentPresenter);
                    return edit as MyEdit;
                }
                else
                {
                    DataTemplate contentControlTemplate = contentControl.ContentTemplate;
                    ContentPresenter presenter = VisualTreeHelper.GetChild(contentControl, 0) as ContentPresenter;
                    
                    object ed;
                    if (isInlineNote)
                        ed = contentControlTemplate.FindName("PART_InlineEdit", presenter);
                    else
                        ed = contentControlTemplate.FindName("PART_MyEdit", presenter);                    

                    return ed as MyEdit;
                }
            }
            catch
            {
                return null;
            }
        }

        private ContentPresenter GetContentPresenter (GridViewRowPresenter rowPresenter, int index)                
        {
            int childCount = VisualTreeHelper.GetChildrenCount(rowPresenter);
            if (index > childCount - 1)
                return null;

            return VisualTreeHelper.GetChild(rowPresenter, index) as ContentPresenter;
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            IsSelected = true;
            Keyboard.Focus(this);
            e.Handled = true;
        }

        public double TopDragMargin
        {
            get { return (double)GetValue(TopDragMarginProperty); }
            set { SetValue(TopDragMarginProperty, value); }
        }

        private int _level = -1;

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private Visibility __AddNoteSignVisibility = Visibility.Hidden;
        public Visibility AddNoteSignVisibility
        {
            get
            {
                return __AddNoteSignVisibility;
            }
            set
            {
                __AddNoteSignVisibility = value;
                DoPropertyChanged("AddNoteSignVisibility");
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            AddNoteSignVisibility = System.Windows.Visibility.Visible;
            base.OnMouseEnter(e);            
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            AddNoteSignVisibility = System.Windows.Visibility.Hidden;
            base.OnMouseLeave(e);            
        }


        public void DoPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler h = PropertyChanged;
            if (h != null)
                h(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

}