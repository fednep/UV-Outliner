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
using System.ComponentModel;

namespace UVOutliner
{
    /// <summary>
    /// Outliner styles are a collection of pairs "property" - "value"
    /// This class represent such a style
    /// </summary>
    public class LevelStyleProperty : INotifyPropertyChanged
    {
        private StylePropertyType __PropertyType;
        private object __Value;

        public LevelStyleProperty(StylePropertyType propertyType, object value)
        {
            __PropertyType = propertyType;
            __Value = value;
        }

        public StylePropertyType PropertyType
        {
            get { return __PropertyType; }
        }

        public object Value
        {
            get
            {
                return __Value;
            }

            set
            {
                __Value = value;
                DoPropertyChanged("Value");
                DoPropertyChanged("DisplayValue");
            }
        }

        public override string ToString()
        {
            switch (__PropertyType)
            {
                case StylePropertyType.IsBold:
                    return "Font weight: " + ((bool)Value == true ? "bold" : "normal");
                case StylePropertyType.IsItalic:
                    return "Italic: " + ((bool)Value == true ? "yes" : "no");
                case StylePropertyType.IsUnderlined:
                    return "Underline: " + ((bool)Value == true ? "yes" : "no");
                case StylePropertyType.IsStrike:
                    return "Strikethough: " + ((bool)Value == true ? "yes" : "no");
            }

            return "";
        }

        public string DisplayValue
        {
            get
            {
                return ToString();
            }
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
