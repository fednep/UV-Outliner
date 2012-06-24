using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Markup;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows;
using System.Xml;

namespace UVOutliner.Export
{
    public class ExportHelpers
    {
        internal static Section GetParagraphWithFigures(Paragraph figure1, Section figure2)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter writer = new StreamWriter(ms);
            writer.Write("<Section xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
            writer.Write("<Paragraph><Figure VerticalAnchor=\"PageTop\" HorizontalAnchor=\"PageLeft\" Margin=\"0,0,0,0\" Padding=\"0,0,0,0\">");
            writer.Write("<Paragraph Margin=\"0,0,0,0\">ПИЗДЕЦ</Paragraph>");
            writer.Write("</Figure><Figure VerticalAnchor=\"PageTop\" HorizontalAnchor=\"PageLeft\" Margin=\"0,0,0,0\" Padding=\"0,0,0,0\">");
            writer.Write("<Paragraph Margin=\"0,0,0,0\">ПИЗДЕЦ 2</Paragraph>");
            writer.Write("</Figure></Paragraph></Section>");
            writer.Flush();
            ms.Flush();
            ms.Seek(0, SeekOrigin.Begin);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.CheckCharacters = false;            

            XmlReader reader = XmlReader.Create(ms, settings);
            return XamlReader.Load(reader) as Section;


        }
    }
}
