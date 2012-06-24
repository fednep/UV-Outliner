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
using System.IO;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Xps.Packaging;
using System.IO.Packaging;
using System.Windows.Xps.Serialization;
using System.Windows.Media;

namespace UVOutliner
{

    public class DocumentPaginatorWrapper : DocumentPaginator
    {
        Size m_PageSize;
        Size m_Margin;
        DocumentPaginator m_Paginator;
        Typeface m_Typeface;

        public DocumentPaginatorWrapper(DocumentPaginator paginator, Size pageSize, Size margin)
        {
            m_PageSize = pageSize;
            m_Margin = margin;
            m_Paginator = paginator;
            m_Paginator.PageSize = new Size(m_PageSize.Width - margin.Width * 2,
                                            m_PageSize.Height - margin.Height * 2);
        }

        Rect Move(Rect rect)
        {
            if (rect.IsEmpty)
            {
                return rect;
            }
            else
            {
                return new Rect(rect.Left + m_Margin.Width, rect.Top + m_Margin.Height,
                                rect.Width, rect.Height);
            }
        }

        public override DocumentPage GetPage(int pageNumber)
        {

            DocumentPage page = m_Paginator.GetPage(pageNumber);
            // Create a wrapper visual for transformation and add extras

            ContainerVisual newpage = new ContainerVisual();
            DrawingVisual title = new DrawingVisual();
            using (DrawingContext ctx = title.RenderOpen())
            {
                if (m_Typeface == null)
                {
                    m_Typeface = new Typeface("Times New Roman");
                }

                FormattedText text = new FormattedText("Page " + (pageNumber + 1),
                    System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    m_Typeface, 14, Brushes.Black);
                ctx.DrawText(text, new Point(0, -96 / 4)); // 1/4 inch above page content
            }

            ContainerVisual smallerPage = new ContainerVisual();
            smallerPage.Children.Add(page.Visual);
            smallerPage.Transform = new MatrixTransform(0.95, 0, 0, 0.95,
                0.025 * page.ContentBox.Width, 0.025 * page.ContentBox.Height);

            newpage.Children.Add(smallerPage);
            newpage.Children.Add(title);
            newpage.Transform = new TranslateTransform(m_Margin.Width, m_Margin.Height);

            return new DocumentPage(newpage, m_PageSize, Move(page.BleedBox), Move(page.ContentBox));
        }

        public override bool IsPageCountValid
        {
            get
            {
                return m_Paginator.IsPageCountValid;
            }
        }

        public override int PageCount
        {
            get
            {
                return m_Paginator.PageCount;
            }
        }


        public override Size PageSize
        {
            get
            {
                return m_Paginator.PageSize;
            }

            set
            {
                m_Paginator.PageSize = value;
            }
        }

        public override IDocumentPaginatorSource Source
        {
            get
            {
                return m_Paginator.Source;
            }
        }
    }

    public class PrintHelpers
    {

        public static int SaveAsXps(FlowDocument doc, string fileName, Size printableArea)
        {
            doc.ColumnWidth = printableArea.Width;
            using (Package container = Package.Open(fileName, FileMode.Create))
            {
                using (XpsDocument xpsDoc = new XpsDocument(container, CompressionOption.Maximum))
                {
                    XpsSerializationManager rsm = new XpsSerializationManager(new XpsPackagingPolicy(xpsDoc), false);
                    DocumentPaginator paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                    paginator.ComputePageCount();
                    
                    DocumentPaginator newPaginator = new DocumentPaginatorWrapper(
                        paginator,
                        printableArea, new Size(8, 8));

                    rsm.SaveAsXaml(paginator);
                }
            }

            return 0;
        }


        internal static void PrintReport(FlowDocument flowDocument)
        {
            bool shouldPrint = false;
            PrintDialog pDialog = null;
            
            // Create the print dialog object and set options
            pDialog = new PrintDialog();                    
            pDialog.PageRangeSelection = PageRangeSelection.AllPages;
            pDialog.UserPageRangeEnabled = true;

            // Display the dialog. This returns true if the user presses the Print button.
            shouldPrint = (bool)pDialog.ShowDialog();

            string fileName = System.IO.Path.GetTempFileName() + ".xps";            
            if (shouldPrint && pDialog != null)
            {
                SaveAsXps(flowDocument, fileName, new Size(800, 1024));
                XpsDocument xpsDocument = new XpsDocument(fileName, FileAccess.ReadWrite);
                FixedDocumentSequence fixedDocSeq = xpsDocument.GetFixedDocumentSequence();
                pDialog.PrintDocument(fixedDocSeq.DocumentPaginator, "Atola Insight report print");
            }
        }
    }
}
