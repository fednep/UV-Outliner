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
using Microsoft.Win32;
using System.Windows.Media;

namespace UVOutliner
{
    public class Settings
    {
        public static SolidColorBrush DefaultFontColor;
        public static double DefaultFontSize;
        public static FontFamily DefaultFontFamily;
        public static bool FontSelectionPreview;        

        private static RegistryKey registryKey = Registry.CurrentUser;

        static Settings()
        {
            RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner", false);
            if (outlinerKey != null)
            {
                string defaultFont = outlinerKey.GetValue("DefaultFont") as string;                

                if (defaultFont == null)
                    DefaultFontFamily = GetDefaultFontFamily();
                else
                    DefaultFontFamily = GetFontFamily(defaultFont);

                if (outlinerKey.GetValue("DefaultFontSize") == null)
                    DefaultFontSize = 12;
                else
                    DefaultFontSize = double.Parse((string)outlinerKey.GetValue("DefaultFontSize"));

                object preview = outlinerKey.GetValue("FontPreview");
                if (preview == null)
                    FontSelectionPreview = false;
                else
                    FontSelectionPreview = (int)preview > 0 ? true : false;

                object autoOpenLastFile = outlinerKey.GetValue("AutoOpenLast");
                if (autoOpenLastFile == null)
                    AutoOpenLastSavedFile = false;
                else
                    AutoOpenLastSavedFile = (int)autoOpenLastFile > 0 ? true : false;

                if (outlinerKey.GetValue("ExportLastSelectedFilter") == null)
                    ExportLastSelectedFilter = 0;
                else
                    ExportLastSelectedFilter = (int)outlinerKey.GetValue("ExportLastSelectedFilter");

                if (outlinerKey.GetValue("DismissedVersionNotification") == null)
                    DismissedVersionNotification = new Version(0, 0, 0, 0);
                else
                {
                    try
                    {
                        DismissedVersionNotification = new Version((string)outlinerKey.GetValue("DismissedVersionNotification"));
                    } catch
                    {
                        DismissedVersionNotification = new Version(0,0,0,0);
                    }
                }

                if (outlinerKey.GetValue("ExportListOnly") == null)
                    ExportListOnly = false;
                else
                    ExportListOnly = (int)outlinerKey.GetValue("ExportListOnly") == 1 ? true : false;
            }
            else
            {
                DefaultFontFamily = GetDefaultFontFamily();
                DefaultFontSize = 12;
                FontSelectionPreview = false;
            }

            DefaultFontColor = new SolidColorBrush(Colors.Black);
        }

        private static FontFamily GetFontFamily(string font)
        {
            FontFamily fam = TryToFind(font);
            if (fam == null)
                return GetDefaultFontFamily();

            return fam;
        }

        private static FontFamily TryToFind(string fontName)
        {
            foreach (FontFamily fam in Fonts.SystemFontFamilies)
                for (int i = 0; i < fam.FamilyNames.Count; i++)
                {
                    if (fam.ToString() == fontName)
                        return fam;
                }

            return null;
        }

        private static FontFamily GetDefaultFontFamily()
        {
            FontFamily fam = TryToFind("Segoe UI");
            if (fam != null)
                return fam;

            fam = TryToFind("Arial");
            if (fam != null)
                return fam;

            fam = TryToFind("Times New Roman");
            if (fam != null)
                return fam;

            fam = TryToFind("Tahoma");
            if (fam != null)
                return fam;

            fam = TryToFind("MS Sans Serif");
            if (fam != null)
                return fam;

            return Fonts.SystemFontFamilies.GetEnumerator().Current;
        }

        internal static void SaveSettings()
        {
            RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner", true);
            if (outlinerKey == null)
                return;

            outlinerKey.SetValue("DefaultFont", Settings.DefaultFontFamily.ToString());
            outlinerKey.SetValue("DefaultFontSize", Settings.DefaultFontSize.ToString());

            outlinerKey.SetValue("AutoOpenLast", Settings.AutoOpenLastSavedFile ? 1 : 0);
            outlinerKey.SetValue("FontPreview", Settings.FontSelectionPreview? 1 : 0);

            outlinerKey.SetValue("ExportLastSelectedFilter", Settings.ExportLastSelectedFilter);
            outlinerKey.SetValue("ExportListOnly", Settings.ExportListOnly ? 1 : 0);

            outlinerKey.SetValue("DismissedVersionNotification", DismissedVersionNotification.ToString());

            outlinerKey.Close();
        }

        public static bool AutoOpenLastSavedFile
        {
            get;
            set;
        }

        public static int ExportLastSelectedFilter
        {
            get;
            set;
        }

        public static bool ExportListOnly
        {
            get;
            set;
        }

        public static Version DismissedVersionNotification
        {
            get;
            set;
        }
    }
}
