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
using System.Xml;
using System.Windows.Markup;
using System.Windows.Documents;
using System.IO;
using System.Windows;
using UVOutliner.Columns;
using DragDropListBox;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UVOutliner.Export;
using UVOutliner.Styles;

namespace UVOutliner.Lib
{
    public class OpenSave
    {
        public const string FileFilter = "Outliner files (*.uvxml) | *.uvxml";

        public static bool SaveFile(MainWindow wnd, OutlinerDocument rnl, string fileName)
        {
            rnl.FileName = fileName;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;          
            XmlWriter wr = XmlWriter.Create(fileName, settings);            
            // TODO: Write header           
            wr.WriteStartElement("Body");
            wr.WriteAttributeString("CheckboxesVisible", rnl.CheckboxesVisble.ToString());

            wr.WriteAttributeString("OddBackgroundColor", rnl.OddBackgroundColor.Color.ToString());
            wr.WriteAttributeString("EvenBackgroundColor", rnl.EvenBackgroundColor.Color.ToString());
            wr.WriteAttributeString("UseRowSeparator", rnl.LinesBetweenRows ? "true" :"false");
            wr.WriteAttributeString("RowSeparatorColor", rnl.LinesBetweenRowsBrush.Color.ToString());

            if (rnl.ShowInspectors == false)
                wr.WriteAttributeString("ShowInspectors", rnl.ShowInspectors.ToString());

            wr.WriteStartElement("Styles");
            for (int i = 0; i < rnl.Styles.Count; i++)
                SaveStyle(wr, rnl.Styles[i]);
            wr.WriteEndElement();

            string[] id = new string[rnl.ColumnDefinitions.Count];
            for (int i = 0; i < rnl.ColumnDefinitions.Count; i++)
                id[i] = wnd.GetViewColumnId(i).ToString();

            wr.WriteStartElement("ColumnDefinitions");
            wr.WriteAttributeString("Order", String.Join(";", id));
            for (int i = 0; i < rnl.ColumnDefinitions.Count; i++)
                SaveColumnDefinition(wr, rnl.ColumnDefinitions[i]);            
            wr.WriteEndElement();            

            for (int i = 0; i < rnl.Count; i++)
                SaveRecursive(wr, rnl[i]);
            wr.WriteEndElement();
            wr.Close();

            rnl.DocumentSaved();
            return true;
        }

        private static void SaveColumnDefinition(XmlWriter wr, OutlinerColumnDefinition columnDefinition)
        {
            wr.WriteStartElement("ColumnDefinition");
            wr.WriteAttributeString("Name", columnDefinition.ColumnName);
            wr.WriteAttributeString("Type", ((int)columnDefinition.DataType).ToString());
            wr.WriteAttributeString("Width", (columnDefinition.Width).ToString());
            wr.WriteEndElement();
        }

        private static void SaveStyle(XmlWriter wr, BaseStyle outlinerStyle)
        {
            wr.WriteStartElement("Style");
            
            wr.WriteAttributeString("Type", outlinerStyle.StyleType.ToString());
            switch (outlinerStyle.StyleType)
            {
                case StyleType.Level:            
                    wr.WriteAttributeString("Level", ((LevelStyle)outlinerStyle).Level.ToString());                    
                    break;
                case StyleType.WholeDocument:
                    wr.WriteAttributeString("Level", ((LevelStyle)outlinerStyle).Level.ToString());                    
                    break;
            }

            for (int i = 0; i < outlinerStyle.Properties.Count; i++)
            {
                wr.WriteAttributeString(
                    OutlinerStyles.StylePropertyTypesToString[outlinerStyle.Properties[i].PropertyType],
                    outlinerStyle.Properties[i].Value.ToString());
            }

            wr.WriteEndElement();
        }

        public static void CopyToClipboard(OutlinerNote note)
        {
            MemoryStream clipboardStream = new MemoryStream();

            XmlWriter wr = XmlWriter.Create(clipboardStream);
            // TODO: Write header
            wr.WriteStartElement("UVOutlinerClipboard");
            SaveRecursive(wr, note);
            wr.WriteEndElement();
            wr.Close();

            Clipboard.SetData("uvoutlinerdata", Encoding.UTF8.GetString((clipboardStream as MemoryStream).ToArray()));
        }

        public static OutlinerNote PasteFromClipboard(OutlinerNote insertAfterThisNote)
        {
            OutlinerNote note = null;
            OutlinerNote parentNode = insertAfterThisNote.Parent;
            int insertionIndex = insertAfterThisNote.Parent.SubNotes.IndexOf(insertAfterThisNote) + 1;

            if (Clipboard.ContainsData("uvoutlinerdata"))
            {
                try
                {
                    string data = (string)Clipboard.GetData("uvoutlinerdata");
                    if (data == null || data == "")
                        return null;

                    byte[] clipboard = Encoding.UTF8.GetBytes(data);

                    MemoryStream stream = new MemoryStream();
                    stream.Write(clipboard, 0, clipboard.Length);
                    stream.Seek(0, SeekOrigin.Begin);

                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.CheckCharacters = false;
                    XmlReader reader = XmlReader.Create(stream, settings);

                    reader.ReadStartElement("UVOutlinerClipboard");
                    //reader.Read();
                    XmlReader subtree = reader.ReadSubtree();
                    note = ReadRecustive(subtree, parentNode);
                    subtree.Close();

                    if (note != null)
                        parentNode.SubNotes.Insert(insertionIndex, note);

                    reader.Close();
                }
                catch
                {
                    // do nothing
                }

            }
            else if (Clipboard.ContainsData(DataFormats.Text))
            {
                note = new OutlinerNote(parentNode);
                FlowDocument firstColumnDocument = (FlowDocument)(note.Columns[0].ColumnData);
                TextRange range = new TextRange(firstColumnDocument.ContentStart, firstColumnDocument.ContentEnd);
                range.Text = Clipboard.GetText();
                parentNode.SubNotes.Insert(insertionIndex, note);
            }

            return note;
        }

        private static void SaveRecursive(XmlWriter wr, OutlinerNote outlinerNote)
        {
            wr.WriteStartElement("Outline");            
            wr.WriteAttributeString("IsExpanded", outlinerNote.IsExpanded.ToString());            
            wr.WriteAttributeString("IsChecked", outlinerNote.IsChecked.ToString());
            wr.WriteStartElement("Columns");

            // Save all columns            
            for (int i = 0; i < outlinerNote.Columns.Count; i++)            
                outlinerNote.Columns[i].Save(wr);            
            wr.WriteEndElement();

            if (outlinerNote.HasInlineNote)
                SaveInlineNote(wr, outlinerNote);

            for (int i = 0; i < outlinerNote.SubNotes.Count; i++)
                SaveRecursive(wr, outlinerNote.SubNotes[i]);
            wr.WriteEndElement();
        }

        private static void SaveInlineNote(XmlWriter wr, OutlinerNote outlinerNote)
        {
            wr.WriteStartElement("InlineNote");
            wr.WriteRaw(XamlWriter.Save(outlinerNote.InlineNoteDocument));
            wr.WriteEndElement();
        }

        internal static OutlinerDocument OpenFile(string fileName)
        {            
            OutlinerDocument rnl = new OutlinerDocument();            
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.CheckCharacters = false;
            XmlReader reader = XmlReader.Create(fileName, settings);

            while (reader.Name != "Body" && !reader.EOF)
                reader.Read();

            if (reader.EOF)
                throw new Exception("Document read error.");

            //reader.ReadStartElement("Body");
            string showCheckboxes = reader.GetAttribute("CheckboxesVisible");
            if (showCheckboxes != null)
                rnl.CheckboxesVisble = bool.Parse(showCheckboxes);

            ReadDocumentStyleAttributes(rnl, reader);

            string showInspectors = reader.GetAttribute("ShowInspectors");
            if (showInspectors == null)
                rnl.ShowInspectors = true;
            else
            {
                rnl.ShowInspectors = bool.Parse(showInspectors);
            }

            while (reader.Name != "Styles" && reader.Name != "Outline" && reader.Name != "ColumnDefinitions" && !reader.EOF)
                reader.Read();

            if (reader.Name == "Styles" || reader.Name == "ColumnDefinitions")
            {
                if (reader.Name == "Styles")
                {
                    ReadStyles(reader, rnl);

                    while (reader.Name != "Outline" && reader.Name != "ColumnDefinitions" && !reader.EOF)
                        reader.Read();

                    if (reader.Name == "ColumnDefinitions")
                        ReadColumnDefinitions(rnl, reader);
                }
                else if (reader.Name == "ColumnDefinitions")
                    ReadColumnDefinitions(rnl, reader);
            }
            
            // For document, which has no ColumnDefinitions, "Outliner" will be the current node
            //  otherwise: "/ColumnDefinitions"
            if (reader.Name == "Outline")
                ReadOutlinerRow(rnl, reader);
            
            while (reader.ReadToFollowing("Outline"))
                ReadOutlinerRow(rnl, reader);
            
            reader.Close();
            rnl.FileName = fileName;
            return rnl;
        }

        private static void ReadDocumentStyleAttributes(OutlinerDocument rnl, XmlReader reader)
        {
            if (reader.GetAttribute("OddBackgroundColor") != null)
                rnl.OddBackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    reader.GetAttribute("OddBackgroundColor")));

            if (reader.GetAttribute("EvenBackgroundColor") != null)
                rnl.EvenBackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    reader.GetAttribute("EvenBackgroundColor")));

            if (reader.GetAttribute("RowSeparatorColor") != null)
                rnl.LinesBetweenRowsBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    reader.GetAttribute("RowSeparatorColor")));

            string useRowSeparator = reader.GetAttribute("UseRowSeparator");
            if (useRowSeparator != null)
                rnl.LinesBetweenRows = bool.Parse(useRowSeparator);
        }

        private static void ReadColumnDefinitions(OutlinerDocument document, XmlReader reader)
        {
            string order = reader.GetAttribute("Order");
            if (order != null)
                document.ColumnDefinitions.OrderOnInit = order;

            document.ColumnDefinitions.Clear();
            XmlReader subtree = reader.ReadSubtree();
            while (subtree.ReadToFollowing("ColumnDefinition"))
            {
                string name = reader.GetAttribute("Name");
                ColumnDataType type = (ColumnDataType)int.Parse(reader.GetAttribute("Type"));
                
                double width = 100;
                string width_str = reader.GetAttribute("Width");
                try
                {
                    width = double.Parse(width_str);
                }
                catch
                {
                    
                    if (width_str.Contains("."))
                        width = double.Parse(reader.GetAttribute("Width").Replace(".", ","));
                    else if (width_str.Contains(","))
                        width = double.Parse(reader.GetAttribute("Width").Replace(",", "."));
                }

                if (name != null)
                {
                    OutlinerColumnDefinition columnDefinition = new OutlinerColumnDefinition(name, type);
                    columnDefinition.Width = width;
                    document.ColumnDefinitions.Add(columnDefinition);
                }
            }
        }

        private static void ReadOutlinerRow(OutlinerDocument rnl, XmlReader reader)
        {
            XmlReader subtree = reader.ReadSubtree();
            OutlinerNote note = ReadRecustive(subtree, rnl.RootNode);
            subtree.Close();
            if (note != null)
                rnl.Add(note);
        }

        private static void ReadStyles(XmlReader reader,OutlinerDocument rnl)
        {
            XmlReader subtree = reader.ReadSubtree();
            while (subtree.ReadToFollowing("Style"))
            {
                if (subtree.MoveToFirstAttribute())
                {
                    string type = "Level";
                    int level = -1;
                    if (subtree.Name == "Type")
                    {
                        type = subtree.Value;
                        if (type == "Level")
                            subtree.MoveToNextAttribute();
                    }

                    if (type == "Level")
                    {
                        if (subtree.Name != "Level")
                            continue;
                        
                        try
                        {
                            level = int.Parse(subtree.Value);
                        }
                        catch
                        {
                            continue;
                        }
                    }                    

                    BaseStyle style;

                    if (type == "Inline")
                        style = rnl.Styles.InlineNoteStyle;
                    else
                    {
                        if (level <= 0)
                            style = rnl.Styles.WholeDocumentStyle;
                        else
                            style = rnl.Styles.GetStyleForLevel(level);
                    }

                    while (subtree.MoveToNextAttribute())
                    {
                        if (OutlinerStyles.StringToStylePropertyTypes.ContainsKey(subtree.Name))
                        {
                            StylePropertyType propertyType = OutlinerStyles.StringToStylePropertyTypes[subtree.Name];
                            object value = LevelStyle.GetValueFromString(propertyType, subtree.Value);

                            if (value != null)
                                style.AddProperty(propertyType, value);
                        }
                    }                    
                }
            }
            
        }

        private static OutlinerNote ReadRecustive(XmlReader reader, OutlinerNote parent)
        {
            OutlinerNote note = new OutlinerNote(parent);
            note.LastStyleApplied = note.Document.Styles.GetStyleForLevel(note.Level);
            reader.Read();
            string res = reader.GetAttribute("IsExpanded");
            note.IsExpanded = bool.Parse(res);            

            string isChecked = reader.GetAttribute("IsChecked");
            if (isChecked != null && isChecked != "")
                note.IsChecked = bool.Parse(isChecked);

            reader.MoveToElement();
            if (reader.ReadToFollowing("Columns") == false)
                throw new OpenFileException("No column tag found");

            XmlReader subtree = reader.ReadSubtree();
            ReadColumns(subtree, note);
            subtree.Close();

            while (reader.Read())
            {
                if (reader.Name == "InlineNote" && reader.IsStartElement())
                    ReadInlineNote(reader, note);

                if (reader.Name == "Outline" && reader.IsStartElement())
                {
                    subtree = reader.ReadSubtree();
                    OutlinerNote subNote = ReadRecustive(subtree, note);
                    subtree.Close();

                    note.SubNotes.Add(subNote);
                }
            }            

            return note;
        }

        private static void ReadInlineNote(XmlReader reader, OutlinerNote note)
        {
            reader.ReadToFollowing("FlowDocument");
            XmlReader xmlReader = reader.ReadSubtree();            
            FlowDocument inlineDoc = (FlowDocument)XamlReader.Load(xmlReader);
            note.CreateInlineNote(inlineDoc);
            xmlReader.Close();
        }

        private static void ReadColumns(XmlReader reader, OutlinerNote note)
        {
            for (int i = 0; i < note.Columns.Count; i++)
                note.Columns[i].Load(reader);
        }

        internal static string ExportAsHtml(OutlinerDocument Document, MainWindow mainWindow)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(ExportToHtml.Export(Document, mainWindow));
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }


        internal static string ExportAsHtml_List(OutlinerDocument Document)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            ExportAsHtml_List(Document, writer);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void ExportAsHtml_List(OutlinerDocument Document, StreamWriter writer)
        {
            
            writer.WriteLine("<html>");
            writer.WriteLine("<head>");
            writer.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" />");
            writer.WriteLine("<title>{0}</title>", System.IO.Path.GetFileName(Document.FileName));
            writer.WriteLine("<style>p {{margin:0px;}}</style>", System.IO.Path.GetFileName(Document.FileName));
            writer.WriteLine("</head>");
            writer.WriteLine("<body topmargin=0>\n");

            DumpHTMLRecursive(writer, Document.RootNode);

            writer.WriteLine("\n</body>");
            writer.WriteLine("</html>");
        }


        internal static void ExportAsHtml(OutlinerDocument Document, MainWindow wnd, string fileName, bool exportAsList)
        {
            if (exportAsList == true)
            {
                StreamWriter writer = new StreamWriter(fileName);
                ExportAsHtml_List(Document, writer);
                writer.Close();
            }
            else
            {
                StreamWriter writer = new StreamWriter(fileName);
                string res = ExportToHtml.Export(Document, wnd);
                writer.Write(res);
                writer.Close();
            }
        }

        private static void DumpHTMLRecursive(StreamWriter writer, OutlinerNote outlinerNote)
        {
            if (!outlinerNote.IsRoot)
            {
                string style;
                string html = HtmlFromReport((FlowDocument)outlinerNote.Columns[0].ColumnData, out style);
                string inlineNote = "";
                if (outlinerNote.HasInlineNote)
                {
                    string inlineStyle;
                    inlineNote = HtmlFromReport((FlowDocument)outlinerNote.InlineNoteDocument, out inlineStyle);
                    inlineNote = string.Format("<div id=note style='{0}'>{1}</div>", inlineStyle, inlineNote);
                }
                writer.WriteLine("<li style='{0}'>{1}{2}</li>", style, html, inlineNote);
            }

            if (outlinerNote.SubNotes.Count > 0)
            {
                writer.WriteLine("<ul>");
                for (int i = 0; i < outlinerNote.SubNotes.Count; i++)
                    DumpHTMLRecursive(writer, outlinerNote.SubNotes[i]);
                writer.WriteLine("</ul>");
            }
        }

        public static string HtmlFromReport(FlowDocument flowDocument, out string styles)
        {
            styles = "";
            TextRange range = new TextRange(flowDocument.ContentStart, flowDocument.ContentEnd);
            if (range.Text.Trim() == "")
                return "";

            StringWriter sw = new StringWriter();
            XamlWriter.Save(flowDocument, sw);
            string xaml = sw.ToString();
            styles = GetFontProperties(flowDocument);            
            return HTMLConverter.HtmlFromXamlConverter.ConvertXamlToHtml(xaml);
        }

        private static string GetFontProperties(FlowDocument flowDocument)
        {
            List<string> styles = new List<string>();

            if (flowDocument.FontFamily != null)
                styles.Add( String.Format("font-family: {0}", flowDocument.FontFamily.ToString()));
            
            styles.Add( String.Format("font-size: {0}px", flowDocument.FontSize));
            styles.Add( String.Format("color: #{0}", flowDocument.Foreground.ToString().Substring(3)));

            if (flowDocument.FontWeight == FontWeights.Bold)
                styles.Add(String.Format("font-weight: bold"));
            else
                styles.Add(String.Format("font-weight: normal"));


            if (flowDocument.FontStyle == FontStyles.Italic)
                styles.Add(String.Format("font-style: italic"));
            else
                styles.Add(String.Format("font-style: normal"));


            return String.Join("; ", styles.ToArray());
        }

        internal static void ExportAsTXT(OutlinerDocument Document, string fileName)
        {
            StreamWriter writer = new StreamWriter(fileName);
           
            DumpTextRecursively(writer, Document.RootNode, 0);
            writer.Close();            
        }

        internal static string ExportAsTXT(OutlinerDocument Document, MainWindow mainWindow)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            DumpTextRecursively(writer, Document.RootNode, 0);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        internal static string ExportAsTXT_List(OutlinerDocument Document)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            DumpTextRecursively(writer, Document.RootNode, 0);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void DumpTextRecursively(StreamWriter writer, OutlinerNote outlinerNote, int padding)
        {
            int newPadding = padding;
            if (!outlinerNote.IsRoot)
            {
                string paddingStr = "";
                for (int i = 0; i < padding; i++)
                    paddingStr += " ";

                writer.Write(paddingStr);

                 TextRange selection = new TextRange(outlinerNote.DefaultRichTextDocument.ContentStart, outlinerNote.DefaultRichTextDocument.ContentEnd);
                using (MemoryStream stream = new MemoryStream())
                {                  
                    selection.Save(stream, System.Windows.DataFormats.Text);
                    stream.Seek(0, SeekOrigin.Begin);

                    StreamReader sr = new StreamReader(stream);
                    string text = sr.ReadToEnd();
                    text = text.Replace("\r\n", "\r\n  " + paddingStr);
                    text = text.Trim();
                    writer.WriteLine("* {0}", text);                  
                }

                newPadding += 4;
            }

            for (int i = 0; i < outlinerNote.SubNotes.Count; i++)
                DumpTextRecursively(writer, outlinerNote.SubNotes[i], newPadding);
        }

        internal static string ExportAsXAML_List(OutlinerDocument Document)
        {
            FlowDocument wholeDocument = new FlowDocument();
            wholeDocument.FontFamily = UVOutliner.Settings.DefaultFontFamily;
            wholeDocument.FontSize = UVOutliner.Settings.DefaultFontSize;            
            List newList = new List();
            wholeDocument.Blocks.Add(newList);
            DumpRtfRecursively_List(newList, Document.RootNode);

            MemoryStream outStream = new MemoryStream();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(outStream, settings);
            XamlDesignerSerializationManager manager = new XamlDesignerSerializationManager(writer);
            XamlWriter.Save(wholeDocument, manager);

            outStream.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(outStream);
            return reader.ReadToEnd();
        }

        


        internal static FlowDocument ExportAsFlowDocument_List(OutlinerDocument Document)
        {
            FlowDocument wholeDocument = new FlowDocument();
            wholeDocument.FontFamily = UVOutliner.Settings.DefaultFontFamily;
            wholeDocument.FontSize = UVOutliner.Settings.DefaultFontSize;
            List newList = new List();
            wholeDocument.Blocks.Add(newList);
            DumpRtfRecursively_List(newList, Document.RootNode);
            return wholeDocument;
        }

        private static void DumpRtfRecursively_List(List list, OutlinerNote outlinerNote)
        {

            foreach(var note in outlinerNote.SubNotes)
            {                

                MemoryStream sectionStream = ExportToXaml_Printing.TransformFlowDocumentToSection(note.DefaultRichTextDocument);
                ListItem newItem = new ListItem();

                Section section = XamlReader.Load(sectionStream) as Section;
                if (section == null)
                    section = new Section();

                newItem.Blocks.Add(section);
                list.ListItems.Add(newItem);

                if (outlinerNote.SubNotes.Count > 0)
                {
                    List newList = new List();
                    list.FontFamily = UVOutliner.Settings.DefaultFontFamily;
                    list.FontSize = UVOutliner.Settings.DefaultFontSize;

                    newItem.Blocks.Add(newList);
                    DumpRtfRecursively_List(newList, note);
                }
            }
        }

        internal static void ExportAsXAML(OutlinerDocument document, MainWindow wnd, string fileName)
        {
            string xaml = ExportToXaml_WordFriendly.ExportToXaml(document, wnd);
            
            FileStream stream = new FileStream(fileName, FileMode.Create);            
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(xaml);
            writer.Close();
            stream.Close();
        }

        internal static void ExportAsXAML_List(OutlinerDocument document, string fileName)
        {
            string xaml = ExportAsXAML_List(document);

            FileStream stream = new FileStream(fileName, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(xaml);
            writer.Close();
            stream.Close();
        }

        public static void ExportAsRtf_List(OutlinerDocument document, string fileName)
        {
            FlowDocument flowDoc = ExportAsFlowDocument_List(document);
            TextRange range = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd);

            FileStream stream = new FileStream(fileName, FileMode.Create);
            range.Save(stream, DataFormats.Rtf);
            stream.Close();
        }

        public static void ExportAsRtf(OutlinerDocument document, MainWindow mainWindow, string fileName)
        {
            string documentAsXaml = ExportToXaml_WordFriendly.ExportToXaml(document, mainWindow);
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);            
            writer.Write(documentAsXaml);
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            FlowDocument flowDoc = XamlReader.Load(stream) as FlowDocument;
            TextRange range = new TextRange(flowDoc.ContentStart, flowDoc.ContentEnd);
            FileStream fStream = new FileStream(fileName, FileMode.Create);
            range.Save(fStream, DataFormats.Rtf);
            fStream.Close();
        }


        internal static void ExportAsRtf(OutlinerDocument document, MainWindow mainWindow, string fileName, bool exportAsList)
        {
            if (exportAsList)
                ExportAsRtf_List(document, fileName);
            else
                ExportAsRtf(document, mainWindow, fileName);
        }

        internal static void ExportAsXAML(OutlinerDocument document, MainWindow mainWindow, string fileName, bool exportAsList)
        {
            if (exportAsList)                
                ExportAsXAML_List(document, fileName);
            else
                ExportAsXAML(document, mainWindow, fileName);
        }
    }


}
