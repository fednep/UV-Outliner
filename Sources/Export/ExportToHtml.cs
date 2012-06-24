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
using UVOutliner.Lib;

namespace UVOutliner.Export
{
    public class ExportToHtml
    {
        public static string Export(OutlinerDocument Document, MainWindow wnd)
        {
            StringBuilder writer = new StringBuilder();
            writer.AppendLine("<html>");
            writer.AppendLine("<head>");            
            writer.AppendLine("<meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" />");
            writer.AppendLine(String.Format("<title>{0}</title>", System.IO.Path.GetFileName(Document.FileName)));
            writer.AppendLine("<style>p {{margin:0px;}}");
            writer.AppendLine("  td {padding:0px;}");
            writer.AppendLine("  th {font-size:12px; align:right; font-family: Helvetica,Arial,sans-serif;}" );
            writer.AppendLine("  td#column{padding-left:4px;}");
            writer.AppendLine("  td#note{padding-left:4px;}");
            writer.AppendLine("  img#exp {padding-right:1px;}");
            writer.AppendLine("  img#checkbox {padding-right:1px;}");
            writer.AppendLine("  img#bul {padding-right:1px;}");
            writer.AppendLine("</style>");
            writer.AppendLine("</head>");
            writer.AppendLine("<body topmargin=0>\n");

            FlowDocument wholeDocument = new FlowDocument();
            wholeDocument.FontFamily = UVOutliner.Settings.DefaultFontFamily;
            wholeDocument.FontSize = UVOutliner.Settings.DefaultFontSize;

            List<OutlinerNote> linearList = new List<OutlinerNote>();
            DocumentHelpers.GetLinearNotesList(Document.RootNode, linearList, false);

            double totalWidth = 0;
            for (int i = 0; i < Document.ColumnDefinitions.Count; i++)
                totalWidth += Document.ColumnDefinitions[i].Width;

            writer.AppendLine("<table width='100%'>");
            int[] columnWidths = new int[wnd.OutlinerTreeColumns.Count];

            for (int i = 0; i < wnd.OutlinerTreeColumns.Count; i++)
                columnWidths[i] = (int)((Document.ColumnDefinitions[wnd.GetColumnIdByView(i)].Width / totalWidth) * 100);                

            // add column headers
            if (Document.ColumnDefinitions.Count > 1)
            {
                writer.AppendLine("<tr>");

                for (int i = 0; i < wnd.OutlinerTreeColumns.Count; i++)
                {
                    int columnId = wnd.GetColumnIdByView(i);
                    writer.Append( String.Format("<th width='{0}%'>", columnWidths[i]));
                    writer.Append(Document.ColumnDefinitions[columnId].ColumnName);
                    writer.AppendLine("</th>");
                }
                writer.AppendLine("</tr>");
            }

            // add all other columns
            for (int i = 0; i < linearList.Count; i++)
            {
                OutlinerNote note = linearList[i];
                double indent = (Math.Max(0, note.Level - 1)) * 20;
                string indent_str = ""; for (int t = 0; t < indent / 10; t++) indent_str += " ";

                if (note.IsEmpty)
                {
                    writer.AppendLine(indent_str + "<tr><td>&nbsp;</td></tr>");
                    continue;
                }

                writer.AppendLine(indent_str + "<tr>");
                for (int c = 0; c < wnd.OutlinerTreeColumns.Count; c++)
                {
                    int columnId = wnd.GetColumnIdByView(c);

                    FlowDocument flowDocument = linearList[i].Columns[columnId].ColumnData as FlowDocument;
                    if (flowDocument == null)
                    {
                        writer.AppendLine(indent_str + "<td></td>");
                        continue;
                    }

                    if (columnId != 0)
                    {
                        string style;
                        var html = OpenSave.HtmlFromReport(flowDocument, out style);
                        writer.AppendLine(String.Format(indent_str + "<td id=column style='{0}'>{1}</td>", style, html));
                    }
                    else
                    {
                        writer.AppendLine(indent_str + "<td>");
                        writer.AppendLine(indent_str + "  <table width='100%'>");
                        writer.AppendLine(indent_str + "  <tr><td>");
                        writer.Append(String.Format(indent_str + "      <nobr><div style='margin-left: {0}px;'>", indent));

                        if (note.SubNotes.Count == 0)
                            writer.Append("<img src='uvbul.png' id=bul width=14 height=14 alt='&bull;'>");
                        else
                        {
                            if (note.IsExpanded)
                                writer.Append("<img src='uvndexpa.png' id=exp width=14 height=14>");
                            else
                                writer.Append("<img src='uvndcol.png' id=exp width=14 height=14>");
                        }

                        if (Document.CheckboxesVisble)
                        {
                            if (note.IsChecked == true)
                                writer.Append("<img src='uvchboxch.png' id=checkbox width=14 height=14>");
                            else
                                writer.Append("<img src='uvchboxunch.png' id=checkbox width=14 height=14>");
                        }
                        writer.AppendLine("</div></nobr>");
                        string style;
                        var html = OpenSave.HtmlFromReport(flowDocument, out style);

                        writer.AppendLine(indent_str + String.Format("  </td><td width='100%' id=column style='{0}'>", style));
                        writer.AppendLine(indent_str + String.Format("    {0}", html));

                        writer.AppendLine(indent_str + "  </td></tr>");
                        if (note.HasInlineNote)
                        {
                            html = OpenSave.HtmlFromReport(note.InlineNoteDocument, out style);

                            writer.AppendLine(indent_str + "  <tr><td></td>");
                            writer.AppendLine(indent_str + string.Format("      <td id=note style='{0}'>", style));
                            writer.AppendLine(indent_str + String.Format("        {0}", html));
                            writer.AppendLine(indent_str + "      </td></tr>");
                        }

                        writer.AppendLine(indent_str + "  </table>");
                        writer.AppendLine(indent_str + "</td>");
                    }                    
                }
                writer.AppendLine(indent_str + "</tr>");
            }
            writer.AppendLine("</table></body></html>");

            return writer.ToString();
        }
    }
}
