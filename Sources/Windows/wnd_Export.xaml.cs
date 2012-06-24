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
using UVOutliner.Lib;
using System.Windows.Markup;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using UVOutliner.Export;
using Microsoft.Win32;
using mshtml;

namespace UVOutliner
{
    /// <summary>
    /// Interaction logic for wnd_Export.xaml
    /// </summary>
    public partial class wnd_Export : Window
    {
        private MainWindow __MainWindow;
        private List<string> __FilesToDelete = new List<string>();

        public wnd_Export()
        {
            InitializeComponent();

            ExportedRtf.FontFamily = UVOutliner.Settings.DefaultFontFamily;
            ExportedRtf.FontSize = UVOutliner.Settings.DefaultFontSize;

            Loaded += new RoutedEventHandler(wnd_Export_Loaded);
            Closed += new EventHandler(wnd_Export_Closed);

            ExportAsList.IsChecked = Settings.ExportListOnly;
            ExportAs.SelectedIndex = Settings.ExportLastSelectedFilter;
        }

        void wnd_Export_Closed(object sender, EventArgs e)
        {
            Settings.ExportListOnly = IsExportAsList();
            Settings.ExportLastSelectedFilter = ExportAs.SelectedIndex;
            Settings.SaveSettings();

            for (int i = 0; i < __FilesToDelete.Count; i++)
            {
                try
                {
                    File.Delete(__FilesToDelete[i]);
                }
                catch
                {

                }
            }
        }

        public void SetMainWindow(MainWindow mainWindow)            
        {
            __MainWindow = mainWindow;
        }

        DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
        void wnd_Export_Loaded(object sender, RoutedEventArgs e)
        {            
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            UpdateExportedDocument();            
        }

        public OutlinerDocument Document
        {
            get;
            set;
        }

        private string __DocumentAsXaml_List;
        private string __DocumentAsXaml;

        public void UpdateExportAsRtf()
        {


            if (IsExportAsList() == false && __DocumentAsXaml == null)
                __DocumentAsXaml = ExportToXaml_WordFriendly.ExportToXaml(Document, __MainWindow);
            else if (IsExportAsList() && __DocumentAsXaml_List == null)
                __DocumentAsXaml_List = OpenSave.ExportAsXAML_List(Document);
            

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            
            if (IsExportAsList())
                writer.Write(__DocumentAsXaml_List);
            else
                writer.Write(__DocumentAsXaml);

            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            FlowDocument document = XamlReader.Load(stream) as FlowDocument;

            if (document != null)
            {
                ExportedRtf.Document = document;
            }

            Browser.Visibility = Visibility.Hidden;
            ExportedText.Visibility = Visibility.Hidden;
            ExportedRtf.Visibility = Visibility.Visible;
            PleaseWait.Visibility = Visibility.Hidden;
        }

        private bool IsExportAsList()
        {
            return ExportAsList.IsChecked == true;
        }

        private string __DocumentAsHtml = null;
        private string __DocumentAsHtml_List = null;

        private void UpdateExportAsHtml()
        {
            string data = "";
            if (IsExportAsList() == false)
            {
                if (__DocumentAsHtml == null)
                    __DocumentAsHtml = OpenSave.ExportAsHtml(Document, __MainWindow);

                data = __DocumentAsHtml;
            }
            else if (IsExportAsList() == true)
            {
                if (__DocumentAsHtml_List == null)
                    __DocumentAsHtml_List = OpenSave.ExportAsHtml_List(Document);

                data = __DocumentAsHtml_List;
            }

            var files = SaveImageFiles(System.IO.Path.GetTempPath());
            __FilesToDelete.AddRange(files);

            string tmp = System.IO.Path.GetTempFileName();
            string tempFile = tmp + ".html";
            __FilesToDelete.Add(tmp);
            __FilesToDelete.Add(tempFile);

            StreamWriter writer = new StreamWriter(tempFile);
            writer.Write(data);                       
            writer.Close();

            ExportedText.Text = data;

            Browser.Navigate("file:///" + tempFile);
            Browser.Visibility = Visibility.Visible;
            ExportedText.Visibility = Visibility.Hidden;
            ExportedRtf.Visibility = Visibility.Hidden;
            PleaseWait.Visibility = Visibility.Hidden;
        }

        private string[] SaveImageFiles(string path)
        {
            List<string> files = new List<string>();

            var fn = System.IO.Path.Combine(path, "uvbul.png");
            res.Resources.bullet.Save(fn);
            files.Add(fn);

            fn = System.IO.Path.Combine(path, "uvchboxch.png");
            files.Add(fn);
            res.Resources.checkbox_checked.Save(fn);

            fn = System.IO.Path.Combine(path, "uvchboxunch.png");
            files.Add(fn);
            res.Resources.checkbox_unchecked.Save(fn);

            fn = System.IO.Path.Combine(path, "uvndcol.png");
            files.Add(fn);
            res.Resources.node_collapsed.Save(fn);

            fn = System.IO.Path.Combine(path, "uvndexpa.png");
            files.Add(fn);
            res.Resources.node_expanded.Save(fn);

            return files.ToArray();
        }

        private string __DocumentAsText;

        private void UpdateExportAsText()
        {
            if (__DocumentAsText == null)
                __DocumentAsText = OpenSave.ExportAsTXT(Document, __MainWindow);
            
            ExportedText.Text = __DocumentAsText;
            ExportedText.Visibility = Visibility.Visible;
            Browser.Visibility = Visibility.Hidden;
            ExportedRtf.Visibility = Visibility.Hidden;
            PleaseWait.Visibility = Visibility.Hidden;
        }
       
        private void ExportAs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExportedDocument();
        }

        private void UpdateExportedDocument()
        {
            if (Document == null)
                return;

            if (ExportAsRTF.IsSelected)
                UpdateExportAsRtf();
            else if (ExportAsText.IsSelected)
                UpdateExportAsText();
            else if (ExportAsHtml.IsSelected)
                UpdateExportAsHtml();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (ExportedRtf.IsVisible)
            {
                TextPointer cursor = ExportedRtf.CaretPosition;

                ExportedRtf.SelectAll();
                ExportedRtf.Copy();

                ExportedRtf.CaretPosition = cursor;
            }

            if (ExportedText.IsVisible)
            {
                int caretIndex = ExportedText.CaretIndex;
                ExportedText.SelectAll();
                ExportedText.Copy();

                ExportedText.CaretIndex = caretIndex;
            }

            if (Browser.IsVisible)
            {
                HTMLDocument doc = (HTMLDocument)Browser.Document;
                if (doc == null)
                    return;

                doc.execCommand("SelectAll");
                doc.execCommand("Copy");
                doc.execCommand("Unselect");
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();            
            sfd.Filter = "Rich text format (*.rtf)|*.rtf|Text document (*.txt)|*.txt|HTML document (*.html)|*.html";


            if (ExportAsRTF.IsSelected)
                sfd.FilterIndex = 1;
            else if (ExportAsText.IsSelected)
                sfd.FilterIndex = 2;
            else if (ExportAsHtml.IsSelected)
                sfd.FilterIndex = 3;

            if (sfd.ShowDialog() == true)
            {

                try
                {
                    string extension = System.IO.Path.GetExtension(sfd.FileName).ToLower();
                    if (extension == ".txt")
                        OpenSave.ExportAsTXT(Document, sfd.FileName);
                    else
                        if (extension == ".html" || extension == ".htm")
                        {
                            if (!IsExportAsList())
                                SaveImageFiles(System.IO.Path.GetDirectoryName(sfd.FileName));
                            OpenSave.ExportAsHtml(Document, __MainWindow, sfd.FileName, IsExportAsList());
                        }
                        else
                            if (extension == ".rtf")
                                OpenSave.ExportAsRtf(Document, __MainWindow, sfd.FileName, IsExportAsList());
                            else
                                if (extension == ".xaml")
                                    OpenSave.ExportAsXAML(Document, __MainWindow, sfd.FileName, IsExportAsList());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while exporting document: " + ex.Message, "Error Exporting Document", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportAsList_Checked(object sender, RoutedEventArgs e)
        {
            UpdateExportedDocument();
        }

        private void ExportAsList_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateExportedDocument();
        }
    }
}
