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
using System.Windows;
using System.Windows.Markup;
using System.IO;

namespace UVOutliner.Columns
{
    public static class ColumnHelpers
    {
        public static OutlinerColumn CreateColumnClass(OutlinerNote note, OutlinerColumnDefinition definition)
        {
            switch (definition.DataType)
            {
                case ColumnDataType.RichText:                    
                    return new RichTextColumn(note);

                default:
                    return null;
            }
        }

        const string RichTextColumn = "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:l='clr-namespace:UVOutliner;assembly=uv'>" +
            "<Grid>" +
            "    <Grid.ColumnDefinitions>" +
            "        <ColumnDefinition Width=\"*\"/>" +
            "    </Grid.ColumnDefinitions>" +
            "                             " +
            "    <l:MyEdit Margin=\"0\"" +
            "          BorderBrush=\"{x:Null}\" " +
            "          AcceptsReturn=\"True\" " +
            "          IsUndoEnabled=\"True\" " +
            "          IsDocumentEnabled=\"True\" " +
            "          Name=\"PART_MyEdit\" " +
            "          Document=\"{Binding Path=Columns[#COLUMNINDEX].ColumnData, Mode=OneWay}\"  " +
            "          /> " +
            " </Grid> " +
            "</DataTemplate>";

        public static DataTemplate TemplateForColumn(MainWindow wnd, int idx, ColumnDataType type)
        {
            string template;
            if (type == ColumnDataType.RichText)
            {                
                template = RichTextColumn.Replace("#COLUMNINDEX", idx.ToString());
                MemoryStream stream = new MemoryStream();
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(template);
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                return (DataTemplate)XamlReader.Load(stream);
            }

            return null;
        }
    }
    
}
