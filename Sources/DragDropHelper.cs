using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections;
using System.Windows.Documents;
using System.Reflection;
using UVOutliner;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UVOutliner.Undo;

namespace DragDropListBox
{
	public class DragDropHelper
	{
		// source and target
		private DataFormat format = DataFormats.GetDataFormat("DragDropItemsControl");
		private Point initialMousePosition;
		private object draggedData;
		//private DraggedAdorner draggedAdorner;
		private InsertionAdorner insertionAdorner;
		private Window topWindow;

		// source
		private ItemsControl sourceItemsControl;
		private TreeListViewItem sourceItemContainer;

		// target
		private ItemsControl targetItemsControl;
		private ItemsControl targetItemContainer;
        private TreeListViewItem targetOverItem;        

		private bool hasVerticalOrientation;
		private int insertionIndex;

        private OutlinerNote __DragOverNote;

		public static bool GetIsDragSource(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsDragSourceProperty);
		}

		public static void SetIsDragSource(DependencyObject obj, bool value)
		{
			obj.SetValue(IsDragSourceProperty, value);
		}

		public static readonly DependencyProperty IsDragSourceProperty =
			DependencyProperty.RegisterAttached("IsDragSource", typeof(bool), typeof(DragDropHelper), new UIPropertyMetadata(false, IsDragSourceChanged));


		public static bool GetIsDropTarget(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsDropTargetProperty);
		}

		public static void SetIsDropTarget(DependencyObject obj, bool value)
		{
			obj.SetValue(IsDropTargetProperty, value);
		}

		public static readonly DependencyProperty IsDropTargetProperty =
			DependencyProperty.RegisterAttached("IsDropTarget", typeof(bool), typeof(DragDropHelper), new UIPropertyMetadata(false, IsDropTargetChanged));

		public static DataTemplate GetDragDropTemplate(DependencyObject obj)
		{
			return (DataTemplate)obj.GetValue(DragDropTemplateProperty);
		}

		public static void SetDragDropTemplate(DependencyObject obj, DataTemplate value)
		{
			obj.SetValue(DragDropTemplateProperty, value);
		}

		public static readonly DependencyProperty DragDropTemplateProperty =
			DependencyProperty.RegisterAttached("DragDropTemplate", typeof(DataTemplate), typeof(DragDropHelper), new UIPropertyMetadata(null));

		private static void IsDragSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var dragSource = obj as ItemsControl;
            
            DragDropHelper instance = GetInstanceForObj(dragSource);
			if (dragSource != null)
			{
				if (Object.Equals(e.NewValue, true))
				{
                    dragSource.PreviewMouseLeftButtonDown += instance.DragSource_PreviewMouseLeftButtonDown;
                    dragSource.PreviewMouseLeftButtonUp += instance.DragSource_PreviewMouseLeftButtonUp;
                    dragSource.PreviewMouseMove += instance.DragSource_PreviewMouseMove;
				}
				else
				{
                    dragSource.PreviewMouseLeftButtonDown -= instance.DragSource_PreviewMouseLeftButtonDown;
                    dragSource.PreviewMouseLeftButtonUp -= instance.DragSource_PreviewMouseLeftButtonUp;
                    dragSource.PreviewMouseMove -= instance.DragSource_PreviewMouseMove;
				}
			}
		}

        private static Dictionary<ItemsControl, DragDropHelper> __InstanceDict = new Dictionary<ItemsControl,DragDropHelper>();

        private static DragDropHelper GetInstanceForObj(ItemsControl dragSource)
        {
            if (!__InstanceDict.ContainsKey(dragSource))
                __InstanceDict[dragSource] = new DragDropHelper();

            return __InstanceDict[dragSource];
        }

		private static void IsDropTargetChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var dropTarget = obj as ItemsControl;
            DragDropHelper instance = GetInstanceForObj(dropTarget);
			if (dropTarget != null)
			{
				if (Object.Equals(e.NewValue, true))
				{
                    Debug.WriteLine("Drop Target not changed");
					dropTarget.AllowDrop = true;
                    dropTarget.PreviewDrop += instance.DropTarget_PreviewDrop;
                    dropTarget.PreviewDragEnter += instance.DropTarget_PreviewDragEnter;
                    dropTarget.PreviewDragOver += instance.DropTarget_PreviewDragOver;
                    dropTarget.PreviewDragLeave += instance.DropTarget_PreviewDragLeave;
				}
				else
				{
                    Debug.WriteLine("Drop Target Changed!");
					dropTarget.AllowDrop = false;
                    dropTarget.PreviewDrop -= instance.DropTarget_PreviewDrop;
                    dropTarget.PreviewDragEnter -= instance.DropTarget_PreviewDragEnter;
                    dropTarget.PreviewDragOver -= instance.DropTarget_PreviewDragOver;
                    dropTarget.PreviewDragLeave -= instance.DropTarget_PreviewDragLeave;
				}
			}
		}

		// DragSource

        public static MainWindow GetMainWindow(Visual visual)
        {
            return DranDropUtilities.FindAncestor(typeof(Window), visual) as MainWindow;
        }

		private void DragSource_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{                        
            this.sourceItemsControl = (ItemsControl)sender;
            TreeListView tlv = (TreeListView)this.sourceItemsControl;

            this.topWindow = (Window)DranDropUtilities.FindAncestor(typeof(Window), this.sourceItemsControl);
            this.initialMousePosition = e.GetPosition(this.topWindow);

            Visual visual = e.OriginalSource as Visual;

            this.sourceItemContainer = DranDropUtilities.GetItemContainer(this.sourceItemsControl, visual);
            if (this.sourceItemContainer != null)            
                this.draggedData = sourceItemContainer.DataContext;            

		}

		// Drag = mouse down + move by a certain amount
		private void DragSource_PreviewMouseMove(object sender, MouseEventArgs e)
		{

			if (this.draggedData != null)
			{
				// Only drag when user moved the mouse by a reasonable amount.
				if (DranDropUtilities.IsMovementBigEnough(this.initialMousePosition, e.GetPosition(this.topWindow)))
				{
					DataObject data = new DataObject(this.format.Name, this.draggedData);

					// Adding events to the window to make sure dragged adorner comes up when mouse is not over a drop target.
					bool previousAllowDrop = this.topWindow.AllowDrop;
					this.topWindow.AllowDrop = true;
					this.topWindow.DragEnter += TopWindow_DragEnter;
					this.topWindow.DragOver += TopWindow_DragOver;
					this.topWindow.DragLeave += TopWindow_DragLeave;

                    DragDropEffects effects = DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);

					// Without this call, there would be a bug in the following scenario: Click on a data item, and drag
					// the mouse very fast outside of the window. When doing this really fast, for some reason I don't get 
					// the Window leave event, and the dragged adorner is left behind.
					// With this call, the dragged adorner will disappear when we release the mouse outside of the window,
					// which is when the DoDragDrop synchronous method returns.
					//RemoveDraggedAdorner();
                    //RemoveInsertionAdorner();

					this.topWindow.AllowDrop = previousAllowDrop;
					this.topWindow.DragEnter -= TopWindow_DragEnter;
					this.topWindow.DragOver -= TopWindow_DragOver;
					this.topWindow.DragLeave -= TopWindow_DragLeave;
					
					this.draggedData = null;
				}
			}
		}
			
		private void DragSource_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
            this.draggedData = null;
            RemoveDragOverMargin();
            RemoveInsertionAdorner();            
		}

		private void DropTarget_PreviewDragEnter(object sender, DragEventArgs e)
		{
			this.targetItemsControl = (ItemsControl)sender;
			object draggedItem = e.Data.GetData(this.format.Name);

			DecideDropTarget(e);
			if (draggedItem != null)
			{
				CreateInsertionAdorner();
			}
			e.Handled = true;
		}

		private void DropTarget_PreviewDragOver(object sender, DragEventArgs e)
		{
			object draggedItem = e.Data.GetData(this.format.Name);

			DecideDropTarget(e);
			if (draggedItem != null)
			{				
				UpdateInsertionAdornerPosition();
			}
			e.Handled = true;
		}

		private void DropTarget_PreviewDrop(object sender, DragEventArgs e)
		{
            
			object draggedItem = e.Data.GetData(this.format.Name);
			int indexRemoved = -1;            
			if (draggedItem != null)
			{

                OutlinerNote note = draggedItem as OutlinerNote;
                if (note == null)
                {
                    RemoveInsertionAdorner();
                    RemoveDragOverMargin();
                    return;
                }

                UndoDragDrop undo = new UndoDragDrop(note); 

                indexRemoved = note.Parent.SubNotes.IndexOf(note);
                note.Parent.SubNotes.Remove(note);

				// This happens when we drag an item to a later position within the same ItemsControl.
				if (indexRemoved != -1 && 
                    this.sourceItemContainer.ParentItemsControl == this.targetItemContainer && indexRemoved < this.insertionIndex)
				{
					this.insertionIndex--;
				}
				DranDropUtilities.InsertItemInItemsControl(this.targetItemContainer, draggedItem as OutlinerNote, this.insertionIndex);                
                note.Document.UndoManager.PushUndoAction(undo);

				RemoveInsertionAdorner();
                RemoveDragOverMargin();                
			}
			e.Handled = true;
		}

		private void DropTarget_PreviewDragLeave(object sender, DragEventArgs e)
		{
			// Dragged Adorner is only created once on DragEnter + every time we enter the window. 
			// It's only removed once on the DragDrop, and every time we leave the window. (so no need to remove it here)
			object draggedItem = e.Data.GetData(this.format.Name);

			if (draggedItem != null)
			{
				RemoveInsertionAdorner();
                RemoveDragOverMargin();
			}
			e.Handled = true;
		}

		private void DecideDropTarget(DragEventArgs e)
		{            
			int targetItemsControlCount = this.targetItemsControl.Items.Count;
			object draggedItem = e.Data.GetData(this.format.Name);

            Visual visual = e.OriginalSource as Visual;

            targetOverItem = DranDropUtilities.FindAncestor(typeof(TreeListViewItem), visual) as TreeListViewItem;
            if (targetOverItem == null)
            {                
                this.targetItemContainer = null;
                this.insertionIndex = -1;
                e.Effects = DragDropEffects.None;
                return;
            }

            bool newDragOverNote = false;
            OutlinerNote dragOverNote = null;
            if (targetOverItem != null)
            {
                dragOverNote = targetOverItem.DataContext as OutlinerNote;
                if (dragOverNote != null)
                {                    
                    if (__DragOverNote != dragOverNote)
                    {
                        if (__DragOverNote != null)
                            __DragOverNote.DragOverNote = false;

                        newDragOverNote = true;
                    }                    
                }
            }
            
            ItemsControl parentItemsControl = targetOverItem.ParentItemsControl;
            if (parentItemsControl == null)
            {
                this.targetItemContainer = null;
                this.insertionIndex = -1;
                e.Effects = DragDropEffects.None;
                return;
            }

			if (targetItemsControlCount > 0)
			{
				this.hasVerticalOrientation = DranDropUtilities.HasVerticalOrientation(this.targetItemsControl.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement);
				this.targetItemContainer = parentItemsControl;
                this.insertionIndex = parentItemsControl.ItemContainerGenerator.IndexFromContainer(targetOverItem);
                if (this.insertionIndex == -1)
                {
                    this.targetItemContainer = null;
                    this.insertionIndex = -1;
                    e.Effects = DragDropEffects.None;
                    return;
                }

                if (IsParent(this.targetItemContainer as TreeListViewItem, this.sourceItemContainer))
                {
                    this.targetItemContainer = null;
                    this.insertionIndex = -1;
                    e.Effects = DragDropEffects.None;
                    return;
                }

                if (newDragOverNote && dragOverNote != null)
                {
                    dragOverNote.DragOverNote = true;
                    __DragOverNote = dragOverNote;
                }
			}
			else
			{
				this.targetItemContainer = null;
				this.insertionIndex = 0;
			}
		}

        private bool IsParent(TreeListViewItem child, ItemsControl parent)
        {
            if (child == null)
                return false;

            if (child == parent)
                return true;

            return IsParent(child.ParentItemsControl as TreeListViewItem, parent);
        }

		// Can the dragged data be added to the destination collection?
		// It can if destination is bound to IList<allowed type>, IList or not data bound.
		private bool IsDropDataTypeAllowed(object draggedItem)
		{
			bool isDropDataTypeAllowed;
			IEnumerable collectionSource = this.targetItemsControl.ItemsSource;
			if (draggedItem != null)
			{
				if (collectionSource != null)
				{
					Type draggedType = draggedItem.GetType();
					Type collectionType = collectionSource.GetType();

					Type genericIListType = collectionType.GetInterface("IList`1");
					if (genericIListType != null)
					{
						Type[] genericArguments = genericIListType.GetGenericArguments();
						isDropDataTypeAllowed = genericArguments[0].IsAssignableFrom(draggedType);
					}
					else if (typeof(IList).IsAssignableFrom(collectionType))
					{
						isDropDataTypeAllowed = true;
					}
					else
					{
						isDropDataTypeAllowed = false;
					}
				}
				else // the ItemsControl's ItemsSource is not data bound.
				{
					isDropDataTypeAllowed = true;
				}
			}
			else
			{
				isDropDataTypeAllowed = false;			
			}
			return isDropDataTypeAllowed;
		}

		// Window

		private void TopWindow_DragEnter(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;
			e.Handled = true;
		}

		private void TopWindow_DragOver(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;
			e.Handled = true;

		}

		private void TopWindow_DragLeave(object sender, DragEventArgs e)
		{
			e.Handled = true;            
		}

        private void RemoveDragOverMargin()
        {
            if (__DragOverNote != null)
            {
                __DragOverNote.DragOverNote = false;
                __DragOverNote = null;
            }
        }

		// Adorners

        private void CreateInsertionAdorner()
		{
            if (this.targetOverItem != null)
			{
                // Here, I need to get adorner layer from targetItemContainer and not targetItemsControl. 
				// This way I get the AdornerLayer within ScrollContentPresenter, and not the one under AdornerDecorator (Snoop is awesome).
                // If I used targetItemsControl, the adorner would hang out of ItemsControl when there's a horizontal scroll bar.
                targetOverItem.InsertingBefore = true;
                var adornerLayer = AdornerLayer.GetAdornerLayer(targetOverItem);
                this.insertionAdorner = new InsertionAdorner(this.hasVerticalOrientation, true /*this.isInFirstHalf*/, targetOverItem, adornerLayer);
			}
		}

		private void UpdateInsertionAdornerPosition()
		{
			if (this.insertionAdorner != null)
			{
                this.insertionAdorner.InvalidateVisual();
			}
		}

		private void RemoveInsertionAdorner()
		{
			if (this.insertionAdorner != null)
			{
				this.insertionAdorner.Detach();
                (this.insertionAdorner.AdornedElement as TreeListViewItem).InsertingBefore = false;
				this.insertionAdorner = null;
			}            
		}
	}
}
