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
using System.Windows;
using System.Windows.Media;
using System.Collections;
using System.Diagnostics;

namespace UVOutliner
{
    public class VisualUtilities
    {

        private int indentDepth = 0;

        public void PrintVisualTree(Visual v)
        {
            if (v == null)
                return;

            string name = null;

            increaseIndent();

            if (v is FrameworkElement)
                name = ((FrameworkElement)v).Name;

            print("Visual Type: " + v.GetType().ToString() + (name != null ? ", Name: " + name : ""));

            // recurse through the children
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(v); i++)
            {
                PrintVisualTree(VisualTreeHelper.GetChild(v, i) as Visual);
            }
            decreaseIndent();
        }

        public void PrintLogicalTree(Object obj)
        {
            increaseIndent();

            if (obj is FrameworkElement)
            {
                FrameworkElement fe = (FrameworkElement)obj;
                print("Logical Type: " + fe.GetType() + ", Name: " + fe.Name);

                // recurse through the children
                IEnumerable children = LogicalTreeHelper.GetChildren(fe);
                foreach (object child in children)
                {
                    PrintLogicalTree(child);
                }

            }
            else
            {
                // stop recursing as we certainly can't have any more FrameworkElement children
                print("Logical Type: " + obj.GetType());
            }

            decreaseIndent();

        }

        private void print(String line)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < indentDepth; i++)
                builder.Append("\t");

            builder.Append(line);
            Debug.WriteLine(builder);
        }

        private void increaseIndent()
        {
            indentDepth++;
        }

        private void decreaseIndent()
        {
            indentDepth--;
        }
    }
}
