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
using UVOutliner.Styles;
using System.Windows.Media;

namespace UVOutliner
{
    public class StyleChangedArgs:EventArgs
    {
        public StyleChangedArgs(BaseStyle style)
        {
            Style = style;
        }

        public BaseStyle Style
        {
            get;
            set;
        }
    }

    public class OutlinerStyles: ObservableCollection<BaseStyle>
    {
        public static Dictionary<StylePropertyType, string> StylePropertyTypesToString = new Dictionary<StylePropertyType, string>();
        public static Dictionary<string, StylePropertyType> StringToStylePropertyTypes = new Dictionary<string, StylePropertyType>();

        public event EventHandler<StyleChangedArgs> StyleChanged;

        private List<LevelStyle> __LevelStyles;
        private LevelStyle __WholeDocumentStyle; 
        private InlineNoteStyle __InlineNoteStyle;

        static OutlinerStyles()
        {
            StylePropertyTypesToString[StylePropertyType.FontColor] = "FontColor";
            StylePropertyTypesToString[StylePropertyType.FontSize] = "FontSize";
            StylePropertyTypesToString[StylePropertyType.IsBold] = "IsBold";
            StylePropertyTypesToString[StylePropertyType.IsItalic] = "IsItalic";
            StylePropertyTypesToString[StylePropertyType.IsStrike] = "IsStrikethrough";
            StylePropertyTypesToString[StylePropertyType.IsUnderlined] = "IsUnderlined";
            StylePropertyTypesToString[StylePropertyType.Typeface] = "Typeface";

            StringToStylePropertyTypes["FontColor"] = StylePropertyType.FontColor;
            StringToStylePropertyTypes["FontSize"] = StylePropertyType.FontSize;
            StringToStylePropertyTypes["IsBold"] = StylePropertyType.IsBold;
            StringToStylePropertyTypes["IsItalic"] = StylePropertyType.IsItalic;
            StringToStylePropertyTypes["IsStrikethrough"] = StylePropertyType.IsStrike;
            StringToStylePropertyTypes["IsUnderlined"] = StylePropertyType.IsUnderlined;
            StringToStylePropertyTypes["Typeface"] = StylePropertyType.Typeface;
        }


        public OutlinerStyles()
        {
            __LevelStyles = new List<LevelStyle>();

            __WholeDocumentStyle = new LevelStyle(-1);
            __WholeDocumentStyle.StyleChanged += new EventHandler(OnStyleChanged);
            Add(__WholeDocumentStyle);

            __InlineNoteStyle = new InlineNoteStyle();
            __InlineNoteStyle.AddProperty(StylePropertyType.FontSize, Math.Floor(Settings.DefaultFontSize / 1.2));
            __InlineNoteStyle.AddProperty(StylePropertyType.FontColor, new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)));
            __InlineNoteStyle.StyleChanged += new EventHandler(OnStyleChanged);
            Add(__InlineNoteStyle);

            GetStyleForLevel(5); // Create styles for the first 5 levels         
        }

        void OnStyleChanged(object sender, EventArgs e)
        {
            EventHandler<StyleChangedArgs> handler = StyleChanged;
            if (handler != null)
                handler(this, new StyleChangedArgs(sender as BaseStyle));
        }

        internal BaseStyle GetStyleByTag(Guid tag)
        {
            for (int i = 0; i < Count; i++)
                if (this[i].Tag == tag)
                    return this[i];

            return null;
        }

        // level - starts from zero
        internal LevelStyle GetStyleForLevel(int level)
        {
            if (level == -1)
                return __WholeDocumentStyle;

            while (__LevelStyles.Count <= level)
            {
                var levelStyle = new LevelStyle(__LevelStyles.Count + 1);
                levelStyle.StyleChanged += new EventHandler(OnStyleChanged);
                __LevelStyles.Add(levelStyle);
                Add(levelStyle);
            }

            return __LevelStyles[level - 1];
        }

        public LevelStyle WholeDocumentStyle
        {
            get { return __WholeDocumentStyle; }
        }

        public BaseStyle InlineNoteStyle
        {
            get { return __InlineNoteStyle; }
        }
    }
}
