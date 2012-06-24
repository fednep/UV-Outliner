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
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System.Windows.Controls;

namespace UVOutliner.Styles
{
    public class BaseStyle: INotifyPropertyChanged
    {
        public event EventHandler StyleChanged;

        private Guid __Tag;
        private ObservableCollection<LevelStyleProperty> __StyleList = new ObservableCollection<LevelStyleProperty>();

        // This fields are required only for displaying style properties in inspector
        // They are not used for formatting
        private FontStyle? __FontStyle;
        private double? __FontSize;
        private SolidColorBrush __Foreground;
        private FontWeight? __FontWeight;
        private TextDecorationCollection __TextDecorations;
        private FontFamily __FontFamily;
        private bool __IsItalic;
        private bool __IsBold;
        private bool __IsUnderlined;
        private bool __IsStrikethrough;
        private StyleType __StyleType;

        public BaseStyle(StyleType styleType)
        {
            __StyleType = styleType;
            __Tag = Guid.NewGuid();
        }

        public Guid Tag
        {
            get { return __Tag; }
        }

        public virtual StyleType StyleType
        {
            get
            {
                return __StyleType;
            }
        }

        public virtual string Name
        {
            get
            {
                return "";
            }
        }

        public void DoStyleChanged()
        {
            EventHandler handler = StyleChanged;
            if (handler != null)
                handler(this, new EventArgs());
        }

        public void UpdateInspectorStyles()
        {
            __Foreground = null;
            __FontSize = null;
            __IsBold = false;            
            __FontWeight = null;
            __IsItalic = false;
            __FontStyle = null;
            __IsStrikethrough = false;
            __TextDecorations = new TextDecorationCollection();
            __FontFamily = null;

            foreach (LevelStyleProperty style in __StyleList)
            {
                switch (style.PropertyType)
                {
                    case StylePropertyType.FontColor:
                        __Foreground = style.Value as SolidColorBrush;
                        break;
                    case StylePropertyType.FontSize:
                        __FontSize = (double?)style.Value;
                        break;
                    case StylePropertyType.IsBold:
                        __IsBold = (bool)style.Value;
                        __FontWeight = __IsBold ? FontWeights.Bold : FontWeights.Normal;
                        break;
                    case StylePropertyType.IsItalic:
                        __IsItalic = (bool)style.Value;
                        __FontStyle = __IsItalic ? FontStyles.Italic : FontStyles.Normal;
                        break;
                    case StylePropertyType.IsStrike:
                        __IsStrikethrough = (bool)style.Value;
                        if (__IsStrikethrough)
                            __TextDecorations.Add(TextDecorations.Strikethrough);
                        break;
                    case StylePropertyType.IsUnderlined:
                        __IsUnderlined = (bool)style.Value;
                        if (__IsUnderlined)
                            __TextDecorations.Add(TextDecorations.Underline);
                        break;
                    case StylePropertyType.Typeface:
                        __FontFamily = style.Value as FontFamily;
                        break;
                }
            }
            DoInspectorChanged();
            DoStyleChanged();
        }

        private void DoInspectorChanged()
        {
            DoPropertyChanged("InspectorFontSize");
            DoPropertyChanged("InspectorFontStyle");
            DoPropertyChanged("InspectorFontFamily");
            DoPropertyChanged("InspectorForeground");
            DoPropertyChanged("InspectorFontWeight");
            DoPropertyChanged("InspectorTextDecorations");
        }

        public void ApplyToRange(TextRange range)
        {
            foreach (LevelStyleProperty style in __StyleList)
            {
                ApplyPropertyToRange(range, style.PropertyType, style.Value);
            }
        }

        public static void ApplyPropertyToRange(TextRange range, StylePropertyType propertyType, object value)
        {
            switch (propertyType)
            {
                case StylePropertyType.FontColor:
                    range.ApplyPropertyValue(Run.ForegroundProperty, value);
                    break;
                case StylePropertyType.FontSize:
                    range.ApplyPropertyValue(Run.FontSizeProperty, (double)value);
                    break;
                case StylePropertyType.IsBold:
                    TextRangeHelpers.SetBold(range, (bool)value);
                    break;
                case StylePropertyType.IsItalic:
                    TextRangeHelpers.SetItalic(range, (bool)value);
                    break;
                case StylePropertyType.IsStrike:
                    TextRangeHelpers.SetTextDecorationOnSelection(range,
                        TextDecorationLocation.Strikethrough, TextDecorations.Strikethrough, (bool)value);
                    break;
                case StylePropertyType.IsUnderlined:
                    TextRangeHelpers.SetTextDecorationOnSelection(range,
                        TextDecorationLocation.Underline, TextDecorations.Underline, (bool)value);
                    break;
                case StylePropertyType.Typeface:
                    range.ApplyPropertyValue(Control.FontFamilyProperty, value);
                    break;
            }
        }

        internal void ApplyToNote(OutlinerNote note)
        {
            for (int i = 0; i < note.Columns.Count; i++)
            {
                FlowDocument document = note.Columns[i].ColumnData as FlowDocument;
                if (document == null)
                    continue;

                TextRange range = new TextRange(document.ContentStart, document.ContentEnd);

                ApplyToRange(range);
                ApplyToDocument(document);
            }
        }

        internal void ApplyToDocument(FlowDocument document)
        {
            foreach (LevelStyleProperty style in __StyleList)
            {
                ApplyPropertyToFlowDocument(document, style.PropertyType, style.Value);
            }
        }

        internal void ApplyToMyEdit(MyEdit myEdit)
        {
            foreach (LevelStyleProperty style in __StyleList)
            {
                ApplyPropertyToFlowDocument(myEdit.Document, style.PropertyType, style.Value);
            }
        }

        public static void ApplyPropertyToFlowDocument(FlowDocument flowDocument, StylePropertyType propertyType, object value)
        {
            switch (propertyType)
            {
                case StylePropertyType.FontColor:
                    flowDocument.Foreground = (Brush)value;
                    break;
                case StylePropertyType.FontSize:
                    flowDocument.FontSize = (double)value;
                    break;
                case StylePropertyType.IsBold:
                    flowDocument.FontWeight = (bool)value == true ? FontWeights.Bold : FontWeights.Normal;
                    break;
                case StylePropertyType.IsItalic:
                    flowDocument.FontStyle = (bool)value == true ? FontStyles.Italic : FontStyles.Normal;
                    break;
                case StylePropertyType.IsStrike:
                    break;
                case StylePropertyType.IsUnderlined:
                    break;
                case StylePropertyType.Typeface:
                    flowDocument.FontFamily = (FontFamily)value;
                    break;
            }
        }

        public void UnapplyToRange(TextRange range)
        {
            foreach (LevelStyleProperty style in __StyleList)
            {
                switch (style.PropertyType)
                {
                    case StylePropertyType.Typeface:
                        range.ApplyPropertyValue(Control.FontFamilyProperty, Settings.DefaultFontFamily);
                        break;
                    case StylePropertyType.FontColor:
                        range.ApplyPropertyValue(Run.ForegroundProperty, Settings.DefaultFontColor);
                        break;
                    case StylePropertyType.FontSize:
                        range.ApplyPropertyValue(Run.FontSizeProperty, Settings.DefaultFontSize);
                        break;
                    case StylePropertyType.IsBold:
                        TextRangeHelpers.SetBold(range, false);
                        break;
                    case StylePropertyType.IsItalic:
                        TextRangeHelpers.SetItalic(range, false);
                        break;
                    case StylePropertyType.IsStrike:
                        TextRangeHelpers.SetTextDecorationOnSelection(range,
                            TextDecorationLocation.Strikethrough, TextDecorations.Strikethrough, false);
                        break;
                    case StylePropertyType.IsUnderlined:
                        TextRangeHelpers.SetTextDecorationOnSelection(range,
                            TextDecorationLocation.Underline, TextDecorations.Underline, false);
                        break;
                }
            }
        }

        internal void UnapplyStyle(OutlinerNote note)
        {
            for (int i = 0; i < note.Columns.Count; i++)
            {
                if (note.Columns[i].DataType != UVOutliner.Columns.ColumnDataType.RichText)
                    continue;

                FlowDocument document = (FlowDocument)note.Columns[i].ColumnData;

                TextRange range = new TextRange(document.ContentStart, document.ContentEnd);
                UnapplyToRange(range);
                UnapplyToDocument(document);
            }
        }

        internal void UnapplyToDocument(FlowDocument document)
        {
            foreach (LevelStyleProperty style in __StyleList)
            {
                switch (style.PropertyType)
                {
                    case StylePropertyType.Typeface:
                        document.FontFamily = Settings.DefaultFontFamily;
                        break;
                    case StylePropertyType.FontColor:
                        document.Foreground = Settings.DefaultFontColor;
                        break;
                    case StylePropertyType.FontSize:
                        document.FontSize = Settings.DefaultFontSize;
                        break;
                    case StylePropertyType.IsBold:
                        document.FontWeight = FontWeights.Normal;
                        break;
                    case StylePropertyType.IsItalic:
                        document.FontStyle = FontStyles.Normal;
                        break;
                }
            }
        }

        internal void UnapplyToMyEdit(MyEdit myEdit)
        {
            foreach (LevelStyleProperty style in __StyleList)
            {
                switch (style.PropertyType)
                {
                    case StylePropertyType.Typeface:
                        myEdit.Document.FontFamily = Settings.DefaultFontFamily;
                        myEdit.FontFamily = Settings.DefaultFontFamily;
                        break;
                    case StylePropertyType.FontColor:
                        myEdit.Document.Foreground = Settings.DefaultFontColor;
                        myEdit.Foreground = Settings.DefaultFontColor;
                        break;
                    case StylePropertyType.FontSize:
                        myEdit.Document.FontSize = Settings.DefaultFontSize;
                        myEdit.FontSize = Settings.DefaultFontSize;
                        break;
                    case StylePropertyType.IsBold:
                        myEdit.Document.FontWeight = FontWeights.Normal;
                        myEdit.FontWeight = FontWeights.Normal;
                        break;
                    case StylePropertyType.IsItalic:
                        myEdit.Document.FontStyle = FontStyles.Normal;
                        myEdit.FontStyle = FontStyles.Normal;
                        break;
                }
            }
        }

        public void AddProperty(StylePropertyType propertyType, object value)
        {
            for (int i = 0; i < __StyleList.Count; i++)
            {
                if (__StyleList[i].PropertyType == propertyType)
                {

                    __StyleList[i].Value = value;

                    UpdateInspectorStyles();
                    return;
                }
            }

            __StyleList.Add(new LevelStyleProperty(propertyType, value));
            UpdateInspectorStyles();
        }

        private bool IsDefaultValue(StylePropertyType propertyType, object value)
        {
            if (propertyType == StylePropertyType.FontSize && (double)value == Settings.DefaultFontSize)
                return true;

            if (propertyType == StylePropertyType.FontColor && ((SolidColorBrush)value).Equals(Settings.DefaultFontColor))
                return true;

            if (propertyType == StylePropertyType.Typeface && ((FontFamily)value).Equals(Settings.DefaultFontFamily))
                return true;

            if (value is bool && (bool)value == false)
                return true;

            return false;
        }

        public int Count
        {
            get { return __StyleList.Count; }
        }

        public ObservableCollection<LevelStyleProperty> Properties
        {
            get { return __StyleList; }
        }

        internal void RemoveProperty(LevelStyleProperty property)
        {
            __StyleList.Remove(property);
        }

        public double? InspectorFontSize
        {
            get { return __FontSize; }
        }

        public SolidColorBrush InspectorForeground
        {
            get
            {
                if (__StyleList.Count > 0)
                {
                    if (__Foreground != null)
                        return __Foreground;
                    return Brushes.Black;
                }

                return new SolidColorBrush(Colors.Gray);
            }
        }

        public FontWeight? InspectorFontWeight
        {
            get { return __FontWeight; }
        }

        public FontFamily InspectorFontFamily
        {
            get { return __FontFamily; }
        }

        public FontStyle? InspectorFontStyle
        {
            get { return __FontStyle; }
        }

        public TextDecorationCollection InspectorTextDecorations
        {
            get { return __TextDecorations; }
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

        internal static object GetValueFromString(StylePropertyType propertyType, string value)
        {
            switch (propertyType)
            {
                case StylePropertyType.FontColor:
                    object c = ColorConverter.ConvertFromString(value);
                    if (c == null)
                        return null;

                    return new SolidColorBrush((Color)c);

                case StylePropertyType.FontSize:
                    double result;
                    if (double.TryParse(value, out result) == false)
                        return null;
                    return result;

                case StylePropertyType.IsBold:
                    if (value == bool.TrueString)
                        return true;
                    return false;

                case StylePropertyType.IsItalic:
                    if (value == bool.TrueString)
                        return true;
                    return false;

                case StylePropertyType.IsStrike:
                    if (value == bool.TrueString)
                        return true;
                    return false;

                case StylePropertyType.IsUnderlined:
                    if (value == bool.TrueString)
                        return true;
                    return false;

                case StylePropertyType.Typeface:
                    return new FontFamily(value);

            }

            return null;
        }

        public FontWeight? FontWeight
        {
            get { return __FontWeight; }
        }

        public FontFamily FontFamily
        {
            get { return __FontFamily; }
        }

        public FontStyle? FontStyle
        {
            get { return __FontStyle; }
        }

        public double? FontSize
        {
            get { return __FontSize; }
        }

        public SolidColorBrush Foreground
        {
            get { return __Foreground; }
        }

        public bool IsBold
        {
            get { return __IsBold; }
        }

        public bool IsItalic
        {
            get { return __IsItalic; }
        }

        public bool IsUnderlined
        {
            get { return __IsUnderlined; }
        }

        public bool IsStrikethrough
        {
            get { return __IsStrikethrough; }
        }

    }
}
