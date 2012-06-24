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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Documents;
using System.Windows.Controls;
using UVOutliner.Styles;

namespace UVOutliner
{
    public class LevelStyle : BaseStyle
    {
        private int __Level;
               
        public LevelStyle(int level): base(StyleType.Level)
        {
            __Level = level;
        }

        public int Level
        {
            get { return __Level; }
        }

        public override string Name
        {
            get
            {
                if (__Level > 0)
                    return String.Format("Level {0}", Level);

                return "Whole document";
            }
        }

    }
}
