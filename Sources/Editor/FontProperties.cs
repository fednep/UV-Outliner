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
using System.Windows.Documents;
using System.Windows.Controls;

namespace UVOutliner.Editor
{
    public class FontProperties
    {
        private object __FontWeight;
        private object __FontSize;
        private FontFamily __FontFamily;
        private TextDecorationCollection __TextDecorations;
        private object __FontStyle;
        private Brush __ForegroundColor;

        public FontProperties(FlowDocument document)
        {
            __FontWeight = document.FontWeight;
            __FontSize = document.FontSize;
            __FontStyle = document.FontStyle;
            __TextDecorations = null;
            __FontFamily = document.FontFamily;
            __ForegroundColor = document.Foreground;
        }

        public FontProperties(TextRange range)
        {            
            __FontWeight = range.GetPropertyValue(RichTextBox.FontWeightProperty);
            __FontSize = range.GetPropertyValue(RichTextBox.FontSizeProperty);
            __FontStyle = range.GetPropertyValue(RichTextBox.FontStyleProperty);
            __TextDecorations = range.GetPropertyValue(TextBlock.TextDecorationsProperty) as TextDecorationCollection;
            __FontFamily = range.GetPropertyValue(RichTextBox.FontFamilyProperty) as FontFamily;
            __ForegroundColor = range.GetPropertyValue(RichTextBox.ForegroundProperty) as Brush;
        }

        public void ApplyToFlowDocument(FlowDocument document)
        {
            if (__FontWeight is FontWeight)
                document.FontWeight = (FontWeight)__FontWeight;
            if (__FontSize is double)
                document.FontSize = (double)__FontSize;

            if (__FontStyle is FontStyle)
                document.FontStyle = (FontStyle)__FontStyle;
            document.FontFamily = __FontFamily;
            document.Foreground = __ForegroundColor;
        }

        public bool HasSameStyle(TextRange range)
        {
            if (CompareBrushes(__ForegroundColor, range.GetPropertyValue(TextBlock.ForegroundProperty) as Brush))
                return false;


            if (__FontWeight != range.GetPropertyValue(RichTextBox.FontWeightProperty))
                return false;

            if (__FontSize != range.GetPropertyValue(RichTextBox.FontSizeProperty))
                return false;

            if (__FontStyle != range.GetPropertyValue(RichTextBox.FontStyleProperty))
                return false;

            if (CompareDecorations(__TextDecorations, range.GetPropertyValue(TextBlock.TextDecorationsProperty) as TextDecorationCollection))
                return false;            

            if (__FontFamily != range.GetPropertyValue(RichTextBox.FontFamilyProperty))
                return false;

            

            return true;
        }

        private bool CompareBrushes(Brush brush1, Brush brush2)
        {
            return !brush1.Equals(brush2);
        }

        private bool CompareDecorations(TextDecorationCollection decoration1, TextDecorationCollection decoration2)
        {
            if (decoration1 == null && decoration2 != null)
                return true;

            if (decoration1 != null && decoration2 == null)
                return true;

            if (decoration1 == null && decoration2 == null)
                return false;

            if (decoration1.Count != decoration2.Count)
                return true;

            for (int i = 0; i < decoration1.Count; i++)
            {
                bool found = false;
                for (int k = 0; k < decoration2.Count; k++)
                {
                    if (decoration1[i] == decoration2[k])
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return true;
            }

            return false;
        }

    }
}
