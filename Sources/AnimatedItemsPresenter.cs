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
using System.Windows.Media.Animation;

namespace UVOutliner
{
    class AnimatedItemsPresenter : ItemsPresenter
    {
        public readonly static DependencyProperty IsExpandedProperty = DependencyProperty.Register("IsExpanded",
            typeof(bool), typeof(AnimatedItemsPresenter),
            new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.AffectsArrange |
                FrameworkPropertyMetadataOptions.AffectsMeasure |
                FrameworkPropertyMetadataOptions.AffectsRender,
                IsExpandedPropertyChanged));

        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set
            {
                SetValue(IsExpandedProperty, value);
            }
        }

        private Duration _Duration = new Duration(new TimeSpan(0, 0, 0, 0, 100));

        public AnimatedItemsPresenter()
            : base()
        {
            this.Visibility = System.Windows.Visibility.Collapsed;

        }

        private static void IsExpandedPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            AnimatedItemsPresenter presenter = sender as AnimatedItemsPresenter;
            if (presenter != null)
            {
                if ((bool)e.NewValue == true) presenter.DoExpand();
                else presenter.DoCollapse();
            }
        }

        private delegate void SetValueDelegate(DependencyProperty dp, object value);

        private void DoExpand()
        {
            this.Visibility = System.Windows.Visibility.Visible;
            this.Height = Double.NaN;
            this.Measure(new Size(this.MaxWidth, this.MaxHeight));
            double from = 0;
            double to = this.DesiredSize.Height;
            AnimateHeight(from, to, _Duration, null);
        }

        private void DoCollapse()
        {
            double to = 0;
            double from = this.ActualHeight;
            this.Height = to;
            AnimateHeight(from, to, _Duration,
                (sender, e) =>
                {
                    this.Visibility = System.Windows.Visibility.Collapsed;
                });
        }

        private void AnimateHeight(double from, double to, Duration duration, EventHandler callback)
        {
            Storyboard animationStoryBoard = new Storyboard();
            DoubleAnimation heightAnimation = new DoubleAnimation(from, to, duration);
            Storyboard.SetTargetName(heightAnimation, this.Name);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(FrameworkElement.HeightProperty));
            animationStoryBoard.Children.Add(heightAnimation);
            if (callback != null) animationStoryBoard.Completed += callback;
            animationStoryBoard.FillBehavior = FillBehavior.Stop;
            animationStoryBoard.Begin(this);
        }
    }

}
