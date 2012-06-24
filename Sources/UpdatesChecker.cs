/*  Copyright (C) 2005-2012 Fedir Nepyivoda <fednep@gmail.com>
    
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
    along with UV Outliner.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UVOutliner.Lib;
using System.Net;
using System.Windows;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;

namespace UVOutliner
{
    internal class UpdatesChecker
    {
        MainWindow __MainWindow;
        public UpdatesChecker(MainWindow mainWindow)
        {
            __MainWindow = mainWindow;
        }

        public void Start()
        {
            Thread checkerThread = new Thread((ThreadStart)delegate
                {
                    Thread_CheckForUpdates();
                });
        }

        private void Thread_CheckForUpdates()
        {
            try
            {
                Version currentVersion = app.CurrentVersion;
                WebClient client = new WebClient();

                string page = System.Text.Encoding.UTF8.GetString(client.DownloadData(String.Format("http://updates.uvoutliner.com/?currentVersion=%s", currentVersion.ToString())));
                var newVersion = GetVersion(page);

                if (newVersion != null && newVersion > currentVersion)
                {
                    __MainWindow.Dispatcher.Invoke((Action)delegate
                    {
                        __MainWindow.NewVersionExists(newVersion);
                    });
                }
            }
            catch
            { }

        }

        private Version GetVersion(string page)
        {
            
            Regex r = new Regex(@"span id='lastversion'>(.*?)<", RegexOptions.Compiled);
            var res = r.Match(page);
            if (res.Groups.Count == 2)
                return new Version(res.Groups[1].Value);

            return null;
        }

    }
}
