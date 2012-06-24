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
using System.Windows.Documents;
using System.Windows;
using System.Windows.Markup;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace UVOutliner.Export
{
    class ExportToXaml_WordFriendly
    {
        public static string ExportToXaml(OutlinerDocument Document, MainWindow wnd)
        {
            if (wnd.OutlinerTreeColumns.Count > 1)
                return ExportToXaml_MultiColumn(Document, wnd);
            else
                return ExportToXaml_MultiColumn(Document, wnd);
        }

        public static string ExportToXaml_MultiColumn(OutlinerDocument Document, MainWindow wnd)
        {
            FlowDocument wholeDocument = new FlowDocument();
            wholeDocument.FontFamily = UVOutliner.Settings.DefaultFontFamily;
            wholeDocument.FontSize = UVOutliner.Settings.DefaultFontSize;

            List<OutlinerNote> linearList = new List<OutlinerNote>();
            DocumentHelpers.GetLinearNotesList(Document.RootNode, linearList, false);

            double totalWidth = 0;
            for (int i = 0; i < Document.ColumnDefinitions.Count; i++)
                totalWidth += Document.ColumnDefinitions[i].Width;


            //temporary
            //pageWidth = totalWidth;
            // just random
            if (totalWidth == 0)
                totalWidth = 300;

            Table table = new Table();
            table.LineHeight = 1;
            table.Margin = new Thickness(0);
            table.Padding = new Thickness(0);
            wholeDocument.Blocks.Add(table);
            for (int i = 0; i < wnd.OutlinerTreeColumns.Count; i++)
            {
                int columnId = wnd.GetColumnIdByView(i);
                TableColumn column = new TableColumn();
                //column.Width = new GridLength(Document.ColumnDefinitions[columnId].Width * pageWidth / totalWidth, GridUnitType.Pixel);
                column.Width = new GridLength(Document.ColumnDefinitions[columnId].Width / totalWidth, GridUnitType.Star);                
                //column.Width = new GridLength(0, GridUnitType.Auto);
                table.Columns.Add(column);
            }

            // add column headers
            if (Document.ColumnDefinitions.Count > 1)
            {
                TableRowGroup rowGroup = new TableRowGroup();
                table.RowGroups.Add(rowGroup);

                TableRow row = new TableRow();
                rowGroup.Rows.Add(row);
                for (int i = 0; i < wnd.OutlinerTreeColumns.Count; i++)
                {
                    int columnId = wnd.GetColumnIdByView(i);
                    row.Cells.Add(new TableCell(new Paragraph(new Run(Document.ColumnDefinitions[columnId].ColumnName))));
                }
            }

            TableRowGroup mainRowGroup = new TableRowGroup();
            table.RowGroups.Add(mainRowGroup);

            // add all other columns
            for (int i = 0; i < linearList.Count; i++)
            {
                TableRow tableRow = new TableRow();
                OutlinerNote note = linearList[i];

                if (note.IsEmpty)
                {
                    tableRow.Cells.Add(new TableCell(new Paragraph()));
                    mainRowGroup.Rows.Add(tableRow);
                    continue;
                }

                for (int c = 0; c < wnd.OutlinerTreeColumns.Count; c++)
                {
                    int columnId = wnd.GetColumnIdByView(c);

                    FlowDocument flowDocument = linearList[i].Columns[columnId].ColumnData as FlowDocument;
                    if (flowDocument == null)
                    {
                        tableRow.Cells.Add(new TableCell());
                        continue;
                    }

                    double indent = (Math.Max(0, note.Level - 1)) * 20;

                    TableCell cell = new TableCell();
                    tableRow.Cells.Add(cell);

                    Section section = XamlReader.Load(ExportToXaml_Printing.TransformFlowDocumentToSection(flowDocument)) as Section;
                    if (section == null)
                        continue;

                    if (columnId != 0)
                        cell.Blocks.Add(section);
                    else
                    {
                        Paragraph para = new Paragraph();
                        para.Margin = new Thickness(indent, 0, 0, 0);

                        Image expImage = new Image();
                        expImage.Margin = new Thickness(0, 2, 0, 0);
                        expImage.Width = 14;
                        expImage.Height = 14;
                        expImage.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);

                        BitmapImage bImage = new BitmapImage();

                        if (note.SubNotes.Count == 0)
                            bImage.UriSource = new Uri("pack://application:,,,/uv;component/res/bullet.png");
                        else
                        {
                            if (note.IsExpanded)
                                bImage.UriSource = new Uri("pack://application:,,,/uv;component/res/node_expanded.png");
                            else
                                bImage.UriSource = new Uri("pack://application:,,,/uv;component/res/node_collapsed.png");
                        }

                        expImage.Source = bImage;
                        expImage.Stretch = Stretch.None;
                        para.Inlines.Add(expImage);

                        if (Document.CheckboxesVisble)
                        {
                            Image checkBox = new Image();
                            checkBox.Margin = new Thickness(0, 2, 0, 0);
                            checkBox.Width = 14;
                            checkBox.Height = 14;
                            checkBox.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);

                            var checkSource = new BitmapImage();
                            if (note.IsChecked == true)
                                checkSource.UriSource = new Uri("pack://application:,,,/uv;component/res/checkbox_checked.png");
                            else
                                checkSource.UriSource = new Uri("pack://application:,,,/uv;component/res/checkbox_unchecked.png");

                            checkBox.Source = checkSource;
                            checkBox.Stretch = Stretch.None;
                            para.Inlines.Add(checkBox);
                        }

                        Paragraph par = new Paragraph();

                        Figure fig1 = new Figure();
                        fig1.Margin = new Thickness(0);
                        fig1.HorizontalAnchor = FigureHorizontalAnchor.PageLeft;
                        fig1.Blocks.Add(para);

                        Figure fig2 = new Figure();
                        fig2.Margin = new Thickness(0);
                        fig2.HorizontalAnchor = FigureHorizontalAnchor.PageLeft;
                        fig2.Blocks.Add(section);

                        par.Inlines.Add(fig1);
                        par.Inlines.Add(fig2);

                        cell.Blocks.Add(par);
                    }
                }

                mainRowGroup.Rows.Add(tableRow);
                if (note.HasInlineNote)
                    ExportToXaml_Printing.AddInlineNote(mainRowGroup.Rows, note, true);
            }


            MemoryStream outStream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(outStream, settings);
            XamlDesignerSerializationManager manager = new XamlDesignerSerializationManager(writer);
            XamlWriter.Save(wholeDocument, manager);

            outStream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(outStream);
            return reader.ReadToEnd();
        }

        public static string ExportToXaml_SingleColumn(OutlinerDocument Document, MainWindow wnd)
        {
            FlowDocument wholeDocument = new FlowDocument();
            wholeDocument.FontFamily = UVOutliner.Settings.DefaultFontFamily;
            wholeDocument.FontSize = UVOutliner.Settings.DefaultFontSize;

            List<OutlinerNote> linearList = new List<OutlinerNote>();
            DocumentHelpers.GetLinearNotesList(Document.RootNode, linearList, false);


            // add all other columns
            for (int i = 0; i < linearList.Count; i++)
            {
                OutlinerNote note = linearList[i];

                if (note.IsEmpty)
                {
                    wholeDocument.Blocks.Add(new Paragraph());
                    continue;
                }


                FlowDocument flowDocument = linearList[i].Columns[0].ColumnData as FlowDocument;
                if (flowDocument == null)
                    continue;

                Section section = XamlReader.Load(ExportToXaml_Printing.TransformFlowDocumentToSection(flowDocument)) as Section;
                if (section == null)
                    continue;

                double indent = (Math.Max(0, note.Level - 1)) * 20;
                Paragraph para = new Paragraph();
                para.Margin = new Thickness(indent, 0, 0, 0);
                Image expImage = new Image();
                expImage.Margin = new Thickness(0, 2, 0, 0);
                expImage.Width = 14;
                expImage.Height = 14;
                expImage.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);

                BitmapImage bImage = new BitmapImage();

                if (note.SubNotes.Count == 0)
                    bImage.UriSource = new Uri("pack://application:,,,/uv;component/res/bullet.png");
                else
                {
                    if (note.IsExpanded)
                        bImage.UriSource = new Uri("pack://application:,,,/uv;component/res/node_expanded.png");
                    else
                        bImage.UriSource = new Uri("pack://application:,,,/uv;component/res/node_collapsed.png");
                }

                expImage.Source = bImage;
                expImage.Stretch = Stretch.None;
                para.Inlines.Add(expImage);

                if (Document.CheckboxesVisble)
                {
                    Image checkBox = new Image();                        
                    checkBox.Margin = new Thickness(0, 2, 0, 0);
                    checkBox.Width = 14;
                    checkBox.Height = 14;
                    checkBox.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);

                    var checkSource = new BitmapImage();
                    if (note.IsChecked == true)
                        checkSource.UriSource = new Uri("pack://application:,,,/uv;component/res/checkbox_checked.png");
                    else
                        checkSource.UriSource = new Uri("pack://application:,,,/uv;component/res/checkbox_unchecked.png");

                    checkBox.Source = checkSource;
                    checkBox.Stretch = Stretch.None;
                    para.Inlines.Add(checkBox);
                }

                Paragraph par = new Paragraph();
                Figure fig1 = new Figure();
                fig1.Margin = new Thickness(0);
                fig1.Padding = new Thickness(0);
                fig1.HorizontalAnchor = FigureHorizontalAnchor.PageLeft;
                fig1.Blocks.Add(para);

                Figure fig2 = new Figure();
                fig2.Margin = new Thickness(0);
                fig2.Padding = new Thickness(0);
                fig2.HorizontalAnchor = FigureHorizontalAnchor.PageLeft;
                fig2.Blocks.Add(section);

                par.Inlines.Add(fig1);
                par.Inlines.Add(fig2);                
                wholeDocument.Blocks.Add( new Section(par));                    
            }


            MemoryStream outStream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            //settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(outStream, settings);
            XamlDesignerSerializationManager manager = new XamlDesignerSerializationManager(writer);
            XamlWriter.Save(wholeDocument, manager);

            outStream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(outStream);
            return reader.ReadToEnd();
        }
    }
}
