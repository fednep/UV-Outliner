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
using System.Windows.Input;

namespace UVOutliner
{
    
    public class OutlinerCommands
    {

        public static RoutedCommand CollapseAll = new RoutedCommand();
        public static RoutedCommand ExpandAll = new RoutedCommand();

        public static RoutedCommand ExpandAllLevel1 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel2 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel3 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel4 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel5 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel6 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel7 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel8 = new RoutedCommand();
        public static RoutedCommand ExpandAllLevel9 = new RoutedCommand();

        public static RoutedCommand InsertAfterCurrent = new RoutedCommand();
        public static RoutedCommand InsertBeforeCurrent = new RoutedCommand();
        public static RoutedCommand DeleteCurrentRow = new RoutedCommand();
        public static RoutedCommand UnfocusEditor = new RoutedCommand();
        public static RoutedCommand FocusEditor = new RoutedCommand();
        public static RoutedCommand New = new RoutedCommand();
        public static RoutedCommand Save = new RoutedCommand();
        public static RoutedCommand SaveAs = new RoutedCommand();
        public static RoutedCommand Open = new RoutedCommand();
        public static RoutedCommand Export = new RoutedCommand();
        public static RoutedCommand IncIndent = new RoutedCommand();
        public static RoutedCommand DecIndent = new RoutedCommand();
        public static RoutedCommand MoveRowUp = new RoutedCommand();
        public static RoutedCommand MoveRowDown = new RoutedCommand();
        public static RoutedCommand ToggleShowCheckboxes = new RoutedCommand();
        public static RoutedCommand ToggleAutoStyles = new RoutedCommand();
        public static RoutedCommand ToggleShowInspectors = new RoutedCommand();
        public static RoutedCommand Exit = new RoutedCommand();
        public static RoutedCommand OpenRecentFile = new RoutedCommand();
        public static RoutedCommand OpenFindWindow = new RoutedCommand();
        public static RoutedCommand ApplyLevelStyle = new RoutedCommand();
        public static RoutedCommand Undo = new RoutedCommand();
        public static RoutedCommand Redo = new RoutedCommand();
        public static RoutedCommand Settings = new RoutedCommand();
        public static RoutedCommand Register = new RoutedCommand();
        public static RoutedCommand Print = new RoutedCommand();

        public static RoutedCommand CheckUncheck = new RoutedCommand();

        public static RoutedCommand Hoist = new RoutedCommand();
        public static RoutedCommand Unhoist = new RoutedCommand();
        public static RoutedCommand UnhoistAll = new RoutedCommand();

        public static RoutedCommand ToggleCrossed = new RoutedCommand();

        public static RoutedCommand NewColumn = new RoutedCommand();
        public static RoutedCommand RemoveColumn = new RoutedCommand();
        public static RoutedCommand ChangeColumnName = new RoutedCommand();

        public static RoutedCommand InsertNote = new RoutedCommand();        
        public static RoutedCommand InsertURL = new RoutedCommand();
        public static RoutedCommand AttachFile = new RoutedCommand();

    }
}
