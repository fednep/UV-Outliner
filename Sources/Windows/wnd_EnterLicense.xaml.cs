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
using System.Diagnostics;

namespace UVOutliner
{
    /// <summary>
    /// Interaction logic for wnd_EnterLicense.xaml
    /// </summary>
    public partial class wnd_EnterLicense : Window
    {
        public wnd_EnterLicense()
        {
            InitializeComponent();
        }

        private void WhereFind_Click(object sender, RoutedEventArgs e)
        {
            UrlHelpers.OpenInBrowser("http://uvoutliner.com/purchase/#wherelicense");
        }

        private void HowToBuy_Click(object sender, RoutedEventArgs e)
        {
            UrlHelpers.OpenInBrowser("http://uvoutliner.com/purchase/");
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ValidateKey(tbLicenseKey.Text);
        }

        private void ValidateKey(string serial)
        {
            if (SerialKey.VerifyKey(serial))
            {
                SerialKey.RegisterSerialKey(serial);
                DialogResult = true;
            }
        }
    }
}
