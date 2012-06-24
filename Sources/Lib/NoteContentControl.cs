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
using System.Windows.Controls;
using System.Windows;

namespace UVOutliner.Lib
{
    public class NoteContentControl : ContentControl
    {

        public static readonly RoutedEvent TemplateChangedEvent = EventManager.RegisterRoutedEvent("TemplateChangedEvent",
            RoutingStrategy.Bubble, typeof(RoutedEventArgs), typeof(NoteContentControl));

        protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);                
            RaiseEvent(new RoutedEventArgs(TemplateChangedEvent, this));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();            
        }
    }


    public class NoteContentPresenter : ContentPresenter
    {

        public static readonly RoutedEvent TemplateChangedEvent = EventManager.RegisterRoutedEvent("TemplateChangedEvent",
            RoutingStrategy.Bubble, typeof(RoutedEventArgs), typeof(NoteContentPresenter));

        protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            RaiseEvent(new RoutedEventArgs(TemplateChangedEvent, this));
        }
    }
}
