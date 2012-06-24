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
using System.Windows.Xps.Packaging;
using UVOutliner.Export;

namespace UVOutliner
{
    /// <summary>
    /// Interaction logic for wnd_Export.xaml
    /// </summary>
    public partial class wnd_PrintPreview : Window
    {
        private MainWindow __MainWindow;
        public wnd_PrintPreview()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(wnd_Export_Loaded);            
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

        private void UpdateExportedDocument()
        {
            string documentAsXaml = ExportToXaml_Printing.ExportToXaml(Document, __MainWindow);                

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(documentAsXaml);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            FlowDocument document = XamlReader.Load(stream) as FlowDocument;

            string fileName = System.IO.Path.GetTempFileName() + ".xps";
            
            PrintHelpers.SaveAsXps(document, fileName, new Size(800, 1024));
            XpsDocument xpsDocument = new XpsDocument(fileName, FileAccess.ReadWrite);
            FixedDocumentSequence fixedDocSeq = xpsDocument.GetFixedDocumentSequence();
            //pDialog.PrintDocument(fixedDocSeq.DocumentPaginator, "UV Outliner document");

            DocumentViewer.Document = fixedDocSeq;
            PleaseWait.Visibility = Visibility.Collapsed;
        }

        public OutlinerDocument Document
        {
            get;
            set;
        }

        private string __DocumentAsXaml;
        public void UpdateExportAsRtf()
        {
            if (__DocumentAsXaml == null)
                __DocumentAsXaml = ExportToXaml_WordFriendly.ExportToXaml(Document, __MainWindow);                

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(__DocumentAsXaml);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            FlowDocument document = XamlReader.Load(stream) as FlowDocument;

            if (document != null)
            {
                DocumentViewer.Document = document;
            }

            DocumentViewer.Visibility = Visibility.Visible;
            PleaseWait.Visibility = Visibility.Hidden;
        }

    }
}
