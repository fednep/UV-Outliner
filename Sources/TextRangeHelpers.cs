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
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;

namespace UVOutliner
{
    public static class TextRangeHelpers
    {
        public static bool? IsBold(TextRange range)
        {
            if (range.GetPropertyValue(MyEdit.FontWeightProperty) == DependencyProperty.UnsetValue)
                return null;

            return (FontWeight)range.GetPropertyValue(MyEdit.FontWeightProperty) == FontWeights.Bold; 
        }

        public static void SetBold(TextRange range, bool value) 
        {
            if (value == true)
                range.ApplyPropertyValue(Control.FontWeightProperty, FontWeights.Bold);
            else
                range.ApplyPropertyValue(Control.FontWeightProperty, FontWeights.Normal);
        }

        public static bool? IsItalic(TextRange range)
        {
            if (range.GetPropertyValue(Control.FontStyleProperty) == DependencyProperty.UnsetValue)
                return null;

            return (FontStyle)range.GetPropertyValue(MyEdit.FontStyleProperty) == FontStyles.Italic;
        }

        public static void SetItalic(TextRange range, bool value)
        {
            if (value == true)
                range.ApplyPropertyValue(Control.FontStyleProperty, FontStyles.Italic);
            else
                range.ApplyPropertyValue(Control.FontStyleProperty, FontStyles.Normal);
        }

        public static bool? IsStrike(TextRange range)
        {
            return GetTextDecorationOnSelection(range, TextDecorationLocation.Strikethrough);
        }

        public static bool? IsUnderlined(TextRange range)
        {
            return GetTextDecorationOnSelection(range, TextDecorationLocation.Underline);
        }

        public static bool? GetTextDecorationOnSelection(TextRange range, TextDecorationLocation decorationLocation)
        {
            if (range.GetPropertyValue(TextBlock.TextDecorationsProperty) == DependencyProperty.UnsetValue)
                return null;

            TextDecorationCollection decorations = (TextDecorationCollection)range.GetPropertyValue(TextBlock.TextDecorationsProperty);
            
            if (decorations == null)
                return false;

            foreach (TextDecoration decoration in decorations)
            {
                if (decoration.Location == decorationLocation)
                    return true;
            }

            return false;
        }

        public static void SetTextDecorationOnSelection(TextRange range, TextDecorationLocation decorationLocation, TextDecorationCollection newTextDecorations, bool value)
        {
            TextDecorationCollection decorations = new TextDecorationCollection();
            if (range.GetPropertyValue(TextBlock.TextDecorationsProperty) != DependencyProperty.UnsetValue)
            {
                TextDecorationCollection oldDecorations = (TextDecorationCollection)range.GetPropertyValue(TextBlock.TextDecorationsProperty);
                if (oldDecorations != null)
                    decorations.Add(oldDecorations);
            }

            if (value == true)
            {
                bool underlineAlreadyFound = false;
                foreach (TextDecoration decoration in decorations)
                {
                    if (decoration.Location == decorationLocation)
                    {
                        underlineAlreadyFound = true;
                        break;
                    }
                }

                if (!underlineAlreadyFound)
                {
                    decorations.Add(newTextDecorations);
                    range.ApplyPropertyValue(TextBlock.TextDecorationsProperty, decorations);
                }
            }
            else
            {
                for (int i = 0; i < decorations.Count; i++)
                {
                    if (decorations[i].Location == decorationLocation)
                    {
                        decorations.RemoveAt(i);
                        break;
                    }
                }

                range.ApplyPropertyValue(TextBlock.TextDecorationsProperty, decorations);
            }
        }

    }
}
