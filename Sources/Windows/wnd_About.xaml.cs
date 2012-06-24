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
using System.Diagnostics;

namespace UVOutliner
{
    /// <summary>
    /// Interaction logic for wnd_About.xaml
    /// </summary>
    public partial class wnd_About : Window
    {
        public static RoutedCommand CopyEmailToClipboardCommand = new RoutedCommand();
        public static RoutedCommand StartEmailProgram = new RoutedCommand();

        public wnd_About()
        {
            InitializeComponent();
            DataContext = this;

            TextRange range = new TextRange(Version.ContentStart, Version.ContentEnd);
            range.Text = app.CurrentVersion.ToString(3);
        }

        private void Buy_Click(object sender, RoutedEventArgs e)
        {
            UrlHelpers.OpenInBrowser("http://uvoutliner.com/purchase/");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            UrlHelpers.OpenInBrowser("http://uvoutliner.com");
        }

        private void Email_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("mailto:fedir@uvoutliner.com");
            }
            catch
            {
                MessageBox.Show("Error opening mail program. Is seems that no default mail client is installed.");            
            }
        }

        public Visibility ProVersionVisible
        {
            get
            {
                    return Visibility.Collapsed;
            }
        }

    }
}
