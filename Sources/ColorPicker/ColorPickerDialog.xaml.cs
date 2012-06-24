
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;

namespace Microsoft.Samples.CustomControls
{
    /// <summary>
    /// Interaction logic for ColorPickerDialog.xaml
    /// </summary>

    public partial class ColorPickerDialog : Window
    {
        private static ObservableCollection<SolidColorBrush> __SavedColors = new ObservableCollection<SolidColorBrush>();

        public static RoutedCommand RemoveColorCommand = new RoutedCommand();
        public static RoutedCommand RemoveAllColorsCommand = new RoutedCommand();
        private static RegistryKey registryKey = Registry.CurrentUser;

        static ColorPickerDialog()
        {
            if (!TryLoadSavedColorsFromRegistry())
            {
                // Если сохраненные значения не были найдены, то использовать по-умолчанию
                __SavedColors.Add(new SolidColorBrush(Color.FromRgb(0xdd, 0, 0)));
                __SavedColors.Add(new SolidColorBrush(Color.FromRgb(100, 150, 230)));
                __SavedColors.Add(new SolidColorBrush(Color.FromRgb(36, 194, 19)));
                __SavedColors.Add(new SolidColorBrush(Color.FromRgb(75, 72, 202)));
                __SavedColors.Add(new SolidColorBrush(Color.FromRgb(48, 46, 133)));
                __SavedColors.Add(new SolidColorBrush(Color.FromRgb(252, 252, 0)));
            }
        }

        private static void SaveSelectedColorsToRegistry()
        {
            string[] colors = new string[__SavedColors.Count];

            for (int i = 0; i < __SavedColors.Count; i++)
            {
                colors[i] = __SavedColors[i].Color.ToString();
            }

            string colorValues = String.Join(";", colors);
            RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\", true);
            if (outlinerKey != null)
            {
                outlinerKey = registryKey.CreateSubKey("Software\\UVOutliner\\", RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (outlinerKey == null)
                    return;

                outlinerKey.SetValue("SavedColors", colorValues, RegistryValueKind.String);
            }

            
        }

        private static bool TryLoadSavedColorsFromRegistry()
        {
            try
            {
                RegistryKey outlinerKey = registryKey.OpenSubKey("Software\\UVOutliner\\", true);
                if (outlinerKey != null)
                {
                    string savedColors = (string)outlinerKey.GetValue("SavedColors");
                    if (savedColors != null)
                    {
                        string[] colors = savedColors.Split(new char[] { ';' });
                        for (int i = 0; i < colors.Length; i++)
                        {
                            __SavedColors.Add(new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i])));
                        }

                        return true;
                    }
                }
            }
            catch
            {

            }

            return false;
        }

        public ColorPickerDialog()
        {
            InitializeComponent();
            SelectedColors.ItemsSource = __SavedColors;
            CommandBinding cb = new CommandBinding(ColorPicker.AddToSelected);
            cb.Executed += new ExecutedRoutedEventHandler(AddToSelected);
            cb.CanExecute += new CanExecuteRoutedEventHandler(CanAddToSelected);
            CommandBindings.Add(cb);

            cb = new CommandBinding(ColorPickerDialog.RemoveColorCommand);
            cb.Executed += new ExecutedRoutedEventHandler(RemoveColor);
            cb.CanExecute += new CanExecuteRoutedEventHandler(CanExecuteAlways);
            CommandBindings.Add(cb);

            cb = new CommandBinding(ColorPickerDialog.RemoveAllColorsCommand);
            cb.Executed += new ExecutedRoutedEventHandler(RemoveAllColors);
            cb.CanExecute += new CanExecuteRoutedEventHandler(CanExecuteAlways);
            CommandBindings.Add(cb);

            SelectedColors.Focus();
        }

        void CanExecuteAlways(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void RemoveAllColors(object sender, ExecutedRoutedEventArgs e)
        {
            __SavedColors.Clear();
            SaveSelectedColorsToRegistry();
        }

        void RemoveColor(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter == null)
                return;

            SolidColorBrush brush = (SolidColorBrush)e.Parameter;
            __SavedColors.Remove(brush);

            SaveSelectedColorsToRegistry();
        }

        void CanAddToSelected(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = cPicker.SelectedColor != m_color;
        }

        void AddToSelected(object sender, ExecutedRoutedEventArgs e)
        {
            if (__SavedColors.Count >= 17)
                __SavedColors[16] = new SolidColorBrush(cPicker.SelectedColor);
            else
                __SavedColors.Add(new SolidColorBrush(cPicker.SelectedColor));

            SaveSelectedColorsToRegistry();
        }

        private void okButtonClicked(object sender, RoutedEventArgs e)
        {

            OKButton.IsEnabled = false;
            m_color = cPicker.SelectedColor;
            DialogResult = true;
            Hide();

        }


        private void cancelButtonClicked(object sender, RoutedEventArgs e)
        {

            OKButton.IsEnabled = false;
            DialogResult = false;

        }

        private void onSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
        {

            if (e.NewValue != m_color)
            {
                OKButton.IsEnabled = true;
                CommandManager.InvalidateRequerySuggested();
                
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {

            OKButton.IsEnabled = false;
            base.OnClosing(e);
        }


        private Color m_color = new Color();
        private Color startingColor = new Color();

        public Color SelectedColor
        {
            get
            {
                return m_color;
            }

        }
        
        public Color StartingColor
        {
            get
            {
                return startingColor;
            }
            set
            {
                cPicker.SelectedColor = value;
                OKButton.IsEnabled = false;
                
            }

        }

        private void SelectedColor_Click(object sender, RoutedEventArgs e)
        {
            cPicker.SelectedColor = ((SolidColorBrush)(((Button)sender).Tag)).Color;
        }        
    }
}