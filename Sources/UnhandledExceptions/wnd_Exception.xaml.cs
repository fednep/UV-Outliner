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
using System.Net;
using System.IO;
using System.Web;
using System.Threading;

namespace UVOutliner.UnhandledException
{
    /// <summary>
    /// Interaction logic for wnd_Exception.xaml
    /// </summary>
    public partial class wnd_Exception : Window
    {
        private Exception exc = null;
        private string report_text = "";

        public wnd_Exception()
        {
            InitializeComponent();
            Activated += new EventHandler(wnd_Exception_Activated);
        }

        void wnd_Exception_Activated(object sender, EventArgs e)
        {
            tbErrorComment.Focus();
        }

        public void ShowException(string message, Exception e)
        {
            tbError.Text = message;            
            exc = e;
            report_text = string.Format("{0}\n\n{1}\n\n{2}\n\n{3}",
                exc.Message, exc.StackTrace, exc.InnerException, e.GetType().ToString());
            ShowDialog();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            wnd_ExceptionDetails exd = new wnd_ExceptionDetails();
            exd.tbException.Text = report_text;
            exd.Owner = this;
            exd.ShowDialog();
        }

        private bool SendErrorReport(string comment)
        {
            string responseString = "";
            try
            {   
                WebRequest req = (HttpWebRequest)WebRequest.Create("http://uvoutliner.com/cgi-bin/erp.py");
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";

                string dataToSend = report_text;
                
                if (comment != "")
                    dataToSend = String.Format("Comment: {0}\n-------------------------------------------\n{1}", comment, dataToSend);
                dataToSend = string.Format("Version: {0}\n{1}", GetVersion(), dataToSend);
                
                string postData = string.Format("err={0}", WebUtility.HtmlEncode(dataToSend));

                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] byte1 = encoding.GetBytes(postData);
                req.ContentLength = byte1.Length;

                Stream newStream = req.GetRequestStream();
                newStream.Write(byte1, 0, byte1.Length);
                // Close the Stream object.
                newStream.Close();

                WebResponse response = req.GetResponse();
                Stream receiveStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(receiveStream);
                responseString = streamReader.ReadToEnd();
                response.Close();
                streamReader.Close();
            }
            catch
            {
                return false;
            }

            if (responseString != "done.")
                return false;

            return true;
        }

        private string GetVersion()
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
            return a.GetName().Version.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            var res = SendErrorReport(tbErrorComment.Text.Trim());
            
            if (res == false)
                MessageBox.Show((string)Application.Current.FindResource("MsgBox_ErrorReportFailed"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            Close();
        }
    }
}
