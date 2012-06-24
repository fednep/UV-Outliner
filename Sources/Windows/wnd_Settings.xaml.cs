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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace UVOutliner
{
    /// <summary>
    /// Interaction logic for wnd_Settings.xaml
    /// </summary>
    public partial class wnd_Settings : Window
    {
        public wnd_Settings()
        {
            InitializeComponent();
            InitFontComboBox();

            cbPreviewFonts.IsChecked = Settings.FontSelectionPreview;
            OpenLast.IsChecked = Settings.AutoOpenLastSavedFile;
        }

        private void InitFontComboBox()
        {
            cbFonts.ItemsSource = Fonts.SystemFontFamilies;
            cbFonts.SelectedItem = Settings.DefaultFontFamily;

            bool foundFontSize = false;
            for (double i = 5; i < 73; i += 1)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = i.ToString();
                if (i == Settings.DefaultFontSize)
                {
                    cbi.IsSelected = true;
                    foundFontSize = true;
                }
                cbFontSizes.Items.Add(cbi);
            }

            if (foundFontSize == false)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Content = Settings.DefaultFontSize.ToString();
                cbi.IsSelected = true;
                cbFontSizes.Items.Insert(0, cbi);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
