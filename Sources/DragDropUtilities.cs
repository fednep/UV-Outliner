using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections;
using UVOutliner;

namespace DragDropListBox
{
	public static class DranDropUtilities
	{

		public static TreeListViewItem GetItemContainer(ItemsControl itemsControl, Visual bottomMostVisual)
		{
            TreeListViewItem itemContainer = null;
			if (itemsControl != null && bottomMostVisual != null && itemsControl.Items.Count >= 1)
				itemContainer = FindAncestor(typeof(TreeListViewItem), bottomMostVisual) as TreeListViewItem;

			return itemContainer;
		}

		public static FrameworkElement FindAncestor(Type ancestorType, Visual visual)
		{
			while (visual != null && !ancestorType.IsInstanceOfType(visual))
			{
				visual = (Visual)VisualTreeHelper.GetParent(visual);
			}
			return visual as FrameworkElement;
		}


		// Finds the orientation of the panel of the ItemsControl that contains the itemContainer passed as a parameter.
		// The orientation is needed to figure out where to draw the adorner that indicates where the item will be dropped.
		public static bool HasVerticalOrientation(FrameworkElement itemContainer)
		{
			bool hasVerticalOrientation = true;
			if (itemContainer != null)
			{
				Panel panel = VisualTreeHelper.GetParent(itemContainer) as Panel;
				StackPanel stackPanel;
				WrapPanel wrapPanel;

				if ((stackPanel = panel as StackPanel) != null)
				{
					hasVerticalOrientation = (stackPanel.Orientation == Orientation.Vertical);
				}
				else if ((wrapPanel = panel as WrapPanel) != null)
				{
					hasVerticalOrientation = (wrapPanel.Orientation == Orientation.Vertical);
				}
				// You can add support for more panel types here.
			}
			return hasVerticalOrientation;
		}

		public static void InsertItemInItemsControl(ItemsControl itemsControl, OutlinerNote itemToInsert, int insertionIndex)
		{
            OutlinerNote parent = itemsControl.DataContext as OutlinerNote;
            if (parent == null)
                parent = (itemToInsert as OutlinerNote).GetRoot();

            OutlinerNote newNote = new OutlinerNote(parent);
            newNote.Clone(itemToInsert);

            Window ownerWindow = TreeListView.FindParentWindow(itemsControl);
            if (ownerWindow == null)
                throw new Exception("Window cannot be null");

            DocumentHelpers.CopyNodesRecursively(newNote, itemToInsert);

            parent.SubNotes.Insert(insertionIndex, newNote);

            if (itemsControl is TreeListView)
                ((TreeListView)itemsControl).MakeActive(newNote, -1, false);
		}

		public static int RemoveItemFromItemsControl(ItemsControl itemsControl, object itemToRemove)
		{
            OutlinerNote note = (OutlinerNote)itemToRemove;
            if (note == null)
                return -1;

            int noteIndex = note.Parent.SubNotes.IndexOf(note);
            note.Parent.SubNotes.Remove(note);
            return noteIndex;
		}

		public static bool IsInFirstHalf(FrameworkElement container, Point clickedPoint, bool hasVerticalOrientation)
		{
			if (hasVerticalOrientation)
			{
				return clickedPoint.Y < container.ActualHeight / 2;
			}
			return clickedPoint.X < container.ActualWidth / 2;
		}

		public static bool IsMovementBigEnough(Point initialMousePosition, Point currentPosition)
		{
			return (Math.Abs(currentPosition.X - initialMousePosition.X) >= SystemParameters.MinimumHorizontalDragDistance ||
				 Math.Abs(currentPosition.Y - initialMousePosition.Y) >= SystemParameters.MinimumVerticalDragDistance);
		}
	}
}
