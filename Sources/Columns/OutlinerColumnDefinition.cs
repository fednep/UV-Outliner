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
using System.ComponentModel;

namespace UVOutliner.Columns
{
    public class OutlinerColumnDefinition: INotifyPropertyChanged
    {
        private const double DEFAULT_COLUMN_WIDTH = 150;
        private ColumnDataType __DataType;
        private string __Name;
        private double __Width;

        public OutlinerColumnDefinition(string name, ColumnDataType dataType)
        {
            __Name = name;
            __Width = DEFAULT_COLUMN_WIDTH;
            __DataType = dataType;
        }

        public string ColumnName
        {
            get
            {
                return __Name;
            }
            set
            {                
                __Name = value;
                DoPropertyChanged("ColumnName");
            }
        }

        public ColumnDataType DataType
        {
            get 
            { 
                return __DataType; 
            }
        }

        public double Width
        {
            get
            {
                return __Width;
            }

            set
            {
                __Width = value;
                if (__Width == 0)
                    __Width = 25;
            }
        }

        public GridViewColumn GridViewColumn
        {
            get;
            set;
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
    }
}
