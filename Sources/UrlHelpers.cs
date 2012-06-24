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
using System.Diagnostics;
using System.Windows;

namespace UVOutliner
{
    public static class UrlHelpers
    {
        public static void OpenInBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                Clipboard.SetText(url);
                MessageBox.Show("Error opening browser at:\n{0}\n\n Please open it manually. Link above was copied to the clipboard.");
            }
        }
    }
}
