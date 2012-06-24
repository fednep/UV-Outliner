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
using System.Xml;
using System.Windows.Markup;
using System.IO;

namespace UVOutliner.Columns
{
    class RichTextColumn: OutlinerColumn
    {
        static readonly bool EnableCache = true;

        FlowDocument __Document;
        OutlinerNote __Note;

        public RichTextColumn(OutlinerNote note)
        {
            __Document = null;
            __Note = note;
        }

        public override ColumnDataType DataType
        {
            get { return ColumnDataType.RichText; }
        }

        public override object ColumnData
        {
            get {

                if (!EnableCache)
                {
                    if (__Document == null)                    
                        CreateNewDocument();                    

                    return __Document;
                }

                if (__Document == null)                
                {
                    if (__LazyDocument == null)
                    {
                        CreateNewDocument();
                    }
                      else
                    {
                        MemoryStream stream = new MemoryStream();
                        StreamWriter writer = new StreamWriter(stream);
                        writer.Write(__LazyDocument);
                        writer.Flush();
                        stream.Seek(0, SeekOrigin.Begin);
                        __Document = (FlowDocument)XamlReader.Load(stream);
                        if (__Document.Tag == null)
                            __Document.Tag = Guid.NewGuid();

                        DoPropertyChanged("ColumnData");
                        __Note.UpdateIsEmpty();
                        __LazyDocument = null;
                    }
                }

                return __Document; 
            }
        }

        public void InvalidateLazyDocument()
        {
            __LazyDocument = null;
        }

        private void CreateNewDocument()
        {
            __Document = new FlowDocument();            
            __Document.Tag = Guid.NewGuid();

            var p = new Paragraph();
            p.FontSize = Settings.DefaultFontSize;
            p.FontFamily = Settings.DefaultFontFamily;
            __Document.Blocks.Add(p);
        }

        public override void RemoveColumnData()
        {
            __Document = null;
            __LazyDocument = null;
            __Note = null;
        }

        public override void CopyColumn(OutlinerColumn oldColumn)
        {           
            MemoryStream stream = new MemoryStream();
            XmlWriter wr = XmlWriter.Create(stream);
            oldColumn.Save(wr);
            wr.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            XmlReader reader = XmlReader.Create(stream);
            Load(reader);
            reader.Close();
            stream.Close();
        }

        public override void Save(XmlWriter writer)
        {
            writer.WriteStartElement("Note");

            if (__Document != null)
            {
                writer.WriteRaw(XamlWriter.Save(__Document));
            }
            else
            {
                if (__LazyDocument != null)
                    writer.WriteRaw(__LazyDocument.ToString());
                else
                {
                    __Document = new FlowDocument();
                    writer.WriteRaw(XamlWriter.Save(__Document));
                }
            } 
            
            writer.WriteEndElement();
        }

        public override void Load(XmlReader reader)
        {
            if (!reader.ReadToFollowing("Note"))
                return;

            reader.ReadToFollowing("FlowDocument");
            XmlReader xmlReader = reader.ReadSubtree();

            if (!EnableCache)
            {
                __Document = (FlowDocument)XamlReader.Load(xmlReader);
                if (__Document.Tag == null)
                    __Document.Tag = Guid.NewGuid();
            }
            else
                StoreToString(xmlReader);

            xmlReader.Close();            
        }

        private StringBuilder __LazyDocument;

        private void StoreToString(XmlReader reader)
        {
            __LazyDocument = new StringBuilder();
	        while (reader.Read())
                __LazyDocument.Append(reader.ReadOuterXml());	            
        }

        public override bool IsEmpty
        {
            get
            {
                if (__Document == null)
                    return true;

                TextRange range = new TextRange(
                            __Document.ContentStart,
                            __Document.ContentEnd);

                return range.Text.Trim() == "";
            }
        }

        public override bool OwnsDocument(Guid documentGuid)
        {
            if (__Document == null)
                return false;

            return __Document.Tag.Equals(documentGuid);
        }
    }
}
